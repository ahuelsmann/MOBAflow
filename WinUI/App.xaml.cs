// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Backend.Extensions;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;
using Common.Serilog;

using Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;

using Serilog;
using Serilog.Events;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using Sound;

using System.Diagnostics;
using System.IO;
using System.Linq;

using View;

using ILogger = Microsoft.Extensions.Logging.ILogger;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;
    private Process? _webAppProcess;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        try
        {
            Services = ConfigureServices();
            InitializeComponent();
        }
        catch (Exception ex)
        {
            // Log the exception before crashing
            Debug.WriteLine("🚨 FATAL ERROR during App initialization:");
            Debug.WriteLine($"   Exception Type: {ex.GetType().Name}");
            Debug.WriteLine($"   Message: {ex.Message}");
            Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                Debug.WriteLine($"   Inner StackTrace: {ex.InnerException.StackTrace}");
            }

            // Re-throw to get Windows Error Reporting
            throw;
        }
    }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Load appsettings.json configuration
        // In DEBUG mode, also load appsettings.Development.json to enable all feature toggles
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
            .Build();

        // Register IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Register AppSettings with IOptions pattern
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // Register SpeechOptions (Sound service configuration)
        services.Configure<SpeechOptions>(configuration.GetSection("Speech"));

        // ✅ Configure Serilog for file logging
        ConfigureSerilog();

        // Logging (required by HealthCheckService and SpeechHealthCheck)
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });

        // ✅ Register ISpeakerEngine with lazy initialization
        // Only creates the CONFIGURED engine, not both
        // Decision is made at registration time based on AppSettings
        services.AddSingleton<ISpeakerEngine>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            sp.GetService<ILogger>();

            // Check if Azure Cognitive Services is configured
            var selectedEngine = settings.Speech.SpeakerEngineName;
            if (!string.IsNullOrEmpty(selectedEngine) &&
                selectedEngine.Contains("Azure", StringComparison.OrdinalIgnoreCase))
            {
                // Only create Azure engine if explicitly configured
                var options = sp.GetRequiredService<IOptions<SpeechOptions>>();
                return new CognitiveSpeechEngine(options, sp.GetService<ILogger<CognitiveSpeechEngine>>()!);
            }

            // Default: Windows SAPI (always available, no Azure SDK needed)
            return new SystemSpeechEngine(sp.GetService<ILogger<SystemSpeechEngine>>()!);
        });

        // Backend Services (Interfaces are in Backend.Interface and Backend.Network)
        // ✅ Use shared extension method for platform-consistent registration
        // Backend now registers: ActionExecutionContext, AnnouncementService, ActionExecutor
        services.AddMobaBackendServices();

        services.AddSingleton<IIoService, IoService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();

        // ✅ ICityService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<ICityService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                return new CityService(appSettings);
            }
            catch
            {
                // Fallback to NullObject if city data unavailable
                return new NullCityService();
            }
        });

        // ✅ ISettingsService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<ISettingsService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                return new SettingsService(appSettings);
            }
            catch
            {
                // Fallback to NullObject if settings file unavailable
                return new NullSettingsService();
            }
        });

        services.AddSingleton<NavigationService>();
        services.AddSingleton<SnapToConnectService>();

        // Sound Services (required by HealthCheckService)
        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<SpeechHealthCheck>();
        services.AddSingleton<HealthCheckService>();

        // ViewModels
        // Note: Wrapper ViewModels (SolutionViewModel, ProjectViewModel, JourneyViewModel, etc.)
        // are created with 'new' at runtime because they wrap Domain models.
        // Only "standalone" ViewModel that don't wrap models are registered here.

        // Domain.Solution is registered in AddMobaBackendServices()

        services.AddSingleton(sp => new MainWindowViewModel(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<WorkflowService>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<Solution>(),
            sp.GetRequiredService<ActionExecutionContext>(),  // ✅ Inject context with all audio services
            sp.GetRequiredService<ILogger<MainWindowViewModel>>(),  // ✅ Inject ILogger
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<ICityService>(),      // Now guaranteed (NullObject if unavailable)
            sp.GetRequiredService<ISettingsService>(),  // Now guaranteed (NullObject if unavailable)
            sp.GetRequiredService<AnnouncementService>()  // For TestSpeech command
        ));
        services.AddSingleton<TrackPlanEditorViewModel>();
        services.AddSingleton<JourneyMapViewModel>();
        services.AddSingleton<MonitorPageViewModel>();

        // Pages (Transient = new instance per navigation)
        services.AddTransient<OverviewPage>();
        services.AddTransient<SolutionPage>();
        services.AddTransient<JourneysPage>();
        services.AddTransient<WorkflowsPage>();
        services.AddTransient<TrainsPage>();
        services.AddTransient<TrackPlanEditorPage>();
        services.AddTransient<JourneyMapPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<MonitorPage>();

        // MainWindow (Singleton = one instance for app lifetime)
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures Serilog for file logging to bin\Debug\logs folder and in-memory sink for MonitorPage.
    /// </summary>
    private static void ConfigureSerilog()
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.InMemory()  // ✅ Custom sink for MonitorPage real-time display
            .WriteTo.File(
                Path.Combine(logDirectory, "mobaflow-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();

        // Optionally start WebApp (REST/API) alongside WinUI
        _ = StartWebAppIfEnabledAsync();

        // Auto-load last solution if enabled
        _ = AutoLoadLastSolutionAsync(((MainWindow)_window).ViewModel);
    }

    /// <summary>
    /// Automatically loads the last used solution if AutoLoadLastSolution preference is enabled.
    /// Delegates to MainWindowViewModel.LoadSolutionFromPathAsync() to ensure all initialization happens correctly.
    /// </summary>
    private async Task AutoLoadLastSolutionAsync(MainWindowViewModel mainWindowViewModel)
    {
        try
        {
            var settingsService = Services.GetService<ISettingsService>();
            if (settingsService == null)
            {
                Debug.WriteLine("⚠️ SettingsService not available - skipping auto-load");
                return;
            }

            if (!settingsService.AutoLoadLastSolution)
            {
                Debug.WriteLine("ℹ️ Auto-load disabled - skipping");
                return;
            }

            var lastPath = settingsService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                Debug.WriteLine("ℹ️ No last solution path - skipping auto-load");
                return;
            }

            if (!File.Exists(lastPath))
            {
                Debug.WriteLine($"⚠️ Last solution file not found: {lastPath}");
                return;
            }

            Debug.WriteLine($"📂 Auto-loading last solution: {lastPath}");

            // ✅ Use the SAME code path as manual loading!
            // This ensures JourneyManager and all other initialization happens correctly.
            await mainWindowViewModel.LoadSolutionFromPathAsync(lastPath);

            Debug.WriteLine($"✅ Auto-load completed: {lastPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Auto-load failed: {ex.Message}");
            // Don't crash the application if auto-load fails
        }
    }

    private async Task StartWebAppIfEnabledAsync()
    {
        try
        {
            var settings = Services.GetRequiredService<AppSettings>();
            var restPort = settings.RestApi.Port;
            if (!settings.Application.AutoStartWebApp)
            {
                Debug.WriteLine("ℹ️ AutoStartWebApp disabled in settings - skipping WebApp launch");
                Debug.WriteLine("   To enable: Set 'Application.AutoStartWebApp' to true in appsettings.json");
                return;
            }

            if (_webAppProcess is not null && !_webAppProcess.HasExited)
            {
                Debug.WriteLine("ℹ️ WebApp already running (PID: {0})", _webAppProcess.Id);
                return;
            }

            // ✅ Ensure Windows Firewall rules exist for WebApp
            FirewallHelper.EnsureFirewallRulesExist(restPort);

            // Search for WebApp.dll in common locations
            var candidates = new[]
            {
                // 1. Same directory as WinUI (published/deployment scenario)
                Path.Combine(AppContext.BaseDirectory, "WebApp.dll"),
                
                // 2. Development: Debug build (sibling project in MOBAflow workspace)
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "WebApp", "bin", "Debug", "net10.0", "WebApp.dll")),
                
                // 3. Development: Release build (sibling project in MOBAflow workspace)
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "WebApp", "bin", "Release", "net10.0", "WebApp.dll"))
            };


            Debug.WriteLine("🔍 Searching for WebApp.dll in {0} locations...", candidates.Length);
            
            string? dllPath = null;
            foreach (var candidate in candidates)
            {
                Debug.WriteLine("   Checking: {0}", candidate);
                if (File.Exists(candidate))
                {
                    dllPath = candidate;
                    Debug.WriteLine("   ✅ Found!");
                    break;
                }
            }

            if (dllPath is null)
            {
                Debug.WriteLine("⚠️ WebApp.dll not found in any location - skipping auto-start");
                Debug.WriteLine("   Searched locations:");
                foreach (var candidate in candidates)
                {
                    Debug.WriteLine("   - {0}", candidate);
                }
                Debug.WriteLine("   Build the WebApp project or disable AutoStartWebApp in settings");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\"",
                WorkingDirectory = Path.GetDirectoryName(dllPath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Override Kestrel URL to the configured REST port (ensures we don't bind to stale appsettings)
            psi.Environment["ASPNETCORE_URLS"] = $"http://0.0.0.0:{restPort}";
            psi.Environment["ASPNETCORE_HTTP_PORTS"] = restPort.ToString();

            _webAppProcess = Process.Start(psi);
            
            if (_webAppProcess is not null)
            {
                Debug.WriteLine("✅ WebApp started successfully");
                Debug.WriteLine("   Path: {0}", dllPath);
                Debug.WriteLine("   PID: {0}", _webAppProcess.Id);
                Debug.WriteLine($"   REST API will be available on http://localhost:{restPort}");
                Debug.WriteLine("   UDP Discovery Service listening on port 21106");
                Debug.WriteLine("   💡 If MAUI can't discover server, check Windows Firewall settings");
                
                // Log WebApp output for debugging
                _ = Task.Run(async () =>
                {
                    while (!_webAppProcess.HasExited)
                    {
                        var output = await _webAppProcess.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.WriteLine($"[WebApp] {output}");
                        }
                    }
                });
            }
            else
            {
                Debug.WriteLine("❌ Failed to start WebApp process");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to start WebApp: {ex.GetType().Name} - {ex.Message}");
            Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            // Do not crash WinUI if WebApp start fails
        }
    }
}