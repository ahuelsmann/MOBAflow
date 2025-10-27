namespace Moba.Backend.Model.Action;

using Enum;

using System.Diagnostics;

public class Command : Base
{
    public Command(byte[] bytes)
    {
        Bytes = bytes;
        Name = "New Command";
    }

    public override ActionType Type => ActionType.Command;

    public byte[]? Bytes { get; set; }

    public string? BytesString { get; set; }

    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        if (Bytes == null || Bytes.Length == 0)
        {
            Debug.WriteLine("  ⚠ Command has no bytes to send");
            return;
        }

        Debug.WriteLine($"  📤 Sending Z21 command: {BitConverter.ToString(Bytes)}");

        if (context.Z21 != null)
        {
            try
            {
                await context.Z21.SendCommandAsync(Bytes);
                Debug.WriteLine("  ✅ Command sent successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ Error sending command: {ex.Message}");
                throw;
            }
        }
        else
        {
            Debug.WriteLine("  ⚠ No Z21 connection available - command not sent");
        }
    }
}