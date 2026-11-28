// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// Release Studio ships the Windows package, so its supply-chain evidence must stay intact: pinned actions,
/// narrowly scoped permissions, and an SBOM plus build provenance bound to the shipped assets.
/// </summary>
[TestFixture]
internal sealed partial class ReleaseWorkflowArchitectureTests
{
    private const string WorkflowPath = ".github/workflows/release-studio.yml";

    [Test]
    public void EveryAction_IsPinnedToFullCommitSha()
    {
        var unpinned = UsesLine().Matches(ReadWorkflow())
            .Select(match => match.Groups["ref"].Value)
            .Where(reference => !FullShaWithVersion().IsMatch(reference))
            .ToList();

        Assert.That(unpinned, Is.Empty, "Release Studio actions must use a 40-character SHA with a version comment.");
    }

    [Test]
    public void Permissions_AreReadOnlyByDefault_AndAttestationScopesStayOnTheJob()
    {
        var workflow = ReadWorkflow();
        var topLevel = workflow[..workflow.IndexOf("\njobs:", StringComparison.Ordinal)];
        var job = workflow[workflow.IndexOf("\njobs:", StringComparison.Ordinal)..];

        Assert.Multiple(() =>
        {
            Assert.That(topLevel, Does.Contain("permissions:\n  contents: read"));
            Assert.That(topLevel, Does.Not.Contain("id-token").And.Not.Contain("attestations"));
            Assert.That(job, Does.Contain("      contents: write"));
            Assert.That(job, Does.Contain("      id-token: write"));
            Assert.That(job, Does.Contain("      attestations: write"));
        });
    }

    [Test]
    public void Provenance_CoversZipAndSbom_AfterValidation_AndBeforeUpload()
    {
        var workflow = ReadWorkflow();
        var validate = workflow.IndexOf("name: Validate SBOM binding", StringComparison.Ordinal);
        var attest = workflow.IndexOf("actions/attest-build-provenance@", StringComparison.Ordinal);
        var upload = workflow.IndexOf("actions/upload-artifact@", StringComparison.Ordinal);
        var draft = workflow.IndexOf("name: Create or update draft GitHub Release", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(validate, Is.GreaterThan(0));
            Assert.That(attest, Is.GreaterThan(validate), "attest after SBOM validation");
            Assert.That(upload, Is.GreaterThan(attest));
            Assert.That(draft, Is.GreaterThan(upload));
            Assert.That(workflow, Does.Contain("artifacts/MOBAflow-${{ inputs.tag }}-win-x64.zip\n            artifacts/MOBAflow-${{ inputs.tag }}-win-x64.spdx.json"));
        });
    }

    [Test]
    public void SbomAndProvenance_AreChecksummedAndAttachedToTheDraftRelease()
    {
        var workflow = ReadWorkflow();
        var assets = workflow[workflow.IndexOf("$assets = @(", StringComparison.Ordinal)..];

        Assert.Multiple(() =>
        {
            Assert.That(workflow, Does.Contain("-win-x64.spdx.json\")) {"), "SBOM is part of SHA256SUMS.txt");
            Assert.That(assets, Does.Contain("-win-x64.spdx.json"));
            Assert.That(assets, Does.Contain("-win-x64.provenance.sigstore.json"));
            Assert.That(assets, Does.Contain("SHA256SUMS.txt"));
            Assert.That(workflow, Does.Contain("is already published. Release Studio will not modify it."));
        });
    }

    private static string ReadWorkflow()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new InvalidOperationException("Moba.slnx was not found above the test directory.");
        return File.ReadAllText(Path.Combine(root, WorkflowPath)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*-?\s*uses:\s*(?<ref>\S+.*)$", RegexOptions.Multiline)]
    private static partial Regex UsesLine();

    [GeneratedRegex(@"^[\w.-]+/[\w./-]+@[0-9a-f]{40}\s+#\s*v\d")]
    private static partial Regex FullShaWithVersion();
}
