// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Extensions;

using Microsoft.Extensions.Configuration;

using Serilog;

/// <summary>
/// Builds application configuration from appsettings, user secrets, and optional Azure App Configuration.
/// </summary>
internal static class WinUiConfigurationBuilder
{
    /// <summary>
    /// Loads configuration for the WinUI host from standard sources.
    /// </summary>
    public static IConfiguration Build()
    {
        var basePath = AppContext.BaseDirectory;
        var devJsonPath = Path.Combine(basePath, "appsettings.Development.json");
        var devJsonExists = File.Exists(devJsonPath);

        Log.Debug("CONFIG BaseDirectory: {BasePath}", basePath);
        Log.Debug("CONFIG appsettings.Development.json exists: {Exists}", devJsonExists);

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
#endif
            ;

#if DEBUG
        configBuilder.AddUserSecrets<App>(optional: true);
        Log.Debug("CONFIG User Secrets loaded (if configured)");
#endif

        var azureAppConfigConnection = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(azureAppConfigConnection))
        {
            try
            {
                configBuilder.AddAzureAppConfiguration(azureAppConfigConnection);
                Log.Debug("CONFIG Azure App Configuration loaded");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "CONFIG Azure App Configuration failed");
            }
        }
        else
        {
            Log.Debug("CONFIG Azure App Configuration skipped (no connection string)");
        }

        var configuration = configBuilder.Build();

        Log.Debug(
            "CONFIG IsTrainControlPageAvailable: {Value}",
            configuration["FeatureToggles:IsTrainControlPageAvailable"]);
        Log.Debug(
            "CONFIG IsTrackPlanEditorPageAvailable: {Value}",
            configuration["FeatureToggles:IsTrackPlanEditorPageAvailable"]);

        return configuration;
    }
}
