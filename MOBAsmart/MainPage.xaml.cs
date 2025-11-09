using MOBAsmart.Services;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace MOBAsmart;

public partial class MainPage : ContentPage
{
    private readonly FeedbackService _feedbackService;
    private readonly string _serverUrl;

    public MainPage()
    {
        InitializeComponent();
        
        // Load configuration
        _serverUrl = LoadServerUrl();
        _feedbackService = new FeedbackService(_serverUrl);
        
        // Bind to the service's Statistics collection
        FeedbackList.ItemsSource = _feedbackService.Statistics;
        
        // Show configured server URL
        UpdateStatus($"Disconnected", Colors.Red);
        
        // Subscribe to connection events
        _feedbackService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private static string LoadServerUrl()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("MOBAsmart.appsettings.json");
            
            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                
                // Check if running on emulator
#if ANDROID
                var isEmulator = Android.OS.Build.Product?.Contains("sdk") == true;
                var serverUrl = isEmulator 
                    ? config["ServerUrlEmulator"] 
                    : config["ServerUrl"];
                
                System.Diagnostics.Debug.WriteLine($"Running on {(isEmulator ? "Emulator" : "Device")}, using URL: {serverUrl}");
                return serverUrl ?? "http://192.168.0.22:5001";
#else
                return config["ServerUrl"] ?? "http://192.168.0.22:5001";
#endif
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
        }
        
        return "http://192.168.0.22:5001";
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isConnected)
            {
                UpdateStatus($"Connected", Colors.Green);
                ConnectButton.Text = "Disconnect";
            }
            else
            {
                UpdateStatus($"Disconnected", Colors.Red);
                ConnectButton.Text = "Connect";
            }
        });
    }

    private void UpdateStatus(string message, Color color)
    {
        StatusLabel.Text = $"{message} ({_serverUrl})";
        StatusLabel.TextColor = color;
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        try
        {
            ConnectButton.IsEnabled = false;
            
            if (_feedbackService.IsConnected)
            {
                await DisconnectAsync();
            }
            else
            {
                await ConnectAsync();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}", Colors.Red);
#pragma warning disable CS0618 // Type or member is obsolete
            await DisplayAlert("Connection Error", ex.Message, "OK");
#pragma warning restore CS0618
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private async Task ConnectAsync()
    {
        UpdateStatus("Connecting...", Colors.Orange);
        
        try
        {
            await _feedbackService.ConnectAsync();
        }
        catch (HttpRequestException)
        {
            UpdateStatus("Server not reachable", Colors.Red);
            
#pragma warning disable CS0618 // Type or member is obsolete
            var retry = await DisplayAlert(
                "Connection Failed", 
                $"Cannot reach server at {_serverUrl}\n\nMake sure:\n1. FeedbackApi is running\n2. IP address is correct\n3. Firewall allows connection",
                "Retry",
                "Cancel");
#pragma warning restore CS0618
            
            if (retry)
            {
                await ConnectAsync();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Connection failed", Colors.Red);
#pragma warning disable CS0618 // Type or member is obsolete
            await DisplayAlert("Error", ex.Message, "OK");
#pragma warning restore CS0618
        }
    }

    private async Task DisconnectAsync()
    {
        await _feedbackService.DisconnectAsync();
        UpdateStatus("Disconnected", Colors.Red);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _feedbackService.ConnectionStateChanged -= OnConnectionStateChanged;
    }
}
