// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

public interface ISpeakerEngine
{
    string Name { get; set; }
    Task AnnouncementAsync(string message, string? voiceName);
}
