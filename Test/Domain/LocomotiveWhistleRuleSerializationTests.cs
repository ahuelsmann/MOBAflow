// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using global::Moba.Domain;
using System.Text.Json;

internal sealed class LocomotiveWhistleRuleSerializationTests
{
    [Test]
    public void Project_RoundTripsWhistleRulesAndLoadsLegacyJson()
    {
        var project = new Project
        {
            LocomotiveWhistleRules =
            [
                new LocomotiveWhistleRule { Name = "Station whistle", InPort = 4, FunctionIndex = 2, DelayMilliseconds = 250, ActiveDurationMilliseconds = 900 }
            ]
        };

        var roundTrip = JsonSerializer.Deserialize<Project>(JsonSerializer.Serialize(project));
        var legacy = JsonSerializer.Deserialize<Project>("{\"Name\":\"Legacy\"}");

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip!.LocomotiveWhistleRules.Single().Name, Is.EqualTo("Station whistle"));
            Assert.That(legacy!.LocomotiveWhistleRules, Is.Empty);
        });
    }
}
