// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Enforces FR-001 of specs/008-project-runtimes: services and ViewModels reach the solution through
/// <c>ISolutionSession</c>, not through <c>MainWindowViewModel</c> or the <c>Solution</c> instance itself.
/// </summary>
[TestFixture]
internal sealed class SolutionSessionArchitectureTests
{
    private static readonly string[] SourceFolders = ["SharedUI", "MOBAflow", "MOBAsmart"];

    private static readonly string[] SolutionAccessFolders = ["SharedUI", "MOBAflow", "MOBAsmart", "MOBApi"];

    /// <summary>
    /// Code that may still use <c>MainWindowViewModel</c>. Folders end with '/'. Each entry names its reason;
    /// RF-24 (page ViewModels) removes the page and shell-state entries.
    /// </summary>
    private static readonly Dictionary<string, string> AllowedDependents = new(StringComparer.Ordinal)
    {
        ["SharedUI/ViewModel/MainWindowViewModel"] = "the shell itself (all partial files)",
        ["MOBAflow/View/"] = "WinUI pages bind their XAML to the shell until RF-24",
        ["MOBAflow/Behavior/GridColumnResizeBehavior.cs"] = "persists page column widths on the shell until RF-24",
        ["MOBAflow/Extensions/"] = "composition root registers the shell",
        ["MOBAflow/Service/NavigationRegistration.cs"] = "creates the pages that bind to the shell",
        ["MOBAflow/Service/PostStartupInitializationService.cs"] = "starts the shell's health checks until RF-24",
        ["SharedUI/ViewModel/MonitorPageViewModel.cs"] = "reads the shell's Z21 traffic monitor state until RF-24",
        ["SharedUI/ViewModel/EventManagerViewModel.cs"] = "reads the shell's journey command status until RF-24",
        ["MOBAsmart/Platforms/Android/MainActivity.cs"] = "shutdown cleanup lookup (MOBAsmart does not register the shell)",
    };

    /// <summary>Files whose constructors may receive the <c>Solution</c> instance.</summary>
    private static readonly HashSet<string> SolutionOwners = new(StringComparer.Ordinal)
    {
        "SharedUI/Service/SolutionSession.cs",
        "SharedUI/ViewModel/SolutionViewModel.cs",
    };

    [Test]
    public void ServicesAndViewModels_DoNotDependOnMainWindowViewModel()
    {
        var violations = new List<string>();
        foreach (var (relative, syntaxRoot) in Sources(SourceFolders))
        {
            if (AllowedDependents.Keys.Any(allowed => relative.StartsWith(allowed, StringComparison.Ordinal)))
            {
                continue;
            }

            var usesShell = syntaxRoot.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => identifier.Identifier.ValueText == "MainWindowViewModel");
            if (usesShell)
            {
                violations.Add(relative);
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Solution session rule: services and ViewModels use ISolutionSession instead of MainWindowViewModel "
            + "(specs/008-project-runtimes FR-001). Violations: " + string.Join("; ", violations));
    }

    [Test]
    public void OnlyTheSessionReceivesTheSolution()
    {
        var violations = new List<string>();
        foreach (var (relative, syntaxRoot) in Sources(SolutionAccessFolders))
        {
            if (!SolutionOwners.Contains(relative))
            {
                violations.AddRange(syntaxRoot.DescendantNodes()
                    .OfType<ParameterSyntax>()
                    // Class constructors only: records that carry a solution as data are not dependencies.
                    .Where(parameter => parameter.Parent?.Parent is ConstructorDeclarationSyntax { Parent: ClassDeclarationSyntax }
                            or ClassDeclarationSyntax
                        && IsSolutionType(parameter.Type))
                    .Select(parameter => $"{relative}: constructor parameter {parameter.Identifier.ValueText}"));
            }

            violations.AddRange(syntaxRoot.DescendantNodes()
                .OfType<GenericNameSyntax>()
                .Where(name => name.Identifier.ValueText is "GetRequiredService" or "GetService"
                    && name.TypeArgumentList.Arguments.Count == 1
                    && IsSolutionType(name.TypeArgumentList.Arguments[0])
                    && !name.Ancestors().OfType<ObjectCreationExpressionSyntax>()
                        .Any(creation => creation.Type.ToString().EndsWith("SolutionSession", StringComparison.Ordinal)))
                .Select(_ => $"{relative}: resolves Solution outside the SolutionSession registration"));
        }

        Assert.That(
            violations,
            Is.Empty,
            "Solution session rule: services and ViewModels reach the solution through ISolutionSession; only the "
            + "session receives the Solution instance (specs/008-project-runtimes FR-001). Violations: "
            + string.Join("; ", violations));
    }

    [Test]
    public void AllowedDependents_StillExist()
    {
        var root = FindRepositoryRoot();
        var stale = AllowedDependents.Keys
            .Where(entry => !entry.EndsWith('/') && !entry.EndsWith("MainWindowViewModel", StringComparison.Ordinal)
                && !File.Exists(Path.Combine(root, entry)))
            .ToList();

        Assert.That(
            stale,
            Is.Empty,
            "Solution session rule: remove entries for deleted files from AllowedDependents: " + string.Join("; ", stale));
    }

    private static bool IsSolutionType(TypeSyntax? type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "Solution",
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText == "Solution",
        NullableTypeSyntax nullable => IsSolutionType(nullable.ElementType),
        _ => false,
    };

    private static IEnumerable<(string Relative, SyntaxNode Root)> Sources(string[] folders)
    {
        var root = FindRepositoryRoot();
        foreach (var folder in folders)
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                {
                    continue;
                }

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
