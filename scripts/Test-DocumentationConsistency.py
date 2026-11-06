"""Offline regressions for source-derived documentation facts and file boundaries."""

import json
import subprocess
import tempfile
import unittest
from pathlib import Path

import documentation_consistency as facts


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
        self.commit()

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



if __name__ == "__main__":
    unittest.main()
