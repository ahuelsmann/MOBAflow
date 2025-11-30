// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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

    public string Name { get; set; }
    public decimal ProsodyRate { get; set; }
}