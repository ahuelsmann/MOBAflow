// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging;

namespace Moba.Common.Extensions;

/// <summary>
/// Extension methods for unified logging across the application.
/// Provides triple logging to Console, Debug output, and ILogger for maximum visibility.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs a message to Console, Debug output, and optionally to ILogger.
    /// Use this for important events: connections, feedback, errors, user actions.
    /// </summary>
    /// <param name="source">The object that is logging (typically 'this')</param>
    /// <param name="message">The message to log (supports emojis for better readability)</param>
    /// <param name="logger">Optional ILogger instance for structured logging</param>
    /// <example>
    /// <code>
    /// this.Log("🔊 Processing order 123", _logger);
    /// this.Log($"❌ Connection failed: {ex.Message}");
    /// </code>
    /// </example>
    public static void Log(this object source, string message, ILogger? logger = null)
    {
        // Console.WriteLine: visible in tests, CI/CD, containers
        Console.WriteLine(message);

        // Debug.WriteLine: visible in Visual Studio Output Window
        System.Diagnostics.Debug.WriteLine(message);

        // Structured logging via ILogger
        logger?.LogInformation("{Message}", message);
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// Automatically formats the exception for better readability.
    /// </summary>
    /// <param name="source">The object that is logging</param>
    /// <param name="message">The error message context</param>
    /// <param name="exception">The exception to log</param>
    /// <param name="logger">Optional ILogger instance for structured logging</param>
    public static void LogError(this object source, string message, Exception exception, ILogger? logger = null)
    {
        var errorMessage = $"❌ {message}: {exception.Message}";
        Console.WriteLine(errorMessage);
        System.Diagnostics.Debug.WriteLine(errorMessage);

        // Log full exception details to Debug only (not to Console to avoid clutter)
        System.Diagnostics.Debug.WriteLine($"   Exception: {exception}");

        // Structured logging with exception
        logger?.LogError(exception, "{Message}", message);
    }

    /// <summary>
    /// Logs a warning message.
    /// Use for non-critical issues that should be investigated.
    /// </summary>
    /// <param name="source">The object that is logging</param>
    /// <param name="message">The warning message</param>
    /// <param name="logger">Optional ILogger instance for structured logging</param>
    public static void LogWarning(this object source, string message, ILogger? logger = null)
    {
        var warningMessage = $"⚠️ {message}";
        Console.WriteLine(warningMessage);
        System.Diagnostics.Debug.WriteLine(warningMessage);

        // Structured logging
        logger?.LogWarning("{Message}", message);
    }
}
