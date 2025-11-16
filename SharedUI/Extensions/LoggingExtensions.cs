namespace Moba.SharedUI.Extensions;

/// <summary>
/// Extension methods for unified logging across the application.
/// Provides dual logging to Console and Debug output for maximum visibility.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs a message to both Console and Debug output.
    /// Use this for important events: connections, feedback, errors, user actions.
    /// </summary>
    /// <param name="source">The object that is logging (typically 'this')</param>
    /// <param name="message">The message to log (supports emojis for better readability)</param>
    /// <example>
    /// <code>
    /// this.Log("🔊 Processing order 123");
    /// this.Log($"❌ Connection failed: {ex.Message}");
    /// </code>
    /// </example>
    public static void Log(this object source, string message)
    {
        // Console.WriteLine: visible in tests, CI/CD, containers
        Console.WriteLine(message);
        
        // Debug.WriteLine: visible in Visual Studio Output Window
        System.Diagnostics.Debug.WriteLine(message);
        
        // Future: Add ILogger support here
        // var logger = source.GetLogger();
        // logger?.LogInformation(message);
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// Automatically formats the exception for better readability.
    /// </summary>
    /// <param name="source">The object that is logging</param>
    /// <param name="message">The error message context</param>
    /// <param name="exception">The exception to log</param>
    public static void LogError(this object source, string message, Exception exception)
    {
        var errorMessage = $"❌ {message}: {exception.Message}";
        Console.WriteLine(errorMessage);
        System.Diagnostics.Debug.WriteLine(errorMessage);
        
        // Log full exception details to Debug only (not to Console to avoid clutter)
        System.Diagnostics.Debug.WriteLine($"   Exception: {exception}");
    }

    /// <summary>
    /// Logs a warning message.
    /// Use for non-critical issues that should be investigated.
    /// </summary>
    /// <param name="source">The object that is logging</param>
    /// <param name="message">The warning message</param>
    public static void LogWarning(this object source, string message)
    {
        var warningMessage = $"⚠️ {message}";
        Console.WriteLine(warningMessage);
        System.Diagnostics.Debug.WriteLine(warningMessage);
    }
}
