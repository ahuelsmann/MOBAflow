"""Offline regressions for documentation drift, model output and publication boundaries."""

import json
import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import documentation_consistency as facts
import run_documentation_consistency as runner


CONTROL_ROOT = Path(__file__).resolve().parent.parent
WORKFLOW = CONTROL_ROOT / ".github/workflows/documentation-consistency.yml"


class RepositoryFixture:
    """Synthetic repository helpers shared by the test cases; this mixin defines no tests."""

    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        subprocess.run(["git", "init", "-q", str(self.root)], check=True)
        self.write(".gitattributes", "* text=auto eol=crlf\n")
        self.write("Directory.Packages.props", '<Project><ItemGroup>' + ''.join(
            f'<PackageVersion Include="{name}" Version="1.0" />' for name in
            ("SkiaSharp", "Microsoft.WindowsAppSDK", "Microsoft.Graphics.Win2D", "Xamarin.AndroidX.Startup.StartupRuntime", "Unused"))
            + '</ItemGroup></Project>')
        self.write("Directory.Build.props", "<Project />")
        self.write("App/App.csproj", '<Project><ItemGroup><PackageReference Include="SkiaSharp" /></ItemGroup></Project>')
        self.write(facts.PACKAGE_DOC, "# Notices\n\n| Package | Version | Directly referenced in | Purpose |\n| --- | --- | --- | --- |\n| `SkiaSharp` | `0.9` | `Old` | Known rendering purpose. |\n\nLicense prose is preserved.\n")
        self.write(facts.MUTATION_DOC, "# Build\n\n| Active lane | Other |\n| --- | --- |\n| domain | old |\n\nOther build guidance.\n")
        self.write("MutationTest/mutation-lanes.json", json.dumps({"lanes": [
            {"name": "domain", "status": "active", "production-projects": ["Domain/Domain.csproj"]},
            {"name": "backend", "status": "planned", "production-projects": ["Backend/Backend.csproj"]}]}))
        self.write("MutationTest/stryker-config.json", json.dumps({"stryker-config": {
            "project": "Domain.csproj", "test-projects": ["Domain.MutationTests.csproj"], "thresholds": {"break": 90}}}))
        self.write("docs/ARCHITECTURE.md", "# Architecture\n\nThe desktop owner is OldOwner.\n")
        self.write("App/Owner.cs", "public class NewOwner { }\n")
        self.base = self.commit()

    def write(self, name, value):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(value, encoding="utf-8")

    def commit(self):
        subprocess.run(["git", "-C", str(self.root), "add", "."], check=True, capture_output=True)
        subprocess.run(["git", "-C", str(self.root), "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                        "commit", "-q", "-m", "fixture"], check=True)
        return facts.git(self.root, "rev-parse", "HEAD").decode().strip()

    def regenerate(self):
        for name, value in facts.fact_updates(self.root).items():
            facts.write_text(self.root, name, value)

    def bundle(self):
        return {"sources": {"App/Owner.cs": "public class NewOwner { }\n"},
                "documents": {"docs/ARCHITECTURE.md": facts.text(self.root, "docs/ARCHITECTURE.md")}}

    def proposal(self):
        return {"status": "update_required", "summary": "Owner changed", "edits": [{
            "path": "docs/ARCHITECTURE.md", "old_text": "OldOwner", "new_text": "NewOwner",
            "evidence": [{"path": "App/Owner.cs", "quote": "public class NewOwner"}]}]}

    def publication(self, head):
        return {"head": head, "base": self.base, "autofix": True, "changes": [{
            "path": "docs/ARCHITECTURE.md", "before_sha256": facts.content_hash(facts.text(self.root, "docs/ARCHITECTURE.md")),
            "content": "# Architecture\n\nThe desktop owner is NewOwner.\n"}]}


class FactsTests(RepositoryFixture, unittest.TestCase):
    def test_inventory_uses_references_and_preserves_purpose(self):
        value = facts.package_table(self.root)
        self.assertIn("`SkiaSharp` | `1.0` | `App`", value)
        self.assertIn("Known rendering purpose", value)
        self.assertNotIn("Unused", value)

    def test_version_upgrade_is_detected_and_idempotent(self):
        self.regenerate()
        self.assertEqual({}, facts.fact_updates(self.root))
        path = self.root / "Directory.Packages.props"
        path.write_text(path.read_text().replace('Version="1.0"', 'Version="2.0"'))
        self.assertIn(facts.PACKAGE_DOC, facts.fact_updates(self.root))
        self.regenerate()
        self.assertEqual({}, facts.fact_updates(self.root))

    def test_mutation_threshold_and_planned_lane(self):
        value = facts.mutation_table(self.root)
        self.assertIn("90%", value)
        self.assertNotIn("backend", value)
        self.write("MutationTest/stryker-config.json", json.dumps({"stryker-config": {
            "project": "Domain.csproj", "test-projects": ["Domain.MutationTests.csproj"], "thresholds": {"break": 93}}}))
        self.assertIn("93%", facts.mutation_table(self.root))

    def test_missing_central_version_fails(self):
        self.write("App/App.csproj", '<Project><PackageReference Include="Unknown" /></Project>')
        with self.assertRaisesRegex(ValueError, "central version"):
            facts.package_table(self.root)

    def test_generated_regions_preserve_license_and_guidance(self):
        self.regenerate()
        self.assertIn("License prose is preserved", facts.text(self.root, facts.PACKAGE_DOC))
        self.assertIn("Other build guidance", facts.text(self.root, facts.MUTATION_DOC))

    def test_broken_region_is_rejected(self):
        self.write(facts.PACKAGE_DOC, facts.START.format(name="packages") + "\n")
        with self.assertRaisesRegex(ValueError, "Invalid generated"):
            facts.fact_updates(self.root)

    def test_no_xml_entities(self):
        self.write("App/App.csproj", '<!DOCTYPE x [<!ENTITY e "secret">]><Project />')
        with self.assertRaisesRegex(ValueError, "entities"):
            facts.package_table(self.root)

    def test_path_traversal_and_symlinks_are_rejected(self):
        for value in ("../outside.md", "/tmp/outside.md", "docs\\outside.md", "C:/outside.md"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                facts.safe_path(self.root, value)
        (self.root / "docs/link.md").symlink_to(self.root / "App/Owner.cs")
        with self.assertRaisesRegex(ValueError, "symlinks"):
            facts.text(self.root, "docs/link.md")

    def test_latest_docs_commit_prevents_rewrite_loop(self):
        self.write("App/Owner.cs", "public class NewOwner { public int Value; }\n")
        code_head = self.commit()
        self.assertTrue(facts.can_autofix(self.root, code_head))
        self.write("docs/ARCHITECTURE.md", "# Updated\n")
        docs_head = self.commit()
        self.assertFalse(facts.can_autofix(self.root, docs_head))
        self.assertIn("App/Owner.cs", facts.changed_paths(self.root, self.base, docs_head))

    def test_purpose_rows_keep_extra_cells_and_ignore_malformed_rows(self):
        rows = ("| `Kept` | `1` | `App` | Purpose with | a pipe |\n"
                "| `Short` | `1` | missing purpose |\n"
                "| Plain | `1` | `App` | Not a package cell |\n"
                "| `Tick`ed` | `1` | `App` | Nested backtick |\n")
        self.assertEqual({"Kept": "Purpose with | a pipe"}, facts.package_purposes(rows))

    def test_adversarial_purpose_row_is_parsed_in_linear_time(self):
        # A row that never closes previously caused quadratic regex backtracking.
        hostile = "| `SkiaSharp` | `1` | " + " |" * 60_000 + "x"
        self.write(facts.PACKAGE_DOC, "# Notices\n\n" + hostile + "\n")
        value = facts.package_table(self.root)
        self.assertIn("New direct dependency", value)


class ProposalTests(RepositoryFixture, unittest.TestCase):
    def test_source_grounded_replacement(self):
        changes = facts.validate_proposal(self.bundle(), self.proposal())
        self.assertIn("NewOwner", changes["docs/ARCHITECTURE.md"])

    def test_consistent_assessment_does_not_rewrite(self):
        self.assertEqual({}, facts.validate_proposal(self.bundle(), {"status": "consistent", "edits": []}))

    def test_uncertainty_blocks(self):
        bundle = self.bundle()
        with self.assertRaisesRegex(ValueError, "could not establish"):
            facts.validate_proposal(bundle, {"status": "uncertain", "edits": []})

    def test_source_quote_must_exist(self):
        bundle, proposal = self.bundle(), self.proposal()
        proposal["edits"][0]["evidence"][0]["quote"] = "invented API"
        with self.assertRaisesRegex(ValueError, "Evidence"):
            facts.validate_proposal(bundle, proposal)

    def test_ambiguous_replacement_blocks(self):
        bundle, proposal = self.bundle(), self.proposal()
        bundle["documents"]["docs/ARCHITECTURE.md"] += " OldOwner"
        with self.assertRaisesRegex(ValueError, "unique"):
            facts.validate_proposal(bundle, proposal)

    def test_code_and_instruction_edits_block(self):
        bundle = self.bundle()
        for name in ("App/Owner.cs", "AGENTS.md", "../README.md", ".github/workflows/quality.yml", "specs/011-x/spec.md", facts.PACKAGE_DOC):
            proposal = self.proposal()
            proposal["edits"][0]["path"] = name
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, "allowed document"):
                facts.validate_proposal(bundle, proposal)

    def test_generated_region_cannot_be_rewritten(self):
        bundle, proposal = self.bundle(), self.proposal()
        bundle["documents"]["docs/ARCHITECTURE.md"] = facts.START.format(name="x") + "\nOldOwner\n" + facts.END.format(name="x")
        with self.assertRaisesRegex(ValueError, "generated facts"):
            facts.validate_proposal(bundle, proposal)

    def test_each_generated_region_is_protected_separately(self):
        first = facts.START.format(name="a") + "\nA\n" + facts.END.format(name="a")
        protected = facts.START.format(name="b") + "\nOldOwner\n" + facts.END.format(name="b")
        second = facts.START.format(name="b") + "\nB\n" + facts.END.format(name="b")
        bundle, proposal = self.bundle(), self.proposal()
        bundle["documents"]["docs/ARCHITECTURE.md"] = first + "\nBetween\n" + protected
        with self.assertRaisesRegex(ValueError, "generated facts"):
            facts.validate_proposal(bundle, proposal)
        bundle["documents"]["docs/ARCHITECTURE.md"] = first + "\nOldOwner\n" + second
        changes = facts.validate_proposal(bundle, proposal)
        self.assertEqual(first + "\nNewOwner\n" + second, changes["docs/ARCHITECTURE.md"])

    def test_unterminated_markers_are_scanned_in_linear_time(self):
        # Many unmatched openings previously caused superlinear regex backtracking.
        hostile = (facts.MARKER_PREFIX + "x:start -->") * 4_000  # Below the document size limit.
        self.assertEqual([], list(facts.generated_spans(hostile)))
        bundle, proposal = self.bundle(), self.proposal()
        bundle["documents"]["docs/ARCHITECTURE.md"] = "OldOwner\n" + hostile
        changes = facts.validate_proposal(bundle, proposal)
        self.assertTrue(changes["docs/ARCHITECTURE.md"].startswith("NewOwner\n"))

    def test_missing_link_blocks(self):
        with self.assertRaisesRegex(ValueError, "missing/outside"):
            facts.verify_links(self.root, {"docs/ARCHITECTURE.md": "[missing](missing.md)"})

    def test_external_links_and_code_examples_are_not_executed(self):
        facts.verify_links(self.root, {"docs/ARCHITECTURE.md": '[API](https://example.invalid)\n```text\n[demo](missing.md)\n```\n'})


class BoundaryTests(RepositoryFixture, unittest.TestCase):
    def test_model_request_has_no_tools_or_credentials_in_input(self):
        response = {"status": "completed", "output": [{"type": "message", "content": [
            {"type": "output_text", "text": '{"status":"consistent","summary":"ok","edits":[]}'}]}]}
        with patch.object(runner, "request_json", return_value=response) as request:
            runner.call_model({"documents": {}}, "trusted prompt", runner.PROPOSAL_SCHEMA,
                              {"OPENAI_API_KEY": "fixture-not-a-real-key", "DOCS_AI_MODEL": "fixture-model"})
        payload = request.call_args.args[2]
        self.assertNotIn("tools", payload)
        self.assertNotIn("fixture-not-a-real-key", payload["input"])
        self.assertFalse(payload["store"])

    def test_missing_credentials_fail_without_request(self):
        with patch.object(runner, "request_json") as request, self.assertRaisesRegex(ValueError, "configuration"):
            runner.call_model({}, "prompt", {}, {})
        request.assert_not_called()

    def test_incomplete_model_output_fails(self):
        with patch.object(runner, "request_json", return_value={"status": "incomplete"}), self.assertRaisesRegex(ValueError, "incomplete"):
            runner.call_model({}, "prompt", {}, {"OPENAI_API_KEY": "fixture", "DOCS_AI_MODEL": "fixture"})

    def test_publication_hash_prevents_stale_document(self):
        proposal = self.publication(self.base)
        self.write("docs/ARCHITECTURE.md", "A human has edited this document.\n")
        with self.assertRaisesRegex(ValueError, "differs"):
            runner.validate_publication(self.root, proposal)

    def test_no_updates_after_docs_only_commit(self):
        proposal = self.publication(self.base)
        proposal["autofix"] = False
        with self.assertRaisesRegex(ValueError, "documentation only"):
            runner.validate_publication(self.root, proposal)

    def test_changed_pr_head_blocks_before_writing(self):
        api = unittest.mock.Mock()
        api.call.return_value = {"state": "open", "head": {"sha": "another-head"}, "base": {"sha": self.base}}
        before = facts.text(self.root, "docs/ARCHITECTURE.md")
        proposal = self.publication(self.base)
        with self.assertRaisesRegex(ValueError, "PR changed"):
            runner.publish(self.root, proposal, "owner/repo", 1, "fixture", api)
        self.assertEqual(before, facts.text(self.root, "docs/ARCHITECTURE.md"))

    def test_fork_blocks_before_writing(self):
        api = unittest.mock.Mock()
        api.call.return_value = {"state": "open", "head": {"sha": self.base, "repo": {"full_name": "fork/repo"}},
                                 "base": {"sha": self.base}, "author_association": "OWNER"}
        proposal = self.publication(self.base)
        with self.assertRaisesRegex(ValueError, "same-repository"):
            runner.publish(self.root, proposal, "owner/repo", 1, "fixture", api)

    def test_main_branch_publication_blocks(self):
        api = unittest.mock.Mock()
        api.call.return_value = {"state": "open", "head": {"sha": self.base, "repo": {"full_name": "owner/repo"}, "ref": "main"},
                                 "base": {"sha": self.base}, "author_association": "OWNER"}
        proposal = self.publication(self.base)
        with self.assertRaisesRegex(ValueError, "never main"):
            runner.publish(self.root, proposal, "owner/repo", 1, "fixture", api)

    def test_independent_review_failure_blocks_proposal(self):
        self.regenerate()
        self.write("App/Owner.cs", "public class NewOwner { public int Value; }\n")
        head = self.commit()
        results = iter([self.proposal(), {"status": "inconsistent", "summary": "Wrong ownership", "findings": ["Mismatch"]}])
        with self.assertRaisesRegex(ValueError, "Independent"):
            runner.assess(self.root, self.base, head, CONTROL_ROOT, {}, model_call=lambda *args: next(results), scanner=lambda _: None)

    def test_independent_review_receives_candidate_without_writer_reasoning(self):
        self.regenerate()
        self.write("App/Owner.cs", "public class NewOwner { public int Value; }\n")
        head = self.commit()
        calls = []
        def model(bundle, prompt, schema, environment):
            calls.append(bundle)
            if len(calls) == 1:
                return self.proposal()
            return {"status": "consistent", "summary": "ok", "findings": []}
        result = runner.assess(self.root, self.base, head, CONTROL_ROOT, {}, model_call=model, scanner=lambda _: None)
        self.assertIn("NewOwner", calls[1]["documents"]["docs/ARCHITECTURE.md"])
        self.assertNotIn("summary", calls[1])
        self.assertEqual(1, len(result["changes"]))

    def test_scanner_failure_prevents_model_call(self):
        self.regenerate()
        self.write("App/Owner.cs", "public class NewOwner { public int Value; }\n")
        head = self.commit()
        model = unittest.mock.Mock()
        scanner = unittest.mock.Mock(side_effect=ValueError("scanner rejected input"))
        with self.assertRaisesRegex(ValueError, "scanner"):
            runner.assess(self.root, self.base, head, CONTROL_ROOT, {}, model_call=model, scanner=scanner)
        model.assert_not_called()

    def test_scanner_failure_is_redacted(self):
        with patch.object(runner.subprocess, "run", return_value=subprocess.CompletedProcess([], 1)):
            with self.assertRaisesRegex(ValueError, "Secrets scan failed"):
                runner.scan({"content": "fixture"})

    def test_duplicate_publication_path_blocks(self):
        proposal = self.publication(self.base)
        proposal["changes"].append(dict(proposal["changes"][0]))
        with self.assertRaisesRegex(ValueError, "duplicate"):
            runner.validate_publication(self.root, proposal)

    def test_notice_license_cannot_be_changed(self):
        proposal = self.publication(self.base)
        proposal["changes"] = [{"path": facts.PACKAGE_DOC,
                                "before_sha256": facts.content_hash(facts.text(self.root, facts.PACKAGE_DOC)),
                                "content": facts.fact_updates(self.root)[facts.PACKAGE_DOC].replace(
                                    "License prose is preserved", "Changed license claim")}]
        with self.assertRaisesRegex(ValueError, "deterministic generation exactly"):
            runner.validate_publication(self.root, proposal)

    def test_publication_push_has_lease_and_no_token_in_arguments(self):
        self.regenerate()
        head = self.commit()
        proposal = self.publication(head)
        api = unittest.mock.Mock()
        api.call.return_value = {"state": "open", "head": {"sha": head, "repo": {"full_name": "owner/repo"}, "ref": "task"},
                                 "base": {"sha": self.base}, "author_association": "OWNER"}
        commands = []
        original = subprocess.run
        def execute(command, **kwargs):
            if "push" in command:
                commands.append(command)
                return subprocess.CompletedProcess(command, 0)
            return original(command, **kwargs)
        with patch.object(runner, "scan"), patch.object(runner.subprocess, "run", side_effect=execute):
            commit = runner.publish(self.root, proposal, "owner/repo", 1, "fixture-secret", api)
        self.assertNotEqual(head, commit)
        self.assertIn("--force-with-lease=refs/heads/task:" + head, commands[0])
        self.assertNotIn("fixture-secret", " ".join(commands[0]))


class EligibilityTests(RepositoryFixture, unittest.TestCase):
    """The workflow skips the head checkout for these cases, so the subject must stay unused."""

    def pull_request(self, repository="owner/repo", association="OWNER"):
        return {"head": {"sha": self.base, "repo": {"full_name": repository}}, "base": {"sha": self.base},
                "author_association": association}

    def event(self):
        return {"pull_request": {"head": {"sha": self.base}, "base": {"sha": self.base}}}

    def evaluate(self, environment, pr):
        missing = self.root / "absent-subject"
        with patch.dict(os.environ, environment, clear=True), patch.object(runner, "assess") as assess:
            result = runner.assessment_result(missing, CONTROL_ROOT, self.event(), pr, "owner/repo")
        return result, assess

    def test_inactive_default_does_not_read_the_subject(self):
        result, assess = self.evaluate({}, self.pull_request())
        self.assertEqual("neutral", result["conclusion"])
        assess.assert_not_called()

    def test_fork_and_untrusted_authors_do_not_read_the_subject(self):
        for pr in (self.pull_request(repository="fork/repo"), self.pull_request(association="CONTRIBUTOR")):
            with self.subTest(pr=pr):
                result, assess = self.evaluate({"DOCS_AI_ENABLED": "true"}, pr)
                self.assertEqual("action_required", result["conclusion"])
                self.assertEqual([], result["changes"])
                assess.assert_not_called()

    def test_trusted_pr_without_checked_out_subject_fails_closed(self):
        environment = {"DOCS_AI_ENABLED": "true", "OPENAI_API_KEY": "fixture", "DOCS_AI_MODEL": "fixture"}
        missing = self.root / "absent-subject"
        with patch.dict(os.environ, environment, clear=True):
            result = runner.assessment_result(missing, CONTROL_ROOT, self.event(), self.pull_request(), "owner/repo")
        self.assertEqual("failure", result["conclusion"])
        self.assertFalse(result["changes"])


class WorkflowBoundaryTests(unittest.TestCase):
    """Structural trust-boundary checks of the privileged target-event workflow."""

    TRUSTED_GUARD = ("github.event.pull_request.head.repo.full_name == github.repository",
                     "contains(fromJSON('[\"OWNER\", \"MEMBER\", \"COLLABORATOR\"]'), github.event.pull_request.author_association)")

    @classmethod
    def setUpClass(cls):
        value = WORKFLOW.read_text(encoding="utf-8").replace("\r\n", "\n")
        cls.steps = re.split(r"\n(?=      - name: )", value)[1:]

    def checkouts(self):
        return [step for step in self.steps if "uses: actions/checkout@" in step]

    def test_every_checkout_drops_credentials(self):
        self.assertEqual(4, len(self.checkouts()))
        for step in self.checkouts():
            with self.subTest(step=step.splitlines()[0]):
                self.assertIn("persist-credentials: false", step)

    def test_controls_use_the_trusted_default_checkout(self):
        controls = [step for step in self.checkouts() if "path: controls" in step]
        self.assertEqual(2, len(controls))
        for step in controls:
            self.assertNotIn("ref:", step)

    def test_pr_heads_are_checked_out_only_for_trusted_same_repository_prs(self):
        heads = [step for step in self.checkouts() if "github.event.pull_request.head" in step and "path: subject" in step]
        self.assertEqual(2, len(heads))
        for step in heads:
            for guard in self.TRUSTED_GUARD:
                with self.subTest(step=step.splitlines()[0], guard=guard):
                    self.assertIn(guard, step)
        assess = next(step for step in heads if "Check out subject as data" in step)
        self.assertIn("vars.DOCS_AI_ENABLED == 'true'", assess)

    def test_only_trusted_controls_are_executed(self):
        commands = [line.strip() for step in self.steps for line in step.splitlines() if "python3" in line]
        self.assertTrue(commands)
        for command in commands:
            self.assertTrue(command.startswith("python3 controls/scripts/"), command)


if __name__ == "__main__":
    unittest.main()
