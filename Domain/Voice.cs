// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

public class Voice
{
    /// <summary>
    /// Represents a specific voice for speech output.
    /// </summary>
    public Voice()
    {
        Name = "ElkeNeural";
    }

    /// <summary>
    /// Gets or sets the name of the voice (e.g., Azure voice name).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the prosody rate factor (speed of speech).
    /// </summary>
    public decimal ProsodyRate { get; set; }
}