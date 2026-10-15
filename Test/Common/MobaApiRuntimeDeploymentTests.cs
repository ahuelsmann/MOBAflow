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
            Assert.That(Directory.Exists(deploymentDirectory), Is.False);

            Directory.CreateDirectory(deploymentDirectory);
            var replacementFile = Path.Combine(deploymentDirectory, "replacement-owner.txt");
            File.WriteAllText(replacementFile, "replacement lifetime");
            deployment.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Directory.Exists(deploymentDirectory), Is.True);
                Assert.That(File.ReadAllText(replacementFile), Is.EqualTo("replacement lifetime"));
            }
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase(" \t\r\n", true)]
    [TestCase(null, false)]
    [TestCase("", false)]
    public void Create_RejectsMissingOrWhitespacePathsWithCorrectParameter(string? invalidPath, bool invalidSource)
    {
        var testRoot = CreateTestRoot();
        var sourceDirectory = Path.Combine(testRoot, "source");
        var runtimeRoot = Path.Combine(testRoot, "runtime");
        Directory.CreateDirectory(sourceDirectory);
        var sourceAssembly = Path.Combine(sourceDirectory, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(sourceAssembly, "assembly");

        try
        {
            var exception = Assert.Catch<ArgumentException>(() =>
            {
                using var deployment = MobaApiRuntimeDeployment.Create(
                    invalidSource ? invalidPath! : sourceAssembly, invalidSource ? runtimeRoot : invalidPath!);
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception!.ParamName, Is.EqualTo(invalidSource ? "sourceAssemblyPath" : "runtimeRootDirectory"));
                Assert.That(Directory.Exists(runtimeRoot), Is.False);
                Assert.That(File.ReadAllText(sourceAssembly), Is.EqualTo("assembly"));
            }
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [Test]
    public void Create_MissingAssemblyReportsFullPathWithoutCreatingRuntimeDirectory()
    {
        var testRoot = CreateTestRoot();
        var sourceAssembly = Path.Combine(testRoot, "missing", MobaApiPathResolver.AssemblyFileName);
        var runtimeRoot = Path.Combine(testRoot, "runtime");
        try
        {
            var exception = Assert.Throws<FileNotFoundException>(() =>
            {
                using var deployment = MobaApiRuntimeDeployment.Create(sourceAssembly, runtimeRoot);
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception!.FileName, Is.EqualTo(sourceAssembly));
                Assert.That(exception.Message, Does.Contain("MOBApi assembly"));
                Assert.That(Directory.Exists(runtimeRoot), Is.False);
            }
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [Test]
    public void Create_MultipleRuntimeCopiesHaveIndependentOwnedDirectories()
    {
        var testRoot = CreateTestRoot();
        var sourceDirectory = Path.Combine(testRoot, "source");
        var runtimeRoot = Path.Combine(testRoot, "runtime");
        Directory.CreateDirectory(sourceDirectory);
        var sourceAssembly = Path.Combine(sourceDirectory, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(sourceAssembly, "assembly");
        try
        {
            using var first = MobaApiRuntimeDeployment.Create(sourceAssembly, runtimeRoot);
            using var second = MobaApiRuntimeDeployment.Create(sourceAssembly, runtimeRoot);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.WorkingDirectory, Is.Not.EqualTo(second.WorkingDirectory));
                Assert.That(Path.GetDirectoryName(first.WorkingDirectory), Is.EqualTo(runtimeRoot));
                Assert.That(Path.GetDirectoryName(second.WorkingDirectory), Is.EqualTo(runtimeRoot));
                Assert.That(File.ReadAllText(first.AssemblyPath), Is.EqualTo("assembly"));
                Assert.That(File.ReadAllText(second.AssemblyPath), Is.EqualTo("assembly"));
            }

            first.Dispose();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Directory.Exists(first.WorkingDirectory), Is.False);
                Assert.That(Directory.Exists(runtimeRoot), Is.True);
                Assert.That(File.ReadAllText(second.AssemblyPath), Is.EqualTo("assembly"));
                Assert.That(File.ReadAllText(sourceAssembly), Is.EqualTo("assembly"));
            }
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [Test]
    [Platform("Win")]
    public void Create_CopyFailureRemovesPartialRuntimeCopyAndPreservesSource()
    {
        var testRoot = CreateTestRoot();
        var sourceDirectory = Path.Combine(testRoot, "source");
        var assetDirectory = Path.Combine(sourceDirectory, "assets");
        var runtimeRoot = Path.Combine(testRoot, "runtime");
        Directory.CreateDirectory(assetDirectory);
        var sourceAssembly = Path.Combine(sourceDirectory, MobaApiPathResolver.AssemblyFileName);
        var lockedAsset = Path.Combine(assetDirectory, "locked.txt");
        File.WriteAllText(sourceAssembly, "assembly");
        File.WriteAllText(lockedAsset, "original asset");
        try
        {
            using (File.Open(lockedAsset, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var exception = Assert.Throws<IOException>(() =>
                {
                    using var deployment = MobaApiRuntimeDeployment.Create(sourceAssembly, runtimeRoot);
                });

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(exception!.Message, Does.Contain("locked.txt"));
                    Assert.That(Directory.Exists(runtimeRoot), Is.True);
                    Assert.That(Directory.GetFileSystemEntries(runtimeRoot), Is.Empty);
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.ReadAllText(sourceAssembly), Is.EqualTo("assembly"));
                Assert.That(File.ReadAllText(lockedAsset), Is.EqualTo("original asset"));
            }
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
