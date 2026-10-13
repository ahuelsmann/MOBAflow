"""Trusted documentation facts and bounded AI proposals; no repository code is executed."""

import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath


PACKAGE_DOC = "docs/THIRD-PARTY-NOTICES.md"
MUTATION_DOC = "docs/BUILD-PERFORMANCE.md"
START = "<!-- documentation-facts:{name}:start -->"
END = "<!-- documentation-facts:{name}:end -->"
MAX_CONTEXT_BYTES = 1_000_000
MAX_DOCUMENT_BYTES = 200_000


def git(root, *arguments):
    return subprocess.check_output(["git", "-C", str(root), *arguments])


def tracked(root):
    return git(root, "ls-files", "-z").decode().rstrip("\0").split("\0")


def safe_path(root, name):
    """Reject traversal, platform-dependent paths and links before reading or writing."""
    parts = PurePosixPath(name).parts
    if not parts or name.startswith("/") or "\\" in name or ":" in name or ".." in parts:
        raise ValueError("Unsafe repository path")
    root = Path(root).resolve()
    path = root.joinpath(*parts)
    current = root
    for part in parts:
        current = current / part
        if current.is_symlink():
            raise ValueError("Repository symlinks are not supported")
    if not path.resolve().is_relative_to(root):
        raise ValueError("Path escapes repository")
    return path


def text(root, name):
    data = safe_path(root, name).read_bytes()
    if len(data) > MAX_DOCUMENT_BYTES or b"\0" in data:
        raise ValueError("File is binary or exceeds the context limit: " + name)
    return data.decode("utf-8-sig").replace("\r\n", "\n")


def write_text(root, name, value):
    safe_path(root, name).write_bytes(value.replace("\r\n", "\n").replace("\n", "\r\n").encode())


def xml(root, name):
    value = text(root, name)
    if "<!DOCTYPE" in value or "<!ENTITY" in value:
        raise ValueError("XML entities are not supported")
    return ET.fromstring(value)


def replace_region(value, name, replacement, table_header):
    start, end = START.format(name=name), END.format(name=name)
    if start in value or end in value:
        if value.count(start) != 1 or value.count(end) != 1:
            raise ValueError("Invalid generated documentation region: " + name)
        before, tail = value.split(start)
        previous, after = tail.split(end)
        if START.format(name=name) in previous:
            raise ValueError("Nested generated region")
        return before + start + "\n" + replacement.rstrip() + "\n" + end + after
    # Bootstrap the existing table only; surrounding prose and licenses remain intact.
    location = value.index(table_header)
    tail = value[location:]
    table = re.match(r"(?:\|[^\n]*\n)+", tail)
    if table is None:
        raise ValueError("Missing source table: " + name)
    return value[:location] + start + "\n" + replacement.rstrip() + "\n" + end + "\n" + tail[table.end():]


def package_table(root):
    versions = {item.attrib["Include"]: item.attrib["Version"]
                for item in xml(root, "Directory.Packages.props").iter("PackageVersion")}
    references = {}
    for name in tracked(root):
        if not (name.endswith(".csproj") or name in ("Directory.Build.props", "Directory.Build.targets")):
            continue
        for item in xml(root, name).iter("PackageReference"):
            package = item.attrib.get("Include")
            if not package:
                continue  # Update changes metadata on an existing reference.
            if package not in versions:
                raise ValueError("Package lacks a central version: " + package)
            scope = str(PurePosixPath(name).parent) if name.endswith(".csproj") else "Build-wide via `" + name + "`"
            references.setdefault(package, set()).add(scope)
    value = text(root, PACKAGE_DOC)
    purposes = {match[0]: match[1] for match in re.findall(
        r"^\| `([^`]+)` \| `[^`]+` \| [^\n]*? \| ([^\n]+) \|$", value, re.M)}
    rows = ["| Package | Version | Directly referenced in | Purpose |", "| --- | --- | --- | --- |"]
    for package in sorted(references, key=str.casefold):
        scope = ", ".join(item if item.startswith("Build-wide") else "`" + item + "`"
                          for item in sorted(references[package]))
        purpose = purposes.get(package, "New direct dependency; purpose and upstream license require review.")
        rows.append(f"| `{package}` | `{versions[package]}` | {scope} | {purpose} |")
    return "\n".join(rows)


