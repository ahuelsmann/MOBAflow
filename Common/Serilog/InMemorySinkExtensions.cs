// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Serilog;

using global::Serilog;
using global::Serilog.Configuration;

/// <summary>
/// Extension methods for configuring the InMemorySink in Serilog.
/// </summary>
public static class InMemorySinkExtensions
{
    /// <summary>
    /// Adds the InMemorySink to the Serilog pipeline for real-time UI logging.
    /// </summary>
    public static LoggerConfiguration InMemory(
        this LoggerSinkConfiguration sinkConfiguration)
    {
        return sinkConfiguration.Sink(new InMemorySink());
    }
}
