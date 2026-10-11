#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using System.ComponentModel;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Service;

using Moq;

[TestFixture]
internal sealed partial class RestApiSolutionSyncServiceTests
{
    [TestCase("192.168.0.112")]
    [TestCase("")]
    public async Task SelectingProject_UpdatesOrClearsEndpointEvenWhenSolutionPushFails(string nextAddress)
    {
        var settings = new AppSettings();
        using var handler = new RuntimeSettingsHandler();
        using var client = new LocalMobApiClient(settings, handler);
        using var process = new RestApiProcessService(settings,
            NullLogger<RestApiProcessService>.Instance, NullLogger<UdpDiscoveryResponder>.Instance);
        var selected = new ProjectViewModel(new Project { Z21 = { IpAddress = "192.168.0.111" } });
        var session = new Mock<ISolutionSession>();
        session.SetupGet(value => value.SelectedProject).Returns(() => selected);
        session.SetupGet(value => value.Solution).Returns(new Solution());
        using var service = new RestApiSolutionSyncService(settings, session.Object, process,
            new EventBus(NullLogger<EventBus>.Instance), NullLogger<RestApiSolutionSyncService>.Instance, client);
        service.QueueRuntimeSettingsPush();
        var initial = await handler.Requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        using var initialJson = JsonDocument.Parse(initial.Body);
        Assert.That(initialJson.RootElement.GetProperty("z21IpAddress").GetString(), Is.EqualTo("192.168.0.111"));

        selected = new ProjectViewModel(new Project { Z21 = { IpAddress = nextAddress } });
        session.Raise(value => value.PropertyChanged += null,
            new PropertyChangedEventArgs(nameof(ISolutionSession.SelectedProject)));
        var request = await handler.Requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.That(request.Method, Is.EqualTo(nextAddress.Length == 0 ? HttpMethod.Delete : HttpMethod.Put));
        if (nextAddress.Length > 0)
        {
            using var json = JsonDocument.Parse(request.Body);
            Assert.That(json.RootElement.GetProperty("z21IpAddress").GetString(), Is.EqualTo(nextAddress));
        }
    }

    private sealed partial class RuntimeSettingsHandler : HttpMessageHandler
    {
        public Channel<SettingsRequest> Requests { get; } = Channel.CreateUnbounded<SettingsRequest>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath == "/api/runtime-settings")
            {
                var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Requests.Writer.TryWrite(new SettingsRequest(request.Method, body));
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    private sealed record SettingsRequest(HttpMethod Method, string Body);
}
#endif
