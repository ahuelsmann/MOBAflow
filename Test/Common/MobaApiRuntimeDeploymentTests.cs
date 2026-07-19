// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;

/// <summary>
/// Tests for isolated MOBApi runtime deployments that keep build output files writable.
/// </summary>
[TestFixture]
internal sealed class MobaApiRuntimeDeploymentTests
{
    [Test]
    public void Create_CopiesCompleteOutputToUniqueRuntimeDirectory()
    {
        var testRoot = CreateTestRoot();
        var sourceDirectory = Path.Combine(testRoot, "source");
        var runtimeRoot = Path.Combine(testRoot, "runtime");
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "assets"));
        var sourceAssembly = Path.Combine(sourceDirectory, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(sourceAssembly, "assembly");
        File.WriteAllText(Path.Combine(sourceDirectory, "assets", "settings.json"), "{}");

        try
        {
            using var deployment = MobaApiRuntimeDeployment.Create(sourceAssembly, runtimeRoot);

            Assert.Multiple(() =>
            {
                Assert.That(deployment.WorkingDirectory, Does.StartWith(runtimeRoot));
                Assert.That(deployment.AssemblyPath, Is.Not.EqualTo(sourceAssembly));
                Assert.That(File.ReadAllText(deployment.AssemblyPath), Is.EqualTo("assembly"));
                Assert.That(File.Exists(Path.Combine(deployment.WorkingDirectory, "assets", "settings.json")), Is.True);
            });
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [Test]
    public void RuntimeCopy_KeepsBuildOutputWritable_WhileRuntimeAssemblyIsOpen()
    {
        var testRoot = CreateTestRoot();
        var sourceDirectory = Path.Combine(testRoot, "source");
        Directory.CreateDirectory(sourceDirectory);
        var sourceAssembly = Path.Combine(sourceDirectory, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(sourceAssembly, "first build");

        try
        {
            using var deployment = MobaApiRuntimeDeployment.Create(sourceAssembly, Path.Combine(testRoot, "runtime"));
            using (File.Open(deployment.AssemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                File.WriteAllText(sourceAssembly, "next build");
            }

            Assert.That(File.ReadAllText(sourceAssembly), Is.EqualTo("next build"));
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [Test]
    public void Dispose_RemovesRuntimeDirectory_AndIsIdempotent()
    {
        var testRoot = CreateTestRoot();
        var sourceDirectory = Path.Combine(testRoot, "source");
        Directory.CreateDirectory(sourceDirectory);
        var sourceAssembly = Path.Combine(sourceDirectory, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(sourceAssembly, "assembly");

        try
        {
            var deployment = MobaApiRuntimeDeployment.Create(sourceAssembly, Path.Combine(testRoot, "runtime"));
            var deploymentDirectory = deployment.WorkingDirectory;

            deployment.Dispose();
            deployment.Dispose();

            Assert.That(Directory.Exists(deploymentDirectory), Is.False);
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private static string CreateTestRoot()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), nameof(MobaApiRuntimeDeploymentTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
