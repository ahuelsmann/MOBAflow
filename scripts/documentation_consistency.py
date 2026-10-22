"""Deterministic documentation facts; no model calls or repository code execution."""

import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath


PACKAGE_DOC = "docs/THIRD-PARTY-NOTICES.md"
MUTATION_DOC = "docs/BUILD-PERFORMANCE.md"
PACKAGE_CATALOG = "Directory.Packages.props"
MUTATION_REGISTRY = "MutationTest/mutation-lanes.json"
PROJECT_SUFFIX = ".csproj"
MARKER_PREFIX = "<!-- documentation-facts:"
START = MARKER_PREFIX + "{name}:start -->"
END = MARKER_PREFIX + "{name}:end -->"
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


def central_versions(root):
    return {item.attrib["Include"]: item.attrib["Version"]
            for item in xml(root, PACKAGE_CATALOG).iter("PackageVersion")}


def package_references(root, versions):
    references = {}
    for name in tracked(root):
        if not (name.endswith(PROJECT_SUFFIX) or name in ("Directory.Build.props", "Directory.Build.targets")):
            continue
        for item in xml(root, name).iter("PackageReference"):
            package = item.attrib.get("Include")
            if not package:
                continue  # Update changes metadata on an existing reference.
            if package not in versions:
                raise ValueError("Package lacks a central version: " + package)
            scope = str(PurePosixPath(name).parent) if name.endswith(PROJECT_SUFFIX) else "Build-wide via `" + name + "`"
            references.setdefault(package, set()).add(scope)
    return references


def code_cell(cell):
    """Return the value of a single inline-code table cell, or None."""
    if len(cell) < 3 or not cell.startswith("`") or not cell.endswith("`") or "`" in cell[1:-1]:
        return None
    return cell[1:-1]


def package_purposes(value):
    # Split rows linearly: package, version, usage, then the purpose with any remaining cells.
    purposes = {}
    for line in value.split("\n"):
        if not line.startswith("| ") or not line.endswith(" |"):
            continue
        cells = line[2:-2].split(" | ", 3)
        if len(cells) == 4 and code_cell(cells[0]) and code_cell(cells[1]) and cells[3]:
            purposes[code_cell(cells[0])] = cells[3]
    return purposes


def package_table(root):
    versions = central_versions(root)
    references = package_references(root, versions)
    purposes = package_purposes(text(root, PACKAGE_DOC))
    rows = ["| Package | Version | Directly referenced in | Purpose |", "| --- | --- | --- | --- |"]
    for package in sorted(references, key=str.casefold):
        scope = ", ".join(item if item.startswith("Build-wide") else "`" + item + "`"
                          for item in sorted(references[package]))
        purpose = purposes.get(package, "New direct dependency; purpose and upstream license require review.")
        rows.append(f"| `{package}` | `{versions[package]}` | {scope} | {purpose} |")
    return "\n".join(rows)


def mutation_table(root):
    lanes = json.loads(text(root, MUTATION_REGISTRY))["lanes"]
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
    versions = central_versions(root)
    notices = changes.get(PACKAGE_DOC, text(root, PACKAGE_DOC))
    for package in ("SkiaSharp", "Microsoft.WindowsAppSDK", "Microsoft.Graphics.Win2D", "Xamarin.AndroidX.Startup.StartupRuntime"):
        notices = re.sub(r"`" + re.escape(package) + r" [^`]+` in `Directory.Packages.props`",
                         "`" + package + " " + versions[package] + "` in `Directory.Packages.props`", notices)
    if notices != text(root, PACKAGE_DOC):
        changes[PACKAGE_DOC] = notices
    return changes


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
