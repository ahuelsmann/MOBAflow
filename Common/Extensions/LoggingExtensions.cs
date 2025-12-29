// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Extensions;

using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Diagnostics;

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

/// <summary>
/// Simple log entry for in-memory storage.
/// Defined in Common so it can be used across all layers.
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

    public override string ToString() => $"[{TimestampFormatted}] [{Source}] {Message}";
}

/// <summary>
/// Extension methods for unified logging across the application.
/// Provides triple logging to Console, Debug output, and ILogger for maximum visibility.
/// Also stores logs in memory for display in MonitorPage.
/// </summary>
public static class LoggingExtensions
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

    /// <summary>
    /// Logs a message to Console, Debug output, and optionally to ILogger.
    /// Use this for important events: connections, feedback, errors, user actions.
    /// </summary>
    public static void Log(this object source, string message, ILogger? logger = null)
    {
        var sourceName = source.GetType().Name;
        AddLogEntry(LogSeverity.Info, sourceName, message);

        Console.WriteLine(message);
        Debug.WriteLine(message);
        logger?.LogInformation("{Message}", message);
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    public static void LogError(this object source, string message, Exception exception, ILogger? logger = null)
    {
        var sourceName = source.GetType().Name;
        var errorMessage = $"{message}: {exception.Message}";
        AddLogEntry(LogSeverity.Error, sourceName, errorMessage);

        Console.WriteLine($"❌ {errorMessage}");
        Debug.WriteLine($"❌ {errorMessage}");
        Debug.WriteLine($"   Exception: {exception}");
        logger?.LogError(exception, "{Message}", message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void LogWarning(this object source, string message, ILogger? logger = null)
    {
        var sourceName = source.GetType().Name;
        AddLogEntry(LogSeverity.Warning, sourceName, message);

        Console.WriteLine($"⚠️ {message}");
        Debug.WriteLine($"⚠️ {message}");
        logger?.LogWarning("{Message}", message);
    }

    private static void AddLogEntry(LogSeverity severity, string source, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Severity = severity,
            Source = source,
            Message = message
        };
        _logEntries.Enqueue(entry);

        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }

        LogAdded?.Invoke(entry);
    }
}