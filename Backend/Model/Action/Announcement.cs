namespace Moba.Backend.Model.Action;

using Enum;

public class Announcement : Base
{
    public Announcement(string textToSpeak)
    {
        TextToSpeak = textToSpeak;
        Name = "New Announcement";
    }

    public override ActionType Type => ActionType.Announcement;

    public string? TextToSpeak { get; set; }
}