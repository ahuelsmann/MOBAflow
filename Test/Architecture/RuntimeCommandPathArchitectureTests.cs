// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Enforces the single runtime command path documented in docs/ARCHITECTURE.md ("Runtime command path"):
/// ViewModels send operator commands only through <c>IRuntimeCommandGateway</c>, which the hosts register.
/// </summary>
[TestFixture]
internal sealed class RuntimeCommandPathArchitectureTests
{
    private static readonly string[] ViewModelFolders = ["SharedUI/ViewModel", "SharedUI/Mobile", "SharedUI/TrainControl", "MOBAflow/ViewModel"];

    /// <summary>Runtime surfaces that expose layout or locomotive commands and therefore bypass the gateway.</summary>
    private static readonly HashSet<string> CommandRuntimeTypes = new(StringComparer.Ordinal)
    {
        "IMobaRuntime",
        "ILocomotiveRuntime",
        "ILayoutControlRuntime",
        "MobaRuntimeService",
    };

    /// <summary>Gateway implementations; only hosts create and register them.</summary>
    private static readonly HashSet<string> GatewayTypes = new(StringComparer.Ordinal)
    {
        "LocalRuntimeCommandGateway",
        "RecordingRuntimeCommandGateway",
        "NoOpRuntimeCommandGateway",
        "MobileRuntimeCoordinator",
    };

    [Test]
    public void ViewModels_DoNotReferenceRuntimeCommandSurfaces()
    {
        var violations = ViewModelSources()
            .SelectMany(source => source.Root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => CommandRuntimeTypes.Contains(identifier.Identifier.ValueText))
                .Select(identifier => $"{source.Path}:{Line(identifier)} uses {identifier.Identifier.ValueText}"))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            "Runtime command path rule: ViewModels read runtime state through IRuntimeSnapshotProvider, "
            + "IConnectionRuntime and ITrafficMonitor and send commands through IRuntimeCommandGateway. "
            + "Violations: " + string.Join("; ", violations));
    }

    [TestCase("MauiViewModel", 3)]
    [TestCase("TrainControlViewModel", 1)]
    public void MobileHost_InjectsTheRegisteredCommandGateway(string viewModelType, int gatewayArgument)
    {
        var path = Path.Combine(FindRepositoryRoot(), "MOBAsmart/Extensions/MobaMauiServiceCollectionExtensions.cs");
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
        var constructor = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Single(creation => TypeName(creation.Type) == viewModelType);
        var argument = constructor.ArgumentList!.Arguments[gatewayArgument].Expression;
        var resolvesGateway = argument.DescendantNodesAndSelf().OfType<GenericNameSyntax>()
            .Any(name => name.Identifier.ValueText == "GetRequiredService"
                && name.TypeArgumentList.Arguments.Count == 1
                && TypeName(name.TypeArgumentList.Arguments[0]) == "IRuntimeCommandGateway");

        Assert.That(resolvesGateway, Is.True,
            "The mobile host must inject IRuntimeCommandGateway so operator commands pass through the recording decorator.");
    }

    [Test]
    public void ViewModels_DoNotCreateRuntimeCommandGateways()
    {
        var violations = ViewModelSources()
            .SelectMany(source => source.Root.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(creation => GatewayTypes.Contains(TypeName(creation.Type)))
                .Select(creation => $"{source.Path}:{Line(creation)} creates {TypeName(creation.Type)}"))
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            "Runtime command path rule: hosts create and register the runtime command gateway; ViewModels receive it "
            + "through their constructor. Violations: " + string.Join("; ", violations));
    }

    private static string TypeName(TypeSyntax type) => type switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => type.ToString(),
    };

    private static int Line(Microsoft.CodeAnalysis.SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static IEnumerable<(string Path, Microsoft.CodeAnalysis.SyntaxNode Root)> ViewModelSources()
    {
        var root = FindRepositoryRoot();
        foreach (var folder in ViewModelFolders)
        {
            var directory = Path.Combine(root, folder);
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                yield return (relative, CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot());
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Moba.slnx was not found above the test directory.");
    }
}
