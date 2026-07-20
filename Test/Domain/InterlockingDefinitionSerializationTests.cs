// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using System.Text.Json;

internal sealed class InterlockingDefinitionSerializationTests
{
    [Test]
    public void Project_RoundTripsSharedInterlockingDefinition()
    {
        var routeId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var project = new Project
        {
            Interlocking = new InterlockingDefinition
            {
                Turnouts =
                [
                    new TurnoutDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Name = "W1",
                        DecoderAddress = 10,
                        Kind = TurnoutKind.ThreeWay,
                        Commands =
                        [
                            new TurnoutCommandMapping
                            {
                                Position = TurnoutPosition.Straight,
                                Commands =
                                [
                                    new TurnoutAccessoryCommand { AddressOffset = 1, Output = 0 }
                                ]
                            }
                        ]
                    }
                ],
                Routes = [new RouteDefinition { Id = routeId, Name = "Route 1" }]
            },
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements =
                [
                    new SbSwitch
                    {
                        Name = "W1 presentation",
                        State = SignalBoxElementState.Occupied,
                        SwitchPosition = SwitchPosition.DivergingLeft
                    },
                    new SbSignal
                    {
                        Name = "N1 presentation",
                        State = SignalBoxElementState.RouteSet,
                        SignalAspect = SignalAspect.Ks1,
                        ExtendedAccessoryValue = 1
                    }
                ]
            }
        };

        var json = JsonSerializer.Serialize(project, JsonOptions.Default);
        var restored = JsonSerializer.Deserialize<Project>(json, JsonOptions.Default);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var presentationElements = root.GetProperty("signalBoxPlan").GetProperty("elements").EnumerateArray().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"interlocking\""));
            Assert.That(json, Does.Contain("\"kind\": \"ThreeWay\""));
            Assert.That(root.GetProperty("signalBoxPlan").TryGetProperty("routes", out _), Is.False);
            Assert.That(presentationElements.All(element => !element.TryGetProperty("state", out _)), Is.True);
            Assert.That(presentationElements.All(element => !element.TryGetProperty("switchPosition", out _)), Is.True);
            Assert.That(presentationElements.All(element => !element.TryGetProperty("signalAspect", out _)), Is.True);
            Assert.That(presentationElements.All(element => !element.TryGetProperty("extendedAccessoryValue", out _)), Is.True);
            Assert.That(root.GetProperty("interlocking").GetProperty("routes").GetArrayLength(), Is.EqualTo(1));
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Interlocking.Turnouts.Single().Kind, Is.EqualTo(TurnoutKind.ThreeWay));
            Assert.That(
                restored.Interlocking.Turnouts.Single().Commands.Single().Commands.Single().AddressOffset,
                Is.EqualTo(1));
            Assert.That(restored.Interlocking.Routes.Single().Id, Is.EqualTo(routeId));
            Assert.That(restored.SignalBoxPlan, Is.Not.Null);
            Assert.That(restored.SignalBoxPlan!.Elements.OfType<SbSwitch>().Single().SwitchPosition, Is.EqualTo(SwitchPosition.Straight));
            Assert.That(restored.SignalBoxPlan.Elements.OfType<SbSignal>().Single().SignalAspect, Is.EqualTo(SignalAspect.Hp0));
        });
    }
}
