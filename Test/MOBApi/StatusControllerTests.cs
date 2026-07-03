// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.Extensions.Configuration;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Service;
using System.Text.Json;

[TestFixture]
internal sealed class RuntimeRemoteRegistryTests
{
    [Test]
    public void RegisterAndUnregister_TracksRemoteClients()
    {
        var registry = new RuntimeRemoteRegistry();

        registry.Register("conn-1", "mobasmart-1");
        registry.Register("conn-2", "mobasmart-2");

        Assert.That(registry.Count, Is.EqualTo(2));

        registry.Unregister("conn-1");

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.GetAll()[0].ClientId, Is.EqualTo("mobasmart-2"));
        });
    }
}

[TestFixture]
internal sealed class StatusControllerTests
{
    [Test]
    public void GetStatus_IncludesRuntimeAndSolutionDiagnostics()
    {
        var snapshotCache = new RuntimeSnapshotCache();
        var signalId = Guid.NewGuid();
        snapshotCache.Set(
            RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
            {
                IsConnected = true,
                SignalBoxElements =
                [
                    new SignalBoxElementRuntimeSnapshot
                    {
                        ElementId = signalId,
                        Kind = SignalBoxElementKind.Signal,
                        SignalAspect = SignalAspect.Hp0
                    }
                ],
                LocomotiveFleet =
                [
                    new LocomotiveFleetSnapshot
                    {
                        LocomotiveId = Guid.NewGuid(),
                        Name = "BR 110",
                        DigitalAddress = 3
                    }
                ]
            }),
            isConnected: true);

        var solutionCache = new SolutionCache();
        solutionCache.Set("{\"name\":\"Test\",\"schemaVersion\":1,\"projects\":[]}", "test.json", "myMOBA");

        var hostRegistry = new RuntimeHostRegistry();
        hostRegistry.SetHost("host-conn");

        var remoteRegistry = new RuntimeRemoteRegistry();
        remoteRegistry.Register("remote-conn", "mobasmart");

        var broadcastMetrics = new RuntimeBroadcastMetrics();
        broadcastMetrics.RecordSnapshotBroadcast(842);

        var controller = new StatusController(
            new ClientRegistry(),
            hostRegistry,
            remoteRegistry,
            broadcastMetrics,
            snapshotCache,
            solutionCache);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Kestrel:Endpoints:Http:Url"] = "http://127.0.0.1:5001" })
            .Build();

        var result = controller.GetStatus(configuration);
        var ok = result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        Assert.That(ok, Is.Not.Null);

        var json = JsonSerializer.Serialize(ok!.Value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var runtimeJson = root.GetProperty("runtime");
        var snapshotCacheJson = runtimeJson.GetProperty("snapshotCache");
        var solutionJson = root.GetProperty("solution");

        Assert.Multiple(() =>
        {
            Assert.That(runtimeJson.GetProperty("hasHost").GetBoolean(), Is.True);
            Assert.That(runtimeJson.GetProperty("remoteClientCount").GetInt32(), Is.EqualTo(1));
            Assert.That(runtimeJson.GetProperty("lastSnapshotPayloadBytes").GetInt32(), Is.EqualTo(842));
            Assert.That(runtimeJson.GetProperty("totalSnapshotBroadcastCount").GetInt64(), Is.EqualTo(1));
            Assert.That(runtimeJson.GetProperty("sessionOperational").GetBoolean(), Is.True);
            Assert.That(snapshotCacheJson.GetProperty("available").GetBoolean(), Is.True);
            Assert.That(snapshotCacheJson.GetProperty("signalBoxElementCount").GetInt32(), Is.EqualTo(1));
            Assert.That(snapshotCacheJson.GetProperty("locomotiveFleetCount").GetInt32(), Is.EqualTo(1));
            Assert.That(solutionJson.GetProperty("available").GetBoolean(), Is.True);
            Assert.That(solutionJson.GetProperty("activeProjectName").GetString(), Is.EqualTo("myMOBA"));
        });
    }
}
