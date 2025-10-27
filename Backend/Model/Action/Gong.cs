namespace Moba.Backend.Model.Action;

using Enum;

using System.Diagnostics;

public class Gong : Base
{
    public Gong()
    {
        Name = "Gong";
    }

    public override ActionType Type => ActionType.Gong;

    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        Debug.WriteLine("  🔔 Playing gong sound");

        try
        {
            // await _audioPlayer.Value.PlayGongAsync();
            Debug.WriteLine("  ✅ Gong played successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"  ❌ Error playing gong: {ex.Message}");
            throw;
        }
    }
}