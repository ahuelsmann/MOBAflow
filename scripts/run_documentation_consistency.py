"""CI entry points. Execute this trusted base-branch file, never a PR's copy."""

import argparse
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
from pathlib import Path

from documentation_consistency import (
    can_autofix, content_hash, context, editable, fact_updates, git,
    text, validate_proposal, verify_links, write_text,
)


CHECK_NAME = "Documentation consistency"
TRUSTED_AUTHORS = {"OWNER", "MEMBER", "COLLABORATOR"}


def object_schema(properties):
    return {"type": "object", "properties": properties, "required": list(properties), "additionalProperties": False}


STRING = {"type": "string"}
EVIDENCE_SCHEMA = object_schema({"path": STRING, "quote": STRING})
PROPOSAL_SCHEMA = object_schema({
    "status": {"type": "string", "enum": ["consistent", "update_required", "uncertain"]},
    "summary": STRING,
    "edits": {"type": "array", "items": object_schema({
        "path": STRING, "old_text": STRING, "new_text": STRING,
        "evidence": {"type": "array", "items": EVIDENCE_SCHEMA},
    })},
})
REVIEW_SCHEMA = object_schema({
    "status": {"type": "string", "enum": ["consistent", "inconsistent", "uncertain"]},
    "summary": STRING,
    "findings": {"type": "array", "items": STRING},
})


def request_json(url, token, payload=None, method=None, timeout=30):
    data = None if payload is None else json.dumps(payload).encode()
    headers = {"Authorization": "Bearer " + token, "Content-Type": "application/json",
               "Accept": "application/json", "User-Agent": "MOBAflow-documentation-consistency"}
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        # Do not expose request bodies, API responses or credentials in CI logs.
        raise RuntimeError("Remote API returned HTTP " + str(error.code)) from None


class GitHub:
    def __init__(self, repository, token):
        if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
            raise ValueError("Invalid repository identifier")
        self.root = "https://api.github.com/repos/" + repository
        self.token = token

    def call(self, path, payload=None, method=None):
        return request_json(self.root + path, self.token, payload, method)


def check_configuration(environment):
    required = ("OPENAI_API_KEY", "DOCS_AI_MODEL")
    missing = [name for name in required if not environment.get(name, "").strip()]
    if missing:
        raise ValueError("Documentation AI is enabled but configuration is missing: " + ", ".join(missing))


def call_model(bundle, prompt, schema, environment):
    check_configuration(environment)
    result = request_json("https://api.openai.com/v1/responses", environment["OPENAI_API_KEY"], {
        "model": environment["DOCS_AI_MODEL"], "instructions": prompt,
        "input": json.dumps(bundle), "store": False, "max_output_tokens": 8000,
        "text": {"format": {"type": "json_schema", "name": "documentation_result", "strict": True, "schema": schema}},
        # No tools: repository data cannot cause command execution or credential access.
    }, timeout=60)
    if result.get("status") != "completed":
        raise ValueError("AI response is incomplete")
    messages = [entry for item in result.get("output", []) if item.get("type") == "message"
                for entry in item.get("content", [])]
    if any(entry.get("type") == "refusal" for entry in messages):
        raise ValueError("AI declined the documentation assessment")
    values = [entry["text"] for entry in messages if entry.get("type") == "output_text"]
    if len(values) != 1:
        raise ValueError("Expected one structured AI response")
    return json.loads(values[0])


