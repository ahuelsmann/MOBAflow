// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;



/// <summary>
/// This class represents general MOBA project related settings.
/// Includes global configuration for Z21 connection, speech synthesis, and application behavior.
/// </summary>
public class Settings
{
    private uint _speechSynthesizerVolume = 90;

    public Settings()
    {
        SpeechSynthesizerRate = -1;
        SpeechSynthesizerVolume = 90;
        IpAddresses = [];
    }

    public string? CurrentIpAddress { get; set; }

    public string? DefaultPort { get; set; }

    public List<string> IpAddresses { get; set; }

    // Azure Speech Configuration
    public string? SpeechKey { get; set; }

    public string? SpeechRegion { get; set; }

    public int SpeechSynthesizerRate { get; set; }
    
    public uint SpeechSynthesizerVolume
    {
        get => _speechSynthesizerVolume;
        set => _speechSynthesizerVolume = Math.Clamp(value, 0, 100);
    }
    
    public string? SpeakerEngineName { get; set; }
    
    public string? VoiceName { get; set; }
    
    // Application Settings
    public bool IsResetWindowLayoutOnStart { get; set; }
}