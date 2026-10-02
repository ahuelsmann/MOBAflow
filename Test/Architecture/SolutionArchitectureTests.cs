// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// Enforces the solution-wide architecture rules documented in docs/ARCHITECTURE.md
/// ("Project dependency rules"). The checks read project files and sources, so they cover
/// MOBAflow and MOBAsmart on every platform without loading their assemblies.
/// </summary>
[TestFixture]
internal sealed partial class SolutionArchitectureTests
{
    /// <summary>
    /// Documented direct dependencies. A project may reference every project it reaches through these edges.
    /// </summary>
    private static readonly Dictionary<string, string[]> DocumentedDependencies = new(StringComparer.Ordinal)
    {
        ["Domain"] = [],
        ["TrackLibrary.Base"] = [],
        ["Common"] = ["Domain"],
        ["Sound"] = ["Common"],
        ["Backend"] = ["Common", "Domain", "Sound"],
        ["TrackPlan.Renderer"] = ["TrackLibrary.Base"],
        ["TrackLibrary.PikoA"] = ["TrackLibrary.Base", "Domain", "TrackPlan.Renderer"],
        ["MOBAdisplay"] = ["Domain"],
        ["MOBApi"] = ["Common"],
        ["SharedUI"] = ["Backend", "Common", "MOBAdisplay", "TrackPlan.Renderer", "TrackLibrary.PikoA"],
        ["MOBAflow"] = ["Backend", "Common", "MOBApi", "SharedUI", "TrackLibrary.Base", "TrackPlan.Renderer", "TrackLibrary.PikoA", "MOBAdisplay"],
        ["MOBAsmart"] = ["SharedUI", "Common", "Sound"],
    };

    private static readonly HashSet<string> PlatformHosts = new(StringComparer.Ordinal) { "MOBAflow", "MOBAsmart" };

    private static readonly string[] PlatformUiPackagePrefixes =
    [
        "Microsoft.WindowsAppSDK",
        "Microsoft.Maui",
        "CommunityToolkit.WinUI",
        "CommunityToolkit.Maui",
        "Microsoft.Xaml.Behaviors.WinUI",
        "Microsoft.Graphics.Win2D",
    ];

    /// <summary>
    /// Known namespace exceptions, each removed by the named refactoring package.
    /// Key: project; value: namespaces outside the project root that are tolerated until then.
    /// </summary>
    private static readonly Dictionary<string, string[]> KnownNamespaceExceptions = new(StringComparer.Ordinal)
    {
        // RF-21 moves these renderer types into a Moba.TrackLibrary.PikoA namespace.
        ["TrackLibrary.PikoA"] = ["Moba.TrackPlan.Renderer"],
    };

    [Test]
    public void EverySolutionProject_HasDocumentedDependencies()
    {
        var undocumented = LoadSolutionProjects()
            .Select(project => project.Name)
            .Where(name => !DocumentedDependencies.ContainsKey(name))
            .ToList();

        Assert.That(
            undocumented,
            Is.Empty,
            "Project dependency rule: every product project in Moba.slnx needs an entry in "
            + "docs/ARCHITECTURE.md and SolutionArchitectureTests.DocumentedDependencies. Missing: "
            + string.Join(", ", undocumented));
    }

    [Test]
    public void ProjectReferences_FollowDocumentedDependencyDirection()
    {
        var violations = new List<string>();
        foreach (var project in LoadSolutionProjects().Where(project => DocumentedDependencies.ContainsKey(project.Name)))
        {
            var allowed = ReachableFrom(project.Name);
            violations.AddRange(project.ProjectReferences
                .Where(reference => !allowed.Contains(reference))
                .Select(reference => $"{project.Name} -> {reference}"));
        }

        Assert.That(
            violations,
            Is.Empty,
            "Project dependency rule: a project may reference only projects below it in the documented "
            + "dependency graph (docs/ARCHITECTURE.md). Forbidden references: " + string.Join("; ", violations));
    }

