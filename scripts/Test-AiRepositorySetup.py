"""Offline structural checks for the active AI repository setup (Python 3.11+)."""
import argparse
import json
import re
import sys
import tomllib
from pathlib import Path
from urllib.parse import unquote, urlsplit


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def without_fences(text):
    return re.sub(r"(?ms)^\s*`{3,}[^\n]*\n.*?^\s*`{3,}\s*$", "", text)


def check_links(root, path, errors):
    text = without_fences(path.read_text(encoding="utf-8-sig"))
    for target in re.findall(r"\[[^\]\n]+\]\(([^)]+)\)", text):
        target = target.strip().split(' "', 1)[0].strip("<>")
        parsed = urlsplit(target)
        if parsed.scheme or target.startswith("#") or not parsed.path:
            continue
        if any(marker in target for marker in ("*", "$", "[", "{", "<")):
            continue  # Template placeholders are not concrete file references.
        resolved = (path.parent / unquote(parsed.path)).resolve()
        if not resolved.is_relative_to(root) or not resolved.exists():
            errors.append(f"{path.relative_to(root)}: missing/outside link {target}")


def nonempty_string(value):
    return isinstance(value, str) and bool(value.strip())


def check_server(name, server, errors):
    if not isinstance(server, dict):
        errors.append(f"MCP {name}: expected an object")
        return
    if not (nonempty_string(server.get("command")) or nonempty_string(server.get("url"))):
        errors.append(f"MCP {name}: missing command/url")
    for field in ("command", "url"):
        if field in server and not nonempty_string(server[field]):
            errors.append(f"MCP {name}: {field} must be a nonempty string")
    arguments = server.get("args", [])
    if not isinstance(arguments, list) or any(not isinstance(v, str) for v in arguments):
        errors.append(f"MCP {name}: args must be strings")
        return
    if any(re.match(r"^[A-Za-z]:[\\\\/]", value) for value in arguments):
        errors.append(f"MCP {name}: machine-specific argument")


def check_hook(hook, errors):
    if hook.get("type") != "command" or not nonempty_string(hook.get("command")):
        errors.append("Invalid prompt hook command")
    if "commandWindows" in hook and not nonempty_string(hook["commandWindows"]):
        errors.append("Invalid prompt hook commandWindows")


def check_configuration(root, errors):
    config = tomllib.loads((root / ".codex/config.toml").read_text(encoding="utf-8-sig"))
    mcp = load_json(root / ".mcp.json")
    for servers in (config.get("mcp_servers", {}), mcp.get("mcpServers")):
        if not isinstance(servers, dict):
            errors.append("MCP servers must be an object")
            continue
        for name, server in servers.items():
            check_server(name, server, errors)
    hooks = load_json(root / ".codex/hooks.json")
    groups = hooks.get("hooks", {}).get("UserPromptSubmit", [])
    if not groups:
        errors.append("Prompt secrets hook is missing")
    for group in groups:
        handlers = group.get("hooks", [])
        if not handlers:
            errors.append("Prompt hook has no handlers")
        for hook in handlers:
            check_hook(hook, errors)


def check_source_entry(root, entry, errors):
    fields = ("name", "path", "source", "revision", "license", "localChanges")
    if not all(isinstance(entry.get(key), str) and entry[key] for key in fields):
        errors.append("Incomplete skill source record")
        return False
    for field in ("path", "manifest"):
        if not entry.get(field):
            continue
        target = (root / entry[field]).resolve()
        if not target.is_relative_to(root) or not target.exists():
            errors.append(f"Invalid source {field}: {entry['name']}")
    if "/" in entry["license"] and not (root / entry["license"]).is_file():
        errors.append(f"Missing license: {entry['name']}")
    return True


def source_records(root, errors):
    registry = load_json(root / ".agents/skills/sources.json")
    if registry.get("schemaVersion") != 1 or not registry.get("reviewedAt"):
        errors.append("Invalid source registry header")
    records = {}
    if not registry.get("skills"):
        errors.append("Source registry has no active skills")
    for entry in registry.get("skills", []):
        if not check_source_entry(root, entry, errors):
            continue
        if entry["name"] in records:
            errors.append(f"Duplicate source name: {entry['name']}")
        records[entry["name"]] = entry
    return records


def skill_name(path):
    text = path.read_text(encoding="utf-8-sig")
    front = re.match(r"\A---\s*\n(.*?)\n---", text, re.S)
    if not front or not re.search(r"(?m)^description:\s*\S+", front[1]):
        return None
    name = re.search(r'^name:\s*["\x27]?([a-z0-9-]+)["\x27]?\s*$', front[1], re.M)
    return name[1] if name else None


def check_skills(root, errors):
    records = source_records(root, errors)
    seen = set()
    for path in (root / ".agents/skills").glob("*/SKILL.md"):
        name = skill_name(path)
        if not name:
            errors.append(f"Invalid skill metadata: {path.parent.name}")
            continue
        if name in seen or name != path.parent.name:
            errors.append(f"Duplicate or mismatched skill name: {name}")
        seen.add(name)
        record = records.get(name)
        if not record or record["path"] != path.parent.relative_to(root).as_posix():
            errors.append(f"Missing/mismatched source: {name}")
        check_links(root, path, errors)
    if seen != set(records):
        errors.append("Source registry and active skills differ")


def check_guidance(root, errors):
    # The index declares active instructions; historical references stay outside this traversal.
    index = root / ".github/instructions/instructions-index.md"
    index_text = index.read_text(encoding="utf-8-sig").split("## Further references")[0]
    guidance = {root / "AGENTS.md", root / ".github/copilot-instructions.md", index,
                root / "docs/AI-DEVELOPMENT.md", root / "docs/SPEC-KIT.md"}
    for link in re.findall(r"\]\(([^)]+\.instructions\.md)\)", index_text):
        guidance.add((index.parent / link).resolve())
    for path in guidance:
        if not path.is_relative_to(root) or not path.is_file():
            errors.append(f"Missing/outside active guidance: {path.name}")
        else:
            check_links(root, path, errors)


def validate(root):
    root = Path(root).resolve()
    errors = []
    try:
        check_configuration(root, errors)
        check_skills(root, errors)
        check_guidance(root, errors)
    except (OSError, ValueError, TypeError, KeyError, AttributeError) as exc:
        errors.append(f"Invalid setup: {exc}")
    return errors
def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    errors = validate(args.root)
    for error in errors:
        print(error, file=sys.stderr)
    if not errors:
        print("AI repository setup structure passed (offline; no live service or agent claim).")
    return bool(errors)


if __name__ == "__main__":
    sys.exit(main())
