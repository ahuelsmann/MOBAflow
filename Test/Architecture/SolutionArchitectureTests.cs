// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Enforces the solution-wide architecture rules in docs/ARCHITECTURE.md ("Project dependency rules").
/// The documented table is the single source of the allowed dependencies and root namespaces. The checks
/// read project and build files and parse the C# sources, so they cover MOBAflow and MOBAsmart on every
/// platform without loading their assemblies.
/// </summary>
[TestFixture]
internal sealed partial class SolutionArchitectureTests
{
    private const string ArchitectureDocument = "docs/ARCHITECTURE.md";
    private const string DependencySection = "### Project dependency rules";

    private static readonly string[] WinUiPackagePrefixes =
    [
        "Microsoft.WindowsAppSDK",
        "CommunityToolkit.WinUI",
        "Microsoft.Xaml.Behaviors.WinUI",
        "Microsoft.Graphics.Win2D",
    ];

    private static readonly string[] MauiPackagePrefixes =
    [
        "Microsoft.Maui",
        "CommunityToolkit.Maui",
    ];

    /// <summary>The only project allowed to use each platform UI framework.</summary>
    private static readonly Dictionary<string, string> UiFrameworkOwners = new(StringComparer.Ordinal)
    {
        ["WinUI"] = "MOBAflow",
        ["MAUI"] = "MOBAsmart",
    };

    [Test]
    public void DocumentedTable_MatchesSolutionProjects()
    {
        var documented = LoadDocumentedProjects();
        var projects = LoadSolutionProjects();
        var violations = new List<string>();

        violations.AddRange(projects
            .Where(project => !documented.ContainsKey(project.Name))
            .Select(project => $"{project.Name} is in Moba.slnx but has no table row"));
        violations.AddRange(documented.Keys
            .Where(name => projects.All(project => project.Name != name))
            .Select(name => $"{name} has a table row but is not in Moba.slnx"));
        violations.AddRange(projects
            .Where(project => documented.TryGetValue(project.Name, out var row)
                && !string.Equals(row.RootNamespace, project.RootNamespace, StringComparison.Ordinal))
            .Select(project => $"{project.Name}: table root namespace {documented[project.Name].RootNamespace}, "
                + $"project file {project.RootNamespace}"));
        violations.AddRange(documented
            .SelectMany(row => row.Value.References
                .Where(reference => !documented.ContainsKey(reference))
                .Select(reference => $"{row.Key} lists unknown project {reference}")));

        Assert.That(
            violations,
            Is.Empty,
            $"Project dependency rule: the table in {ArchitectureDocument} lists every product project in Moba.slnx "
            + "with its root namespace and references. Differences: " + string.Join("; ", violations));
    }