    [Test]
    public void SharedProjects_DoNotUsePlatformUiFrameworks()
    {
        var violations = new List<string>();
        foreach (var project in LoadSolutionProjects().Where(project => DocumentedDependencies.ContainsKey(project.Name)
            && !PlatformHosts.Contains(project.Name)))
        {
            violations.AddRange(project.UiFrameworkProperties.Select(property => $"{project.Name}: <{property}>true"));
            violations.AddRange(project.PackageReferences
                .Where(package => PlatformUiPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.Ordinal)))
                .Select(package => $"{project.Name}: package {package}"));
        }

        Assert.That(
            violations,
            Is.Empty,
            "Platform UI rule: only MOBAflow (WinUI) and MOBAsmart (MAUI) may use WinUI or MAUI; shared projects stay "
            + "platform-neutral. Violations: " + string.Join("; ", violations));
    }

    [Test]
    public void SourceNamespaces_StayBelowTheProjectRootNamespace()
    {
        var violations = new List<string>();
        foreach (var project in LoadSolutionProjects().Where(project => DocumentedDependencies.ContainsKey(project.Name)))
        {
            var tolerated = KnownNamespaceExceptions.GetValueOrDefault(project.Name, []);
            foreach (var (file, declaredNamespace) in ReadNamespaceDeclarations(project))
            {
                if (!IsBelow(declaredNamespace, project.RootNamespace) && !tolerated.Contains(declaredNamespace, StringComparer.Ordinal))
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

    [Test]
    public void KnownNamespaceExceptions_AreStillNeeded()
    {
        var stale = new List<string>();
        foreach (var project in LoadSolutionProjects().Where(project => KnownNamespaceExceptions.ContainsKey(project.Name)))
        {
            var declared = ReadNamespaceDeclarations(project).Select(declaration => declaration.Namespace).ToHashSet(StringComparer.Ordinal);
            stale.AddRange(KnownNamespaceExceptions[project.Name]
                .Where(exception => !declared.Contains(exception))
                .Select(exception => $"{project.Name}: {exception}"));
        }

        Assert.That(
            stale,
            Is.Empty,
            "Namespace rule: remove resolved entries from SolutionArchitectureTests.KnownNamespaceExceptions: "
            + string.Join("; ", stale));
    }

    private static HashSet<string> ReachableFrom(string project)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(DocumentedDependencies[project]);
        while (pending.Count > 0)
        {
            var next = pending.Pop();
            if (reachable.Add(next))
            {
                foreach (var dependency in DocumentedDependencies.GetValueOrDefault(next, []))
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
        var generatedFolders = new[] { "bin", "obj" };
        foreach (var file in Directory.EnumerateFiles(project.Directory, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(project.Directory, file);
            var firstFolder = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (generatedFolders.Contains(firstFolder, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (Match match in NamespaceDeclaration().Matches(File.ReadAllText(file)))
            {
                yield return ($"{project.Name}/{relative.Replace('\\', '/')}", match.Groups["name"].Value);
            }
        }
    }

    private static List<SolutionProject> LoadSolutionProjects()
    {
        var root = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(root, "Moba.slnx"))
            .Descendants("Project")
            .Select(element => element.Attribute("Path")!.Value)
            .Where(path => !path.StartsWith("Test/", StringComparison.Ordinal))
            .Select(path => SolutionProject.Load(Path.Combine(root, path)))
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

    [GeneratedRegex(@"^\s*namespace\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();

    private sealed record SolutionProject(
        string Name,
        string Directory,
        string RootNamespace,
        IReadOnlyList<string> ProjectReferences,
        IReadOnlyList<string> PackageReferences,
        IReadOnlyList<string> UiFrameworkProperties)
    {
        public static SolutionProject Load(string projectFile)
        {
            var name = Path.GetFileNameWithoutExtension(projectFile);
            var document = XDocument.Load(projectFile);
            var rootNamespace = document.Descendants("RootNamespace").Select(element => element.Value.Trim()).FirstOrDefault()
                ?? name;
            var projectReferences = document.Descendants("ProjectReference")
                .Select(element => Path.GetFileNameWithoutExtension(element.Attribute("Include")!.Value.Replace('\\', '/')))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var packageReferences = document.Descendants("PackageReference")
                .Select(element => element.Attribute("Include")?.Value)
                .OfType<string>()
                .ToList();
            var uiFrameworkProperties = document.Descendants()
                .Where(element => element.Name.LocalName is "UseWinUI" or "UseMaui"
                    && string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                .Select(element => element.Name.LocalName)
                .ToList();

            return new SolutionProject(
                name,
                Path.GetDirectoryName(projectFile)!,
                rootNamespace,
                projectReferences,
                packageReferences,
                uiFrameworkProperties);
        }
    }
}
