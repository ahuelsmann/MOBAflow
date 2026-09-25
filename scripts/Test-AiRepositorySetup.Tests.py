"""Behavioral fixtures for AI setup, remote selection and prompt hook isolation."""
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.dont_write_bytecode = True
PWSH = shutil.which("pwsh")
GIT = shutil.which("git")
spec = importlib.util.spec_from_file_location("setup_check", ROOT / "scripts/Test-AiRepositorySetup.py")
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


def run(args, cwd, env=None, input_text=None):
    clean = os.environ.copy()
    for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR"):
        clean.pop(key, None)
    if env:
        clean.update(env)
    return subprocess.run(args, cwd=cwd, env=clean, input=input_text, text=True,
                          capture_output=True, timeout=30, check=False, shell=isinstance(args, str))


class StructureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="mobaflow-ai-")
        self.root = Path(self.temp.name)
        # Copy only active development inputs, never operator data or credentials.
        for folder in (".agents", ".codex", ".specify", ".github/instructions"):
            shutil.copytree(ROOT / folder, self.root / folder)
        for name in ("AGENTS.md", ".mcp.json", ".github/copilot-instructions.md"):
            target = self.root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, target)
        # Link targets are represented without copying product/user contents.
        for directory in ("docs", "SharedUI", "MOBAflow", "Backend", "scripts", "Test", "Common", "MOBAsmart"):
            for source in (ROOT / directory).rglob("*"):
                if source.is_file() and not any(p in ("bin", "obj", ".nuget") for p in source.parts):
                    target = self.root / source.relative_to(ROOT)
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.touch()
        for name in ("README.md", "CONTRIBUTING.md", "Moba.slnx", ".editorconfig", "Moba.sln.DotSettings", "LICENSE"):
            (self.root / name).touch()
        for source in (ROOT / ".github/workflows").glob("*"):
            target = self.root / source.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.touch()
        for name in ("docs/AI-DEVELOPMENT.md", "docs/SPEC-KIT.md"):
            shutil.copyfile(ROOT / name, self.root / name)
        (self.root / "skills").mkdir(exist_ok=True)
        (self.root / "skills/README.md").touch()
        (self.root / "specs/005-ai-repo-setup").mkdir(parents=True, exist_ok=True)
        (self.root / "specs/005-ai-repo-setup/pipeline-review.md").touch()

    def tearDown(self):
        self.temp.cleanup()

    def test_real_setup(self):
        self.assertEqual([], checker.validate(ROOT))

    def test_valid_fixture(self):
        self.assertEqual([], checker.validate(self.root))

    def test_malformed_toml(self):
        (self.root / ".codex/config.toml").write_text("[broken", encoding="utf-8")
        self.assertTrue(any("Invalid setup" in x for x in checker.validate(self.root)))

    def test_malformed_json(self):
        (self.root / ".mcp.json").write_text("{", encoding="utf-8")
        self.assertTrue(any("Invalid setup" in x for x in checker.validate(self.root)))

    def test_duplicate_skill(self):
        path = self.root / ".agents/skills/mobaflow-code-review/SKILL.md"
        path.write_text(path.read_text().replace("name: mobaflow-code-review", "name: mobaflow-diagnosing-bugs"))
        self.assertTrue(any("skill name" in x for x in checker.validate(self.root)))

    def test_missing_provenance(self):
        path = self.root / ".agents/skills/sources.json"
        data = json.loads(path.read_text())
        data["skills"] = [s for s in data["skills"] if s["name"] != "mobaflow-code-review"]
        path.write_text(json.dumps(data))
        self.assertTrue(any("source" in x for x in checker.validate(self.root)))

    def test_broken_link(self):
        path = self.root / "AGENTS.md"
        path.write_text(path.read_text() + "\n[missing](does-not-exist.md)\n")
        self.assertTrue(any("does-not-exist" in x for x in checker.validate(self.root)))

    def test_fenced_example_is_not_link(self):
        path = self.root / "AGENTS.md"
        path.write_text(path.read_text() + "\n```text\n[example](does-not-exist.md)\n```\n")
        self.assertFalse(any("does-not-exist" in x for x in checker.validate(self.root)))

    def test_fixed_checkout_rejected(self):
        data = {"mcpServers": {"files": {"command": "npx", "args": ["C:\\Other\\checkout"]}}}
        (self.root / ".mcp.json").write_text(json.dumps(data))
        self.assertTrue(any("machine-specific" in x for x in checker.validate(self.root)))

    def test_invalid_skill_metadata(self):
        path = self.root / ".agents/skills/mobaflow-code-review/SKILL.md"
        path.write_text("# No metadata\n")
        self.assertTrue(any("Invalid skill metadata" in x for x in checker.validate(self.root)))

    def test_empty_hook_rejected(self):
        (self.root / ".codex/hooks.json").write_text('{"hooks": {}}')
        self.assertTrue(any("hook is missing" in x for x in checker.validate(self.root)))

    def test_invalid_mcp_command_and_url_types(self):
        for field, value in (("command", 42), ("url", True), ("command", "  ")):
            with self.subTest(field=field, value=value):
                errors = []
                checker.check_server("fixture", {field: value}, errors)
                self.assertTrue(any("nonempty string" in x for x in errors))

    def test_invalid_hook_command_types(self):
        for field in ("command", "commandWindows"):
            with self.subTest(field=field):
                errors = []
                handler = {"type": "command", "command": "pwsh", field: 42}
                checker.check_hook(handler, errors)
                self.assertTrue(any(field in x for x in errors))


class GitTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="mobaflow worktree ")
        self.root = Path(self.temp.name) / "main"
        self.root.mkdir()
        self.git("init", "--quiet")
        self.git("config", "user.name", "Fixture")
        self.git("config", "user.email", "fixture@example.invalid")
        self.git("commit", "--allow-empty", "-m", "fixture")

    def tearDown(self):
        self.temp.cleanup()

    def git(self, *args):
        result = run([GIT, *args], self.root)
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout.strip()

    def resolve(self, root=None):
        return run([PWSH, "-NoProfile", "-File", str(ROOT / "scripts/Resolve-GitHubRepository.ps1"),
                    "-RepositoryRoot", str(root or self.root)], self.root)

    def test_supported_urls_and_remote_names(self):
        for name, url in (("github", "https://github.com/owner/repo.git"),
                          ("origin", "git@github.com:owner/repo.git"),
                          ("upstream", "ssh://git@github.com/owner/repo")):
            with self.subTest(url=url):
                self.git("remote", "add", name, url)
                result = self.resolve()
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual("owner/repo", json.loads(result.stdout)["repository"])
                self.git("remote", "remove", name)

    def test_missing_and_ambiguous(self):
        self.assertNotEqual(0, self.resolve().returncode)
        self.git("remote", "add", "one", "https://github.com/owner/one")
        self.git("remote", "add", "two", "https://github.com/owner/two")
        self.assertNotEqual(0, self.resolve().returncode)

    def test_tracking_precedence_and_foreign_tracking(self):
        self.git("remote", "add", "one", "https://github.com/owner/one")
        self.git("remote", "add", "two", "https://github.com/owner/two")
        branch = self.git("branch", "--show-current")
        self.git("config", f"branch.{branch}.remote", "two")
        self.assertEqual("owner/two", json.loads(self.resolve().stdout)["repository"])
        self.git("remote", "set-url", "two", "https://example.invalid/owner/two")
        self.assertNotEqual(0, self.resolve().returncode)

    def test_local_tracking_uses_unique_github_remote(self):
        self.git("remote", "add", "github", "https://github.com/owner/repo")
        base = self.git("branch", "--show-current")
        self.git("checkout", "--quiet", "-b", "feature", "--track", base)
        self.assertEqual(".", self.git("config", "--get", "branch.feature.remote"))
        result = self.resolve()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("owner/repo", json.loads(result.stdout)["repository"])
        self.git("remote", "add", "other", "https://github.com/owner/other")
        self.assertNotEqual(0, self.resolve().returncode)

    def test_worktree_root_and_marker_isolation(self):
        self.git("remote", "add", "github", "https://github.com/owner/repo")
        second = Path(self.temp.name) / "second worktree"
        self.git("worktree", "add", "--quiet", "-b", "fixture-second", str(second))
        (self.root / "marker").write_text("main")
        (second / "marker").write_text("second")
        for folder, marker in ((self.root, "main"), (second, "second")):
            nested = folder / "nested"
            nested.mkdir()
            result = self.resolve(nested)
            self.assertEqual(0, result.returncode, result.stderr)
            resolved = Path(json.loads(result.stdout)["root"])
            self.assertEqual(folder.resolve(), resolved.resolve())
            self.assertEqual(marker, (resolved / "marker").read_text())

    def test_hook_missing_tool_and_failure(self):
        hook = Path(".codex/hooks/sonar-secrets/build-scripts/prompt-secrets.ps1")
        target = self.root / hook
        target.parent.mkdir(parents=True)
        shutil.copyfile(ROOT / hook, target)
        (self.root / "AGENTS.md").write_text("fixture")
        result = run([PWSH, "-NoProfile", "-File", str(target)], self.root, {"PATH": ""}, "{}")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("unavailable", json.loads(result.stdout)["systemMessage"])
        fake = self.root / "fake"
        fake.mkdir()
        if os.name == "nt":
            (fake / "sonar.cmd").write_text('@echo off\necho %CD%\nexit /b 2\n')
        else:
            launcher = fake / "sonar"
            launcher.write_text('#!/bin/sh\npwd\nexit 2\n')
            launcher.chmod(0o755)
        nested = self.root / "nested"
        nested.mkdir()
        result = run([PWSH, "-NoProfile", "-File", str(target)], nested, {"PATH": str(fake)}, "{}")
        self.assertEqual(2, result.returncode, result.stderr)
        self.assertIn(str(self.root), result.stdout)
        self.assertIn("did not pass", result.stderr)

    def test_configured_hook_launch_from_subdirectory(self):
        hook = Path(".codex/hooks/sonar-secrets/build-scripts/prompt-secrets.ps1")
        target = self.root / hook
        target.parent.mkdir(parents=True)
        shutil.copyfile(ROOT / hook, target)
        (self.root / "AGENTS.md").write_text("fixture")
        fake = self.root / "fake"
        fake.mkdir()
        if os.name == "nt":
            (fake / "sonar.cmd").write_text('@echo off\necho %CD%\nexit /b 0\n')
        else:
            launcher = fake / "sonar"
            launcher.write_text('#!/bin/sh\npwd\nexit 0\n')
            launcher.chmod(0o755)
        nested = self.root / "nested"
        nested.mkdir()
        config = json.loads((ROOT / ".codex/hooks.json").read_text())
        handler = config["hooks"]["UserPromptSubmit"][0]["hooks"][0]
        command = handler["commandWindows" if os.name == "nt" else "command"]
        result = run(command, nested, {"PATH": str(fake) + os.pathsep + os.environ["PATH"]}, "{}")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn(str(self.root), result.stdout)

    def test_specify_feature_pointer_and_template_override(self):
        folder = self.root / ".specify/scripts/powershell"
        folder.mkdir(parents=True)
        common = folder / "common.ps1"
        shutil.copyfile(ROOT / ".specify/scripts/powershell/common.ps1", common)
        pointer = self.root / ".specify/feature.json"
        original = '{"feature_directory":"specs/original"}'
        pointer.write_text(original)
        override = self.root / ".specify/templates/overrides"
        override.mkdir(parents=True)
        (override / "plan-template.md").write_text("fixture override")
        command = ". '" + str(common).replace("'", "''") + "'; Get-FeaturePathsEnv | Out-Null; Resolve-TemplateContent -TemplateName plan-template -RepoRoot (Get-Location).Path"
        result = run([PWSH, "-NoProfile", "-Command", command], self.root,
                     {"SPECIFY_FEATURE_DIRECTORY": "specs/other", "SPECIFY_FEATURE_NO_PERSIST": "1"})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(original, pointer.read_text())
        self.assertEqual("fixture override", result.stdout.strip())


if __name__ == "__main__":
    if not PWSH or not GIT:
        raise SystemExit("Git and PowerShell 7 are required; tests cannot be reported as passed.")
    unittest.main()
