using MOBAsmart.Services;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace MOBAsmart;

public partial class MainPage : ContentPage
{
    private readonly Z21FeedbackService _feedbackService;
    private readonly string _z21IpAddress;

    public MainPage()
    {
        InitializeComponent();
        
        // Load configuration
        _z21IpAddress = LoadZ21IpAddress();
        _feedbackService = new Z21FeedbackService();
        
        // Bind to the service's Statistics collection
        FeedbackList.ItemsSource = _feedbackService.Statistics;
        
        // Show configured Z21 IP address
        UpdateStatus($"Disconnected", Colors.Red);
        
        // Subscribe to connection events
        _feedbackService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private static string LoadZ21IpAddress()
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
                
                var ipAddress = config["Z21IpAddress"];
                
                System.Diagnostics.Debug.WriteLine($"Loaded Z21 IP address from config: {ipAddress}");
                return ipAddress ?? "192.168.0.111";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
        }
        
        // Default Z21 IP address
        return "192.168.0.111";
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isConnected)
            {
                UpdateStatus($"Connected to Z21", Colors.Green);
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
        StatusLabel.Text = $"{message} ({_z21IpAddress})";
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
        UpdateStatus("Connecting to Z21...", Colors.Orange);
        
        try
        {
            await _feedbackService.ConnectAsync(_z21IpAddress);
            System.Diagnostics.Debug.WriteLine($"✅ Successfully connected to Z21 at {_z21IpAddress}");
        }
        catch (Exception ex)
        {
            UpdateStatus("Connection failed", Colors.Red);
            
#pragma warning disable CS0618 // Type or member is obsolete
            var retry = await DisplayAlert(
                "Connection Failed", 
                $"Cannot connect to Z21 at {_z21IpAddress}\n\nMake sure:\n1. Z21 is powered on\n2. IP address is correct\n3. Device is in the same network\n\nError: {ex.Message}",
                "Retry",
                "Cancel");
#pragma warning restore CS0618
            
            if (retry)
            {
                await ConnectAsync();
            }
        }
    }

    private async Task DisconnectAsync()
    {
        UpdateStatus("Disconnecting...", Colors.Orange);
        await _feedbackService.DisconnectAsync();
        UpdateStatus("Disconnected", Colors.Red);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _feedbackService.ConnectionStateChanged -= OnConnectionStateChanged;
        _feedbackService.Dispose();
    }
}
