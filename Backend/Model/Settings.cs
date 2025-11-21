// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

using System.ComponentModel.DataAnnotations;

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

    // Z21 Configuration
    [Display(Name = "Current Z21 IP Address")]
    public string? CurrentIpAddress { get; set; }

    [Display(Name = "Z21 IP Address History")]
    public List<string> IpAddresses { get; set; }

    // Azure Speech Configuration
    [Display(Name = "Speech Key")]
    public string? SpeechKey { get; set; }

    [Display(Name = "Speech Region")]
    public string? SpeechRegion { get; set; }

    [Display(Name = "Speech Synthesizer Rate")]
    public int SpeechSynthesizerRate { get; set; }
    
    [Display(Name = "Speech Synthesizer Volume")]
    public uint SpeechSynthesizerVolume
    {
        get => _speechSynthesizerVolume;
        set => _speechSynthesizerVolume = Math.Clamp(value, 0, 100);
    }
    
    [Display(Name = "Speaker Engine Name")]
    public string? SpeakerEngineName { get; set; }
    
    [Display(Name = "Voice Name")]
    public string? VoiceName { get; set; }
    
    // Application Settings
    [Display(Name = "Reset Window Layout On Start")]
    public bool IsResetWindowLayoutOnStart { get; set; }
}