def mutation_table(root):
    lanes = json.loads(text(root, "MutationTest/mutation-lanes.json"))["lanes"]
    configurations = [(name, json.loads(text(root, name))["stryker-config"])
                      for name in tracked(root) if re.fullmatch(r"MutationTest/stryker[^/]*\.json", name)]
    rows = ["| Active lane | Production project | Test project | Stryker configuration | Break threshold |",
            "| --- | --- | --- | --- | --- |"]
    for lane in lanes:
        if lane["status"] != "active":
            continue
        production = lane["production-projects"]
        matches = [(name, config) for name, config in configurations
                   if config["project"] in [PurePosixPath(item).name for item in production]]
        if len(matches) != 1:
            raise ValueError("Expected one configuration for active lane " + lane["name"])
        name, config = matches[0]
        tests = ", ".join("`MutationTest/" + item.replace("\\", "/") + "`" for item in config["test-projects"])
        projects = ", ".join("`" + item + "`" for item in production)
        rows.append(f"| {lane['name']} | {projects} | {tests} | [`{PurePosixPath(name).name}`](../{name}) | {config['thresholds']['break']}% |")
    return "\n".join(rows)


def fact_updates(root):
    changes = {}
    for name, region, generated, header in (
        (PACKAGE_DOC, "packages", package_table(root), "| Package | Version |"),
        (MUTATION_DOC, "mutation", mutation_table(root), "| Active lane |"),
    ):
        previous = text(root, name)
        updated = replace_region(previous, region, generated, header)
        if updated != previous:
            changes[name] = updated
    # Native-component rows repeat central package versions outside the direct inventory.
    versions = {item.attrib["Include"]: item.attrib["Version"]
                for item in xml(root, "Directory.Packages.props").iter("PackageVersion")}
    notices = changes.get(PACKAGE_DOC, text(root, PACKAGE_DOC))
    for package in ("SkiaSharp", "Microsoft.WindowsAppSDK", "Microsoft.Graphics.Win2D", "Xamarin.AndroidX.Startup.StartupRuntime"):
        notices = re.sub(r"`" + re.escape(package) + r" [^`]+` in `Directory.Packages.props`",
                         "`" + package + " " + versions[package] + "` in `Directory.Packages.props`", notices)
    if notices != text(root, PACKAGE_DOC):
        changes[PACKAGE_DOC] = notices
    return changes


def is_documentation(name):
    return name.endswith(".md") and (name.startswith(("docs/", "specs/", "plans/", ".github/")) or "/" not in name)


def editable(name):
    return name in ("README.md", "CONTRIBUTING.md") or (
        name.startswith("docs/") and name.endswith(".md") and name != PACKAGE_DOC)


def changed_paths(root, base, head):
    # Include both sides of renames/deletions; a missing current file is described by the diff.
    return git(root, "diff", "--name-only", "--no-renames", "-z", base, head).decode().rstrip("\0").split("\0")


def can_autofix(root, head):
    # A bot-generated documentation-only commit cannot cause another automatic commit.
    latest = changed_paths(root, head + "^", head)
    return any(name and not is_documentation(name) for name in latest)


def source_file(name):
    return name.endswith((".cs", ".csproj", ".props", ".targets", ".xaml", ".py", ".ps1", ".yml", ".yaml", ".ini", ".cpp", ".h", ".hpp", ".c")) or name in (
        "global.json", "MutationTest/mutation-lanes.json", "MutationTest/stryker-config.json", "MutationTest/stryker-common-config.json")


