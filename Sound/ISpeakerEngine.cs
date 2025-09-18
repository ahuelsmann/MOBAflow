namespace Moba.Common.Interface;

public interface ISpeakerEngine
{
    string Name { get; set; }
    Task AnnouncementAsync(string message, string? voiceName);
}