def scan(value):
    result = subprocess.run(["gitleaks", "stdin", "--redact", "--no-banner"], input=json.dumps(value).encode(),
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if result.returncode != 0:
        raise ValueError("Secrets scan failed; repository context or proposed documentation was not published")


def assess(root, base, head, control, environment, model_call=call_model, scanner=scan):
    originals = {}
    facts = fact_updates(root)
    for name, value in facts.items():
        originals[name] = text(root, name)
        write_text(root, name, value)
    bundle = context(root, base, head)
    scanner(bundle)
    writer = text(control, ".github/documentation/assess.md")
    proposal = model_call(bundle, writer, PROPOSAL_SCHEMA, environment)
    proposed = validate_proposal(bundle, proposal)
    candidate = dict(bundle)
    candidate["documents"] = dict(bundle["documents"], **proposed)
    # A new request with no writer conversation or rationale; verify the candidate independently.
    reviewer = text(control, ".github/documentation/verify.md")
    review = model_call(candidate, reviewer, REVIEW_SCHEMA, environment)
    if review["status"] != "consistent" or review["findings"]:
        raise ValueError("Independent documentation review did not pass")
    updates = dict(facts, **proposed)
    scanner(updates)
    verify_links(root, updates)
    for name in proposed:
        originals.setdefault(name, bundle["documents"][name])
    # The publisher receives data only; no script, patch command or generated workflow is executed.
    return {"base": base, "head": head, "summary": proposal["summary"],
            "autofix": can_autofix(root, head),
            "changes": [{"path": name, "before_sha256": content_hash(originals[name]), "content": value}
                        for name, value in sorted(updates.items())]}


def validate_publication(root, proposal):
    changes = proposal["changes"]
    if changes and not proposal["autofix"]:
        raise ValueError("Latest commit changes documentation only; correct remaining findings manually")
    seen = set()
    generated = fact_updates(root)
    for change in changes:
        name = change["path"]
        if name in seen or (not editable(name) and name != "docs/THIRD-PARTY-NOTICES.md"):
            raise ValueError("Publication contains a duplicate or disallowed path")
        seen.add(name)
        if content_hash(text(root, name)) != change["before_sha256"]:
            raise ValueError("Publication input differs from the assessed document")
        if not change["content"].strip() or len(change["content"].encode()) > 200_000:
            raise ValueError("Publication document is empty or too large")
        if name == "docs/THIRD-PARTY-NOTICES.md" and change["content"] != generated.get(name):
            raise ValueError("Package notice changes must match deterministic generation exactly")
    return changes


def publish(root, proposal, repository, number, token, api=None):
    api = api or GitHub(repository, token)
    pr = api.call("/pulls/" + str(number))
    if pr["state"] != "open" or pr["head"]["sha"] != proposal["head"] or pr["base"]["sha"] != proposal["base"]:
        raise ValueError("PR changed since assessment; rerun on its latest head/base")
    if pr["head"]["repo"]["full_name"] != repository or pr["author_association"] not in TRUSTED_AUTHORS:
        raise ValueError("Automatic updates require a trusted same-repository PR")
    if pr["head"]["ref"] == "main" or pr["head"]["ref"] == pr["base"].get("ref"):
        raise ValueError("Publication must target a task branch, never main or the PR base")
    if git(root, "rev-parse", "HEAD").decode().strip() != proposal["head"]:
        raise ValueError("Publisher checkout is not the assessed head")
    changes = validate_publication(root, proposal)
    scan(changes)
    for change in changes:
        write_text(root, change["path"], change["content"])
    verify_links(root, {change["path"]: change["content"] for change in changes})
    if fact_updates(root):
        raise ValueError("Published documentation facts do not match the PR source")
    if not changes:
        return proposal["head"]
    subprocess.run(["git", "-C", str(root), "add", "--", *[change["path"] for change in changes]], check=True)
    subprocess.run(["git", "-C", str(root), "-c", "core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol",
                    "diff", "--cached", "--check"], check=True)
    tree = git(root, "write-tree").decode().strip()
    env = dict(os.environ, GIT_AUTHOR_NAME="MOBAflow Documentation Bot", GIT_COMMITTER_NAME="MOBAflow Documentation Bot",
               GIT_AUTHOR_EMAIL="documentation-bot@users.noreply.github.com", GIT_COMMITTER_EMAIL="documentation-bot@users.noreply.github.com")
    commit = subprocess.check_output(["git", "-C", str(root), "commit-tree", tree, "-p", proposal["head"],
                                      "-m", "docs: align documentation with PR changes"], env=env).decode().strip()
    # A lease rejects a human push during assessment/publishing, without overwriting it.
    import base64
    env.update(GIT_CONFIG_COUNT="1", GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
               GIT_CONFIG_VALUE_0="AUTHORIZATION: basic " + base64.b64encode(("x-access-token:" + token).encode()).decode())
    branch = pr["head"]["ref"]
    subprocess.run(["git", "-C", str(root), "push", "--force-with-lease=refs/heads/" + branch + ":" + proposal["head"],
                    "https://github.com/" + repository + ".git", commit + ":refs/heads/" + branch], env=env, check=True)
    return commit


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("assess", "publish", "report"))
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--control", type=Path, required=True)
    parser.add_argument("--result", type=Path, required=True)
    args = parser.parse_args()
    event = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text())
    number = event["pull_request"]["number"]
    repository = os.environ["GITHUB_REPOSITORY"]
    api = GitHub(repository, os.environ["GITHUB_TOKEN"])
    pr = api.call("/pulls/" + str(number))
    head, base = pr["head"]["sha"], pr["base"]["sha"]
    if args.mode == "report":
        result = json.loads(args.result.read_text()) if args.result.exists() else {}
        conclusion = result.get("conclusion", "failure")
        message = result.get("summary", "Documentation workflow did not complete; inspect its job log.")
        api.call("/check-runs", {"name": CHECK_NAME, "head_sha": event["pull_request"]["head"]["sha"],
                 "status": "completed", "conclusion": conclusion,
                 "output": {"title": CHECK_NAME, "summary": message[:60_000]}})
        return
    if args.mode == "publish":
        proposal = json.loads(args.result.read_text())
        new_head = publish(args.root, proposal, repository, number, os.environ["DOCS_WRITE_TOKEN"])
        proposal.update(conclusion="neutral", summary="Documentation updated to " + new_head + "; checks must run on the new head.")
        save(args.result, proposal)
        return
    result = {"head": head, "base": base, "changes": [], "conclusion": "failure", "summary": "Assessment did not finish."}
    try:
        if os.environ.get("DOCS_AI_ENABLED") != "true":
            result.update(conclusion="neutral", summary="AI documentation maintenance is not activated. Deterministic facts remain enforced by Quality.")
        elif pr["head"]["repo"]["full_name"] != repository or pr["author_association"] not in TRUSTED_AUTHORS:
            result.update(conclusion="action_required", summary="A maintainer must review documentation for this fork or untrusted PR; no AI credentials or writes were used.")
        else:
            if head != event["pull_request"]["head"]["sha"] or base != event["pull_request"]["base"]["sha"]:
                raise ValueError("Event is stale; rerun against the current PR head/base")
            check_configuration(os.environ)
            result.update(assess(args.root, base, head, args.control, os.environ))
            if result["changes"]:
                if not result["autofix"]:
                    raise ValueError("Documentation-only last commit still needs corrections; automatic rewriting is stopped")
                result.update(conclusion="action_required", summary="Verified documentation corrections are ready for publication.")
            else:
                result.update(conclusion="success", summary="Deterministic facts and independent semantic documentation review passed.")
    except (ValueError, RuntimeError, OSError, KeyError, subprocess.CalledProcessError) as error:
        result.update(conclusion="failure", summary=str(error)[:1000])
    save(args.result, result)
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        output.write("publish=" + str(bool(result["changes"]) and result["conclusion"] == "action_required").lower() + "\n")


if __name__ == "__main__":
    main()
