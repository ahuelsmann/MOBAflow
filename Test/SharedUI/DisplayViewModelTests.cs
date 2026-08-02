// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.Display.Protocol;
using Moba.Display.Transport;
using Moba.SharedUI.ViewModel;

[TestFixture]
[Category("Unit")]
public sealed partial class DisplayViewModelTests
{
    [Test]
    public void Constructor_Should_BlockNetworkCommands_WhenEndpointIsMissing()
    {
        using var client = new FakeDisplayDeviceClient();
        var viewModel = new DisplayViewModel(new AppSettings(), client);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsEndpointValid, Is.False);
            Assert.That(viewModel.ConnectionState, Is.EqualTo(DisplayConnectionState.NotConfigured));
            Assert.That(viewModel.NegotiationState, Is.EqualTo(DisplayNegotiationState.NotStarted));
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Unavailable));
            Assert.That(viewModel.CanConnect, Is.False);
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(viewModel.EndpointStatusText, Is.EqualTo("Configure a display IP address in Settings."));
        }
    }

    [Test]
    public void Constructor_Should_AllowOnlyNegotiation_WhenEndpointIsValid()
    {
        var settings = CreateConfiguredSettings();
        using var client = new FakeDisplayDeviceClient();
        var viewModel = new DisplayViewModel(settings, client);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsEndpointValid, Is.True);
            Assert.That(viewModel.ConfiguredEndpointText, Is.EqualTo("192.168.0.82:4210"));
            Assert.That(viewModel.CanConnect, Is.True);
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Unavailable));
        }
    }

    [Test]
    public async Task ConnectCommand_Should_ProjectLiveDiagnostics_WhenNegotiationSucceeds()
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ConnectionState, Is.EqualTo(DisplayConnectionState.Connected));
            Assert.That(viewModel.NegotiationState, Is.EqualTo(DisplayNegotiationState.Succeeded));
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Live));
            Assert.That(viewModel.CanSendStandardTestPattern, Is.True);
            Assert.That(viewModel.ResolutionText, Is.EqualTo("240 x 280"));
            Assert.That(viewModel.ProtocolVersionText, Is.EqualTo("1.0"));
            Assert.That(viewModel.FirmwareVersionText, Is.EqualTo("1.2.3"));
            Assert.That(viewModel.DeviceIdentityText, Is.EqualTo("esp32-s3"));
            Assert.That(viewModel.AdapterIdentityText, Is.EqualTo("st7789"));
            Assert.That(viewModel.HealthText, Is.EqualTo("Ready; 12 accepted, 1 rejected"));
        }
    }

    [Test]
    public async Task SynchronizeConfiguration_Should_MarkCapabilitiesStale_WhenEndpointChanges()
    {
        var settings = CreateConfiguredSettings();
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(settings, client);
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        settings.Display.Esp32IpAddress = "192.168.0.83";
        viewModel.SynchronizeConfiguration();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Stale));
            Assert.That(viewModel.NegotiationState, Is.EqualTo(DisplayNegotiationState.Stale));
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(client.DisconnectCount, Is.EqualTo(1));
            Assert.That(viewModel.CapabilityStatusText, Does.Contain("stale").IgnoreCase);
        }
    }

    [Test]
    public async Task Constructor_Should_NotRestoreLiveCapabilities_WhenOnlyEndpointWasPersisted()
    {
        var settings = CreateConfiguredSettings();
        using var firstClient = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var firstViewModel = new DisplayViewModel(settings, firstClient);
        await firstViewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using var restartedClient = new FakeDisplayDeviceClient();
        var restartedViewModel = new DisplayViewModel(settings, restartedClient);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(restartedViewModel.ConfiguredEndpointText, Is.EqualTo("192.168.0.82:4210"));
            Assert.That(restartedViewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Unavailable));
            Assert.That(restartedViewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(restartedViewModel.ResolutionText, Is.EqualTo("Not negotiated"));
        }
    }

    [Test]
    public async Task ConnectCommand_Should_ExplainDisabledControls_WhenOptionalCommandsAreMissing()
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(
                CreateCapabilities(DisplayOptionalCommandFlags.Clear)),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CanRenderBuiltInTestPattern, Is.False);
            Assert.That(viewModel.CanSetBrightness, Is.False);
            Assert.That(viewModel.BuiltInPatternAvailabilityText, Does.Contain("does not support"));
            Assert.That(viewModel.BrightnessAvailabilityText, Does.Contain("does not support"));
        }
    }

    [TestCase(
        DisplayPixelFormatFlags.None,
        DisplayRotationFlags.Degrees0,
        DisplayFrameCapabilityFlags.FullFrameStaging
            | DisplayFrameCapabilityFlags.RegionTransfer
            | DisplayFrameCapabilityFlags.AtomicPresentation,
        "RGB565")]
    [TestCase(
        DisplayPixelFormatFlags.Rgb565BigEndian,
        DisplayRotationFlags.Degrees180,
        DisplayFrameCapabilityFlags.FullFrameStaging
            | DisplayFrameCapabilityFlags.RegionTransfer
            | DisplayFrameCapabilityFlags.AtomicPresentation,
        "zero-degree")]
    [TestCase(
        DisplayPixelFormatFlags.Rgb565BigEndian,
        DisplayRotationFlags.Degrees0,
        DisplayFrameCapabilityFlags.FullFrameStaging,
        "atomic frame")]
    public async Task ConnectCommand_Should_BlockStandardPattern_WhenFrameCapabilitiesAreIncompatible(
        DisplayPixelFormatFlags pixelFormats,
        DisplayRotationFlags rotations,
        DisplayFrameCapabilityFlags frameCapabilities,
        string expectedExplanation)
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(
                CreateCapabilities(
                    pixelFormats: pixelFormats,
                    rotations: rotations,
                    frameCapabilities: frameCapabilities)),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(viewModel.StandardPatternAvailabilityText, Does.Contain(expectedExplanation));
        }
    }

    [Test]
    public async Task SendStandardTestPatternCommand_Should_ReportSuccess_WhenCapabilitiesAreLive()
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth()),
            OperationResult = DisplayDeviceOperationResult.Succeeded()
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        await viewModel.SendStandardTestPatternCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(client.StandardPatternSendCount, Is.EqualTo(1));
            Assert.That(viewModel.LastResultText, Is.EqualTo("Standard test pattern presented successfully."));
        }
    }

    [Test]
    public async Task ConnectCommand_Should_RemainFailClosed_WhenNegotiationTimesOut()
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Failed(
                DisplayRequestFailure.TimedOut,
                "No compatible response was received.")
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ConnectionState, Is.EqualTo(DisplayConnectionState.Offline));
            Assert.That(viewModel.NegotiationState, Is.EqualTo(DisplayNegotiationState.Failed));
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Unavailable));
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(viewModel.LastResultText, Does.Contain("timed out").IgnoreCase);
        }
    }

    [Test]
    public async Task ConnectCommand_Should_MarkPriorCapabilitiesStale_WhenRenegotiationFails()
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
        client.NegotiationResult = DisplayDeviceNegotiationResult.Failed(
            DisplayRequestFailure.TimedOut,
            "No response.");

        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ConnectionState, Is.EqualTo(DisplayConnectionState.Offline));
            Assert.That(viewModel.NegotiationState, Is.EqualTo(DisplayNegotiationState.Failed));
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Stale));
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(viewModel.CapabilityStatusText, Does.Contain("stale").IgnoreCase);
        }
    }

    [Test]
    public async Task SendStandardTestPatternCommand_Should_BlockFurtherSends_WhenSessionBecomesStale()
    {
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth()),
            OperationResult = DisplayDeviceOperationResult.Failed(
                DisplayRequestFailure.None,
                "The device restarted.",
                DisplayResultCode.WrongSession)
        };
        var viewModel = new DisplayViewModel(CreateConfiguredSettings(), client);
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

        await viewModel.SendStandardTestPatternCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Stale));
            Assert.That(viewModel.NegotiationState, Is.EqualTo(DisplayNegotiationState.Stale));
            Assert.That(viewModel.CanSendStandardTestPattern, Is.False);
            Assert.That(viewModel.LastResultText, Does.Contain("Reconnect"));
        }
    }

    [Test]
    public async Task ConnectCommand_Should_DiscardSuccess_WhenEndpointChangesInFlight()
    {
        var settings = CreateConfiguredSettings();
        var pendingNegotiation = new TaskCompletionSource<DisplayDeviceNegotiationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new FakeDisplayDeviceClient
        {
            ConnectHandler = (_, _) => pendingNegotiation.Task,
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(settings, client);

        var connectTask = viewModel.ConnectCommand.ExecuteAsync(null);
        settings.Display.Esp32IpAddress = "192.168.0.83";
        viewModel.SynchronizeConfiguration();
        pendingNegotiation.SetResult(DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()));
        await connectTask.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(client.LastConnectEndpoint?.ToString(), Is.EqualTo("192.168.0.82:4210"));
            Assert.That(viewModel.ConfiguredEndpointText, Is.EqualTo("192.168.0.83:4210"));
            Assert.That(viewModel.ConnectionState, Is.EqualTo(DisplayConnectionState.Disconnected));
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Unavailable));
            Assert.That(viewModel.HasLiveCapabilities, Is.False);
            Assert.That(client.DisconnectCount, Is.EqualTo(1));
            Assert.That(client.HealthQueryCount, Is.Zero);
        }
    }

    [Test]
    public async Task RefreshHealthCommand_Should_DiscardResult_WhenEndpointChangesInFlight()
    {
        var settings = CreateConfiguredSettings();
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(settings, client);
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
        var pendingHealth = new TaskCompletionSource<DisplayDeviceHealthResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        client.HealthHandler = _ => pendingHealth.Task;

        var healthTask = viewModel.RefreshHealthCommand.ExecuteAsync(null);
        settings.Display.Esp32IpAddress = "192.168.0.83";
        viewModel.SynchronizeConfiguration();
        pendingHealth.SetResult(DisplayDeviceHealthResult.Succeeded(CreateHealth()));
        await healthTask.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Stale));
            Assert.That(viewModel.HasLiveCapabilities, Is.False);
            Assert.That(viewModel.HealthText, Does.Contain("Stale"));
        }
    }

    [Test]
    public async Task SendStandardTestPatternCommand_Should_DiscardResult_WhenEndpointChangesInFlight()
    {
        var settings = CreateConfiguredSettings();
        using var client = new FakeDisplayDeviceClient
        {
            NegotiationResult = DisplayDeviceNegotiationResult.Succeeded(CreateCapabilities()),
            HealthResult = DisplayDeviceHealthResult.Succeeded(CreateHealth())
        };
        var viewModel = new DisplayViewModel(settings, client);
        await viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
        var pendingOperation = new TaskCompletionSource<DisplayDeviceOperationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        client.OperationHandler = _ => pendingOperation.Task;

        var operationTask = viewModel.SendStandardTestPatternCommand.ExecuteAsync(null);
        settings.Display.Esp32IpAddress = "192.168.0.83";
        viewModel.SynchronizeConfiguration();
        pendingOperation.SetResult(DisplayDeviceOperationResult.Succeeded());
        await operationTask.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CapabilityFreshness, Is.EqualTo(DisplayCapabilityFreshness.Stale));
            Assert.That(viewModel.HasLiveCapabilities, Is.False);
            Assert.That(viewModel.LastResultText, Is.Not.EqualTo("Standard test pattern presented successfully."));
        }
    }

    private static AppSettings CreateConfiguredSettings() =>
        new()
        {
            Display = new DisplaySettings
            {
                Esp32IpAddress = "192.168.0.82",
                Port = 4210
            }
        };

    private static CapabilitiesResponsePayload CreateCapabilities(
        DisplayOptionalCommandFlags optionalCommands =
            DisplayOptionalCommandFlags.Clear
            | DisplayOptionalCommandFlags.SetBrightness
            | DisplayOptionalCommandFlags.RenderTestPattern,
        DisplayPixelFormatFlags pixelFormats = DisplayPixelFormatFlags.Rgb565BigEndian,
        DisplayRotationFlags rotations = DisplayRotationFlags.Degrees0 | DisplayRotationFlags.Degrees180,
        DisplayFrameCapabilityFlags frameCapabilities =
            DisplayFrameCapabilityFlags.FullFrameStaging
            | DisplayFrameCapabilityFlags.RegionTransfer
            | DisplayFrameCapabilityFlags.AtomicPresentation) =>
        new(
            DisplayProtocol.CurrentVersion,
            240,
            280,
            DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH,
            DisplayProtocol.DEFAULT_MAX_PAYLOAD_LENGTH,
            pixelFormats,
            rotations,
            optionalCommands,
            frameCapabilities,
            DisplayAcknowledgementMode.ControlAndCompletion,
            42,
            "esp32-s3",
            "1.2.3",
            "st7789");

    private static HealthResponsePayload CreateHealth() =>
        new(DisplayHealthState.Ready, DisplayResultCode.Ok, 3600, 120000, 12, 1, 10);

    private sealed partial class FakeDisplayDeviceClient : IDisplayDeviceClient
    {
        public Func<DisplayEndpoint, CancellationToken, Task<DisplayDeviceNegotiationResult>>?
            ConnectHandler { get; set; }

        public Func<CancellationToken, Task<DisplayDeviceHealthResult>>? HealthHandler { get; set; }

        public Func<CancellationToken, Task<DisplayDeviceOperationResult>>? OperationHandler { get; set; }

        public DisplayDeviceNegotiationResult NegotiationResult { get; set; } =
            DisplayDeviceNegotiationResult.Failed(DisplayRequestFailure.TimedOut, "No response.");

        public DisplayDeviceHealthResult HealthResult { get; set; } =
            DisplayDeviceHealthResult.Failed(DisplayRequestFailure.TimedOut, "No response.");

        public DisplayDeviceOperationResult OperationResult { get; set; } =
            DisplayDeviceOperationResult.Succeeded();

        public int DisconnectCount { get; private set; }

        public int StandardPatternSendCount { get; private set; }

        public int HealthQueryCount { get; private set; }

        public DisplayEndpoint? LastConnectEndpoint { get; private set; }

        public Task<DisplayDeviceNegotiationResult> ConnectAsync(
            DisplayEndpoint endpoint,
            CancellationToken cancellationToken = default)
        {
            LastConnectEndpoint = endpoint;
            return ConnectHandler?.Invoke(endpoint, cancellationToken)
                ?? Task.FromResult(NegotiationResult);
        }

        public Task<DisplayDeviceHealthResult> QueryHealthAsync(
            CancellationToken cancellationToken = default)
        {
            HealthQueryCount++;
            return HealthHandler?.Invoke(cancellationToken) ?? Task.FromResult(HealthResult);
        }

        public Task<DisplayDeviceOperationResult> SendStandardTestPatternAsync(
            CancellationToken cancellationToken = default)
        {
            StandardPatternSendCount++;
            return OperationHandler?.Invoke(cancellationToken) ?? Task.FromResult(OperationResult);
        }

        public Task<DisplayDeviceOperationResult> RenderBuiltInTestPatternAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult);

        public Task<DisplayDeviceOperationResult> SetBrightnessAsync(
            byte percentage,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult);

        public void Disconnect()
        {
            DisconnectCount++;
        }

        public void Dispose()
        {
        }
    }
}
