using System.Net;

namespace Moba.Backend.Interface;

public delegate void Feedback(FeedbackResult feedbackContent);
public delegate void SystemStateChanged(SystemState systemState);
public delegate void XBusStatusChanged(Moba.Backend.Protocol.XBusStatus status);

public interface IZ21 : IDisposable
{
    event Feedback? Received;
    event SystemStateChanged? OnSystemStateChanged;
    event XBusStatusChanged? OnXBusStatusChanged;

    bool IsConnected { get; }

    Task ConnectAsync(IPAddress address, CancellationToken cancellationToken = default);
    Task DisconnectAsync();

    Task SendCommandAsync(byte[] sendBytes);

    void SimulateFeedback(int inPort);
}