    [Test]
    public void ProjectReferences_FollowDocumentedDependencyDirection()
    {
        var documented = LoadDocumentedProjects();
        var projects = LoadSolutionProjects();
        var projectsByPath = projects.ToDictionary(project => project.ProjectFile, StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();

        foreach (var project in projects.Where(project => documented.ContainsKey(project.Name)))
        {
            var allowed = ReachableFrom(project.Name, documented);
            foreach (var reference in project.Build.ProjectReferences)
            {
                if (!projectsByPath.TryGetValue(reference, out var target))
                {
                    violations.Add($"{project.Name} -> {reference} (not a project in Moba.slnx)");
                }
                else if (!allowed.Contains(target.Name))
                {
                    violations.Add($"{project.Name} -> {target.Name}");
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Project dependency rule: a project may reference only Moba.slnx projects below it in the documented "
            + $"dependency graph ({ArchitectureDocument}). Forbidden references: " + string.Join("; ", violations));
    }

    [Test]
    public void PlatformUiFrameworks_AreUsedOnlyByTheirHost()
    {
        var violations = new List<string>();
        foreach (var project in LoadSolutionProjects())
        {
            foreach (var (framework, evidence) in UiFrameworkUsage(project.Build))
            {
                if (!string.Equals(UiFrameworkOwners[framework], project.Name, StringComparison.Ordinal))
                {
                    violations.Add($"{project.Name}: {framework} via {evidence}");
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Platform UI rule: only MOBAflow uses WinUI and only MOBAsmart uses MAUI; every other project stays "
            + "platform-neutral. Violations: " + string.Join("; ", violations));
    }

    [Test]
    public void BuildImports_CanBeResolved()
    {
        var unresolved = LoadSolutionProjects()
            .SelectMany(project => project.Build.UnresolvedImports.Select(import => $"{project.Name}: {import}"))
            .ToList();

        Assert.That(
            unresolved,
            Is.Empty,
            "Project dependency rule: the architecture test must see every imported build file to check its "
            + "references and packages. Unresolved imports: " + string.Join("; ", unresolved));
    }

    [Test]
    public void SourceNamespaces_StayBelowTheProjectRootNamespace()
    {
        var violations = new List<string>();
        foreach (var project in LoadSolutionProjects())
        {
            foreach (var (file, declaredNamespace) in ReadNamespaceDeclarations(project))
            {
                if (!IsBelow(declaredNamespace, project.RootNamespace))
                {
                    violations.Add($"{file}: namespace {declaredNamespace} (root {project.RootNamespace})");
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Namespace rule: every namespace declared in a project starts with that project's root namespace. "
            + "Violations: " + string.Join("; ", violations));
    }

    private static IEnumerable<(string Framework, string Evidence)> UiFrameworkUsage(BuildDefinition build)
    {
        foreach (var property in build.TrueProperties)
        {
            if (property == "UseWinUI")
            {
                yield return ("WinUI", $"<{property}>true");
            }
            else if (property == "UseMaui")
            {
                yield return ("MAUI", $"<{property}>true");
            }
        }

        foreach (var package in build.PackageReferences)
        {
            if (WinUiPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.Ordinal)))
            {
                yield return ("WinUI", $"package {package}");
            }
            else if (MauiPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.Ordinal)))
            {
                yield return ("MAUI", $"package {package}");
            }
        }
    }

    private static HashSet<string> ReachableFrom(string project, Dictionary<string, DocumentedProject> documented)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(documented[project].References);
        while (pending.Count > 0)
        {
            var next = pending.Pop();
            if (reachable.Add(next) && documented.TryGetValue(next, out var row))
            {
                foreach (var dependency in row.References)
                {
                    pending.Push(dependency);
                }
            }
        }

        return reachable;
    }

    private static bool IsBelow(string declaredNamespace, string rootNamespace) =>
        string.Equals(declaredNamespace, rootNamespace, StringComparison.Ordinal)
        || declaredNamespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal);

    private static IEnumerable<(string File, string Namespace)> ReadNamespaceDeclarations(SolutionProject project)
    {
        string[] generatedFolders = ["bin", "obj"];
        foreach (var file in Directory.EnumerateFiles(project.Directory, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(project.Directory, file);
            var firstFolder = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (generatedFolders.Contains(firstFolder, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var declaration in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            {
                yield return ($"{project.Name}/{relative.Replace('\\', '/')}", FullNamespace(declaration));
            }
        }
    }

    private static string FullNamespace(BaseNamespaceDeclarationSyntax declaration)
    {
        var parts = declaration.AncestorsAndSelf()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .SelectMany(item => item.Name.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            .Select(identifier => identifier.Identifier.ValueText);
        return string.Join('.', parts);
    }

    private static Dictionary<string, DocumentedProject> LoadDocumentedProjects()
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), ArchitectureDocument));
        var start = Array.FindIndex(lines, line => line.Trim() == DependencySection);
        if (start < 0)
        {
            throw new InvalidOperationException($"{ArchitectureDocument} has no '{DependencySection}' section.");
        }

        var rows = new Dictionary<string, DocumentedProject>(StringComparer.Ordinal);
        foreach (var line in lines.Skip(start + 1).SkipWhile(line => !line.StartsWith('|')).TakeWhile(line => line.StartsWith('|')))
        {
            var cells = line.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            var project = CodeSpans(cells[0]).FirstOrDefault();
            if (cells.Length < 3 || project is null)
            {
                continue;
            }

            rows.Add(project, new DocumentedProject(CodeSpans(cells[1]).Single(), CodeSpans(cells[2]).ToArray()));
        }

        return rows;
    }

    private static IEnumerable<string> CodeSpans(string cell) =>
        CodeSpan().Matches(cell).Select(match => match.Groups["value"].Value);

    private static List<SolutionProject> LoadSolutionProjects()
    {
        var root = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(root, "Moba.slnx"))
            .Descendants("Project")
            .Select(element => element.Attribute("Path")!.Value)
            .Where(path => !path.StartsWith("Test/", StringComparison.Ordinal))
            .Select(path => SolutionProject.Load(root, Path.GetFullPath(Path.Combine(root, path))))
            .ToList();
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

    [GeneratedRegex("`(?<value>[^`]+)`")]
    private static partial Regex CodeSpan();

    private sealed record DocumentedProject(string RootNamespace, string[] References);

    private sealed record SolutionProject(string Name, string ProjectFile, string Directory, string RootNamespace, BuildDefinition Build)
    {
        public static SolutionProject Load(string repositoryRoot, string projectFile)
        {
            var name = Path.GetFileNameWithoutExtension(projectFile);
            var rootNamespace = XDocument.Load(projectFile).Descendants("RootNamespace")
                .Select(element => element.Value.Trim())
                .FirstOrDefault() ?? name;
            return new SolutionProject(
                name,
                projectFile,
                Path.GetDirectoryName(projectFile)!,
                rootNamespace,
                BuildDefinition.Load(repositoryRoot, projectFile));
        }
    }

    /// <summary>
    /// Project references, packages and UI framework switches from a project file, the nearest
    /// Directory.Build.props/.targets and every file they import. Conditions are ignored, so a
    /// conditional reference is checked as if it were always active.
    /// </summary>
    private sealed record BuildDefinition(
        IReadOnlyList<string> ProjectReferences,
        IReadOnlyList<string> PackageReferences,
        IReadOnlyList<string> TrueProperties,
        IReadOnlyList<string> UnresolvedImports)
    {
        public static BuildDefinition Load(string repositoryRoot, string projectFile)
        {
            var projectReferences = new List<string>();
            var packageReferences = new List<string>();
            var trueProperties = new List<string>();
            var unresolvedImports = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<string>();
            pending.Push(projectFile);
            foreach (var shared in new[] { "Directory.Build.props", "Directory.Build.targets" })
            {
                var nearest = FindNearest(Path.GetDirectoryName(projectFile)!, repositoryRoot, shared);
                if (nearest is not null)
                {
                    pending.Push(nearest);
                }
            }

            while (pending.Count > 0)
            {
                var file = pending.Pop();
                // A missing file can only come from a conditional import (for example Exists(...)); it adds nothing.
                if (!visited.Add(file) || !File.Exists(file))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(file)!;
                var document = XDocument.Load(file);
                foreach (var element in document.Descendants())
                {
                    switch (element.Name.LocalName)
                    {
                        case "ProjectReference" when element.Attribute("Include") is { } include:
                            projectReferences.Add(ResolvePath(include.Value, directory));
                            break;
                        case "PackageReference" when element.Attribute("Include") is { } package:
                            packageReferences.Add(package.Value);
                            break;
                        case "UseWinUI" or "UseMaui" when string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase):
                            trueProperties.Add(element.Name.LocalName);
                            break;
                        case "Import" when element.Attribute("Project") is { } import && element.Attribute("Sdk") is null:
                            var path = import.Value
                                .Replace("$(MSBuildThisFileDirectory)", directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                                .Replace("$(MSBuildProjectDirectory)", Path.GetDirectoryName(projectFile)!, StringComparison.Ordinal);
                            if (path.Contains("$(", StringComparison.Ordinal))
                            {
                                unresolvedImports.Add($"{Path.GetFileName(file)} imports {import.Value}");
                            }
                            else
                            {
                                pending.Push(ResolvePath(path, directory));
                            }

                            break;
                    }
                }
            }

            return new BuildDefinition(projectReferences, packageReferences, trueProperties, unresolvedImports);
        }

        private static string ResolvePath(string path, string directory) =>
            Path.GetFullPath(Path.Combine(directory, path.Replace('\\', Path.DirectorySeparatorChar)));

        private static string? FindNearest(string start, string repositoryRoot, string fileName)
        {
            var root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar);
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                if (string.Equals(directory.FullName.TrimEnd(Path.DirectorySeparatorChar), root, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            return null;
        }
    }
}
