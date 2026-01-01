// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Serilog;

using global::Serilog.Core;
using global::Serilog.Events;

using System.Collections.Concurrent;

/// <summary>
/// Custom Serilog sink that stores log entries in memory for display in MonitorPage.
/// Provides real-time log streaming to UI while Serilog handles persistence.
/// </summary>
public class InMemorySink : ILogEventSink
{
    private static readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private const int MaxLogEntries = 500;

    /// <summary>
    /// Event fired when a new log entry is added. Subscribe to display logs in UI.
    /// </summary>
    public static event Action<LogEntry>? LogAdded;

    /// <summary>
    /// Gets all log entries (newest first).
    /// </summary>
    public static IEnumerable<LogEntry> GetLogEntries() => _logEntries.Reverse();

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public static void ClearLogs() => _logEntries.Clear();

    public void Emit(LogEvent logEvent)
    {
        var severity = logEvent.Level switch
        {
            LogEventLevel.Verbose => LogSeverity.Debug,
            LogEventLevel.Debug => LogSeverity.Debug,
            LogEventLevel.Information => LogSeverity.Info,
            LogEventLevel.Warning => LogSeverity.Warning,
            LogEventLevel.Error => LogSeverity.Error,
            LogEventLevel.Fatal => LogSeverity.Error,
            _ => LogSeverity.Info
        };

        // Extract source context (logger name)
        var source = logEvent.Properties.TryGetValue("SourceContext", out var sourceContext)
            ? sourceContext.ToString().Trim('"').Split('.').LastOrDefault() ?? "Unknown"
            : "Unknown";

        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.DateTime,
            Severity = severity,
            Source = source,
            Message = logEvent.RenderMessage()
        };

        _logEntries.Enqueue(entry);

        // Keep only last N entries
        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }

        LogAdded?.Invoke(entry);
    }
}

/// <summary>
/// Log entry for UI display.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public LogSeverity Severity { get; init; } = LogSeverity.Info;
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");

    public string SeverityIcon => Severity switch
    {
        LogSeverity.Debug => "🔍",
        LogSeverity.Info => "ℹ️",
        LogSeverity.Warning => "⚠️",
        LogSeverity.Error => "❌",
        _ => "📝"
    };
}

/// <summary>
/// Log severity level.
/// </summary>
public enum LogSeverity
{
    Debug,
    Info,
    Warning,
    Error
}
