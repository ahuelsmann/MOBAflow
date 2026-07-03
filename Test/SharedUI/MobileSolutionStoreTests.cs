// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Runtime;

using Moba.Domain;

using Moba.SharedUI.Interface;

using Moba.SharedUI.Service;

namespace Moba.Test.SharedUI;

[TestFixture]

internal sealed class MobileSolutionStoreTests

{

    private string _tempDirectory = null!;

    [SetUp]

    public void SetUp()

    {

        _tempDirectory = Path.Combine(Path.GetTempPath(), "mobasmart-cache-" + Guid.NewGuid().ToString("N"));

    }

    [TearDown]

    public void TearDown()

    {

        if (Directory.Exists(_tempDirectory))

        {

            Directory.Delete(_tempDirectory, recursive: true);

        }
    }

    [Test]

    public async Task SaveAndLoadAsync_RoundTripsSolutionMetaAndSignalBox()

    {

        var store = CreateStore();

        var solution = CreateSolution();

        var meta = new SolutionSyncMeta(DateTimeOffset.UtcNow, solution.Name, "myMOBA");

        var signalId = Guid.NewGuid();

        var signalBoxElements = new List<SignalBoxElementRuntimeSnapshot>

        {

            new()

            {

                ElementId = signalId,

                Name = "S1",

                Kind = SignalBoxElementKind.Signal,

                X = 1,

                Y = 2,

                SignalAspect = SignalAspect.Hp0

            }
        };

        await store.SaveAsync(solution, meta);

        await store.SaveSignalBoxElementsAsync(signalBoxElements);

        var locomotiveFleet = new List<LocomotiveFleetSnapshot>
        {
            new()
            {
                LocomotiveId = solution.Projects[0].Locomotives[0].Id,
                Name = "BR 110 Verkehrsrot",
                DigitalAddress = 7
            }
        };

        await store.SaveLocomotiveFleetAsync(locomotiveFleet);

        var loaded = await store.TryLoadAsync();

        Assert.That(loaded, Is.Not.Null);

        Assert.Multiple(() =>

        {

            Assert.That(loaded!.Meta.UpdatedAt, Is.EqualTo(meta.UpdatedAt));

            Assert.That(loaded.Meta.ActiveProjectName, Is.EqualTo("myMOBA"));

            Assert.That(loaded.Solution.Projects, Has.Count.EqualTo(1));

            Assert.That(loaded.Solution.Projects[0].Locomotives, Has.Count.EqualTo(1));

            Assert.That(loaded.SignalBoxElements, Has.Count.EqualTo(1));

            Assert.That(loaded.SignalBoxElements[0].ElementId, Is.EqualTo(signalId));

            Assert.That(loaded.LocomotiveFleet, Has.Count.EqualTo(1));

            Assert.That(loaded.LocomotiveFleet[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));

        });

    }

    [Test]

    public async Task TryLoadAsync_ReturnsNull_WhenSolutionJsonIsInvalid()

    {

        Directory.CreateDirectory(_tempDirectory);

        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "mobile-solution.json"), "{ invalid");

        await File.WriteAllTextAsync(

            Path.Combine(_tempDirectory, "mobile-solution-meta.json"),

            """{"updatedAt":"2026-01-01T00:00:00Z","solutionName":"x","activeProjectName":"y"}""");

        var loaded = await CreateStore().TryLoadAsync();

        Assert.That(loaded, Is.Null);

    }

    private MobileSolutionStore CreateStore() =>

        new(_tempDirectory, NullLogger<MobileSolutionStore>.Instance);

    private static Solution CreateSolution() =>

        new()

        {

            Name = "Cached Solution",

            Projects =

            [

                new Project

                {

                    Name = "myMOBA",

                    Locomotives =

                    [

                        new Locomotive

                        {

                            Id = Guid.NewGuid(),

                            Name = "BR 110",

                            DigitalAddress = 7

                        }

                    ]

                }

            ]

        };

}

