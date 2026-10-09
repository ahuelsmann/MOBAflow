// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Rendering;
using Moba.Display.Runtime;
using Moba.Display.Transport;

using System.Globalization;

/// <summary>
/// Maintainer-run sustained-refresh acceptance for Issue #36 on the reference ESP32-S3 display.
/// Explicit, so CI and normal test runs never contact hardware. Run it only with a flashed device on the network:
/// <c>MOBADISPLAY_IP=&lt;address&gt; dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
/// --filter "FullyQualifiedName~SustainedRefreshHardwareTests"</c>.
/// Optional: <c>MOBADISPLAY_PORT</c> (default 4210), <c>MOBADISPLAY_SOAK_MINUTES</c> (default 120) and
/// <c>MOBADISPLAY_REFRESH_HZ</c> (default: the MOBAflow refresh rate). A shorter run or another rate is reported as
/// "not an acceptance run". Cancelling the test run stops sending frames.
/// </summary>
[TestFixture]
[Category("Hardware")]
[Explicit("Drives the real display for the sustained-refresh acceptance; maintainer-run only.")]
internal sealed class SustainedRefreshHardwareTests
{
    private static readonly TimeSpan HealthTimeout = TimeSpan.FromSeconds(10);

    [Test]
    public async Task SustainedRefresh_MeetsAcceptanceThresholds()
    {
        var endpoint = ReadEndpoint();
        var duration = TimeSpan.FromMinutes(ReadInt("MOBADISPLAY_SOAK_MINUTES", 120));
        var normalRefreshHz = new FrameLoopOptions().RefreshHz;
        var refreshHz = ReadInt("MOBADISPLAY_REFRESH_HZ", normalRefreshHz);
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var options = new FrameLoopOptions
        {
            IpAddress = endpoint.Address.ToString(),
            Port = endpoint.Port,
            RefreshHz = refreshHz
        };

        var healthBefore = await QueryHealthAsync(endpoint, cancellationToken).ConfigureAwait(false);

        long rendered = 0, presented = 0, failed = 0, recoveries = 0;
        var lastFailed = false;
        using var renderer = new SkiaFrameRenderer();
        using var sender = new UdpDisplayFrameSender();
        var scheduler = new FrameLoopScheduler(renderer, sender);
        scheduler.FrameReady += (_, _) => rendered++;
        scheduler.FrameTransmissionCompleted += (_, e) =>
        {
            if (e.Success)
            {
                presented++;
                recoveries += lastFailed ? 1 : 0;
            }
            else
            {
                failed++;
                TestContext.Out.WriteLine($"{e.Timestamp:HH:mm:ss.fff} frame failed: {e.FailureMessage}");
            }

            lastFailed = !e.Success;
        };

        await scheduler.StartAsync(options).ConfigureAwait(false);
        try
        {
            await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scheduler.StopAsync().ConfigureAwait(false);
        }

        var healthAfter = await QueryHealthAsync(endpoint, cancellationToken).ConfigureAwait(false);
        var report = new SustainedRefreshReport(
            duration, refreshHz, normalRefreshHz, rendered, presented, failed, recoveries, healthBefore, healthAfter);
        await TestContext.Out.WriteLineAsync(report.Format()).ConfigureAwait(false);

        Assert.That(report.Passed, Is.True, report.Format());
    }

    private static async Task<HealthResponsePayload> QueryHealthAsync(
        DisplayEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(HealthTimeout);
        using var client = new UdpDisplayDeviceClient();
        var negotiation = await client.ConnectAsync(endpoint, timeout.Token).ConfigureAwait(false);
        Assert.That(negotiation.IsSuccessful, Is.True, $"Negotiation failed: {negotiation.Diagnostic}");
        var health = await client.QueryHealthAsync(timeout.Token).ConfigureAwait(false);
        Assert.That(health.IsSuccessful, Is.True, $"Health query failed: {health.Diagnostic}");
        return health.Health!.Value;
    }

    private static DisplayEndpoint ReadEndpoint()
    {
        var address = Environment.GetEnvironmentVariable("MOBADISPLAY_IP");
        var port = ReadInt("MOBADISPLAY_PORT", new FrameLoopOptions().Port);
        Assert.That(
            DisplayEndpoint.TryCreate(address, port, out var endpoint, out var error),
            Is.True,
            $"Set MOBADISPLAY_IP (and optionally MOBADISPLAY_PORT) to the display endpoint: {error}");
        return endpoint!;
    }

    private static int ReadInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        Assert.That(
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0,
            Is.True,
            $"{name} must be a positive whole number.");
        return parsed;
    }
}