def context(root, base, head):
    paths = [name for name in changed_paths(root, base, head) if name]
    existing = set(tracked(root))
    # Include composition and ownership anchors even when only an implementation changed.
    anchors = {
        "MOBAsmart/Extensions/MobaMauiServiceCollectionExtensions.cs",
        "MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs",
        "SharedUI/Service/MobileSolutionContext.cs", "SharedUI/Service/SolutionSession.cs",
        "Backend/Protocol/Z21Protocol.cs", "Backend/Interface/IMobaRuntime.cs",
        "Backend/Service/ProjectRuntimes/ProjectRuntimeHost.cs",
        "SharedUI/Interface/IRuntimeCommandGateway.cs", ".github/workflows/quality.yml",
        "scripts/Get-QualityChangeScope.ps1", "Directory.Packages.props",
        "MutationTest/mutation-lanes.json", "MutationTest/stryker-config.json",
        "MutationTest/stryker-common-config.json",
    }
    # Include callers, interfaces and tests mentioning a changed source type/file stem.
    stems = {PurePosixPath(name).stem.split(".")[0] for name in paths if name.endswith(".cs")}
    sources = {}
    for name in sorted(existing):
        if not source_file(name):
            continue
        if safe_path(root, name).stat().st_size > MAX_DOCUMENT_BYTES and name not in paths and name not in anchors:
            continue
        value = text(root, name)
        if name in paths or name in anchors or any(stem in value for stem in stems):
            sources[name] = value
    documents = {name: text(root, name) for name in sorted(existing) if editable(name)}
    bundle = {"changed_paths": paths, "diff": git(root, "diff", "--no-ext-diff", base, head).decode("utf-8", errors="replace"),
              "sources": sources, "documents": documents}
    if len(json.dumps(bundle).encode()) > MAX_CONTEXT_BYTES:
        raise ValueError("PR context exceeds the limit; manual documentation review is required")
    return bundle


def validate_proposal(bundle, proposal):
    if proposal["status"] not in ("consistent", "update_required", "uncertain"):
        raise ValueError("Unknown proposal status")
    if proposal["status"] == "uncertain":
        raise ValueError("AI could not establish documentation consistency")
    edits = proposal["edits"]
    if (proposal["status"] == "consistent") != (not edits):
        raise ValueError("Inconsistent proposal status")
    documents = dict(bundle["documents"])
    for edit in edits:
        name, old, new = edit["path"], edit["old_text"], edit["new_text"]
        if not editable(name) or name not in documents or not old.strip() or not new.strip():
            raise ValueError("Proposal must update an existing allowed document")
        if old == new or documents[name].count(old) != 1:
            raise ValueError("Edit must replace one unique passage")
        if not edit["evidence"]:
            raise ValueError("Edit lacks source evidence")
        for evidence in edit["evidence"]:
            source = bundle["sources"].get(evidence["path"], "")
            if not evidence["quote"].strip() or evidence["quote"] not in source:
                raise ValueError("Evidence does not match the supplied source")
        # Generated facts are never controlled by AI, including cross-boundary replacements.
        for match in re.finditer(r"<!-- documentation-facts:.*?:start -->.*?<!-- documentation-facts:.*?:end -->", documents[name], re.S):
            location = documents[name].index(old)
            if location < match.end() and location + len(old) > match.start():
                raise ValueError("AI cannot edit generated facts")
        if "<!-- documentation-facts:" in new:
            raise ValueError("AI cannot add generated-region markers")
        documents[name] = documents[name].replace(old, new, 1)
        if len(documents[name].encode()) > MAX_DOCUMENT_BYTES:
            raise ValueError("Updated document exceeds the size limit")
    return {name: value for name, value in documents.items() if value != bundle["documents"][name]}


def verify_links(root, changes):
    for name, value in changes.items():
        value = re.sub(r"(?ms)^```.*?^```\s*$", "", value)
        value = re.sub(r"`[^`\n]+`", "", value)
        for target in re.findall(r"!?\[[^\]]*\]\(([^)\s]+)(?:\s+\"[^\"]*\")?\)", value):
            if re.match(r"[a-zA-Z][a-zA-Z0-9+.-]*:|#", target):
                continue
            path = target.split("#", 1)[0].split("?", 1)[0]
            destination = (Path(root) / name).parent / path
            if not destination.resolve().is_relative_to(Path(root).resolve()) or not destination.exists():
                raise ValueError("Updated document has a missing/outside link")


def content_hash(value):
    return hashlib.sha256(value.encode()).hexdigest()


def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()
    updates = fact_updates(args.root)
    if args.write:
        for name, value in updates.items():
            write_text(args.root, name, value)
    elif updates:
        raise SystemExit("Documentation facts are stale: " + ", ".join(updates))
    print("Documentation facts checked; " + str(len(updates)) + " document(s) updated.")


if __name__ == "__main__":
    main()
