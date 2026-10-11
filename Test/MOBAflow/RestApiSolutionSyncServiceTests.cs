#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Configuration;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.WinUI.Service;

using Moq;

[TestFixture]
[SuppressMessage("Performance", "CA1812", Justification = "NUnit creates this fixture through reflection.")]
internal sealed partial class RestApiSolutionSyncServiceTests
{
    [Test]
    public async Task ModelChange_WithoutSavedPath_PushesTheEditedProjectEndpoint()
    {
        var project = new Project { Z21 = { IpAddress = "192.168.0.111" } };
        var session = new Mock<ISolutionSession>();
        session.SetupGet(value => value.Solution).Returns(new Solution { Projects = [project] });
        session.SetupGet(value => value.CurrentSolutionPath).Returns((string?)null);
        var settings = new AppSettings();
        using var handler = new SolutionPushHandler();
        using var client = new LocalMobApiClient(settings, handler);
        using var process = new RestApiProcessService(settings,
            NullLogger<RestApiProcessService>.Instance, NullLogger<UdpDiscoveryResponder>.Instance);
        using var service = new RestApiSolutionSyncService(settings, session.Object, process,
            NullLogger<RestApiSolutionSyncService>.Instance, client);

        project.Z21.IpAddress = "192.168.0.112";
        project.Z21.Port = 21106;
        session.Raise(value => value.ModelChanged += null,
            new PropertyChangedEventArgs("Z21IpAddress"));

        var push = await handler.Push.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(push.Path, Is.EqualTo("mobaflow://in-memory"));
            Assert.That(push.Solution.Projects[0].Z21.IpAddress, Is.EqualTo("192.168.0.112"));
            Assert.That(push.Solution.Projects[0].Z21.Port, Is.EqualTo(21106));
            Assert.That(push.Uri!.AbsolutePath, Is.EqualTo("/api/solution"));
        }
    }

    private sealed partial class SolutionPushHandler : HttpMessageHandler
    {
        public TaskCompletionSource<(Solution Solution, string Path, Uri? Uri)> Push { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var solution = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default)!;
            Push.TrySetResult((solution, request.Headers.GetValues("X-MOBAflow-Solution-Path").Single(), request.RequestUri));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
#endif
