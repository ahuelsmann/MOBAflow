// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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
    private static readonly ConcurrentQueue<LogEntry> LogEntries = new();
    private const int MaxLogEntries = 500;

    /// <summary>
    /// Event fired when a new log entry is added. Subscribe to display logs in UI.
    /// </summary>
    public static event Action<LogEntry>? LogAdded;

    /// <summary>
    /// Gets all log entries (newest first).
    /// </summary>
    public static IEnumerable<LogEntry> GetLogEntries() => LogEntries.Reverse();

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public static void ClearLogs() => LogEntries.Clear();

    /// <summary>
    /// Emits a Serilog log event into the in-memory buffer and raises <see cref="LogAdded"/>.
    /// </summary>
    /// <param name="logEvent">The log event to capture.</param>
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

        LogEntries.Enqueue(entry);

        // Keep only last N entries
        while (LogEntries.Count > MaxLogEntries)
        {
            LogEntries.TryDequeue(out _);
        }

        LogAdded?.Invoke(entry);
    }
}

/// <summary>
/// Log entry for UI display.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// Gets the severity level of the log entry.
    /// </summary>
    public LogSeverity Severity { get; init; } = LogSeverity.Info;

    /// <summary>
    /// Gets the logical source of the log entry (logger or component name).
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp formatted for display in the UI.
    /// </summary>
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");

    /// <summary>
    /// Gets an icon string that represents the severity level.
    /// </summary>
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
    /// <summary>
    /// Diagnostic messages useful during development and debugging.
    /// </summary>
    Debug,

    /// <summary>
    /// Informational messages about normal application operation.
    /// </summary>
    Info,

    /// <summary>
    /// Warnings about unexpected but non-fatal situations.
    /// </summary>
    Warning,

    /// <summary>
    /// Error messages indicating failures or exceptions.
    /// </summary>
    Error
}