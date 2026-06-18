// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Extensions;

using Common.Serilog;

using Serilog;
using Serilog.Events;

/// <summary>
/// Configures Serilog for the WinUI host with async file logging and in-memory sink for MonitorPage.
/// </summary>
internal static class WinUiSerilogConfigurator
{
    private static bool _configured;

    /// <summary>
    /// Configures Serilog with async file logging, environment and process enrichment.
    /// Uses Async-Sink for non-blocking file I/O, InMemory sink for MonitorPage display.
    /// </summary>
    public static void EnsureConfigured()
    {
        if (_configured)
        {
            return;
        }

        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .WriteTo.InMemory()
            .WriteTo.Async(a => a.File(
                Path.Combine(logDirectory, "mobaflow-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{MachineName}] [{ProcessId}:{ProcessName}] [{ThreadId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"),
                bufferSize: 1000)
            .CreateLogger();

        _configured = true;
    }
}
