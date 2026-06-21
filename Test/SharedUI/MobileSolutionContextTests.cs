// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Domain;
using Moba.SharedUI.Service;

namespace Moba.Test.SharedUI;

[TestFixture]
internal sealed class MobileSolutionContextTests
{
    [Test]
    public void ApplySolution_SelectsActiveProject_ByName()
    {
        var context = new MobileSolutionContext();
        var solution = new Solution
        {
            Name = "Test",
            Projects =
            [
                new Project { Name = "Other", Locomotives = [new Locomotive { Name = "Wrong" }] },
                new Project
                {
                    Name = "myMOBA",
                    Locomotives = [new Locomotive { Name = "BR 110 Verkehrsrot", DigitalAddress = 7 }]
                }
            ]
        };

        context.ApplySolution(solution, "myMOBA");

        Assert.That(context.SelectedProject?.Name, Is.EqualTo("myMOBA"));
        Assert.That(context.SelectedProject?.Locomotives, Has.Count.EqualTo(1));
        Assert.That(context.SelectedProject?.Locomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
    }
}
