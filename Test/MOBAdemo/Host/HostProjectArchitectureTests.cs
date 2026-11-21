// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo;

using System.Xml.Linq;

/// <summary>The demo host references no repository project and builds without Node (FR-001, FR-053, SC-010).</summary>
[TestFixture]
internal sealed class HostProjectArchitectureTests
{
    private static readonly string[] ForbiddenText = ["npm", "node", "package.json"];

    [Test]
    public void HostProject_HasNoProjectReference()
    {
        var references = LoadHostProject().Descendants("ProjectReference").ToList();

        Assert.That(references, Is.Empty, "The demo host must not reference any repository project.");
    }

    [Test]
    public void HostProject_RunsNoExecTaskAndMentionsNoNode()
    {
        var project = LoadHostProject();
        var execTasks = project.Descendants("Exec").ToList();
        var nodeMentions = project.DescendantsAndSelf()
            .SelectMany(element => element.Attributes().Select(attribute => attribute.Value)
                .Append(element.HasElements ? string.Empty : element.Value))
            .Where(value => ForbiddenText.Any(word => value.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(execTasks, Is.Empty, "The host project must not run external commands.");
            Assert.That(nodeMentions, Is.Empty, "The host project must not mention npm, node or package.json.");
        }
    }

    private static XElement LoadHostProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null, "Repository root (Moba.slnx) not found.");
        return XDocument.Load(Path.Combine(directory!.FullName, "MOBAdemo", "Host", "MOBAdemo.csproj")).Root!;
    }
}
