// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

public enum ActionType
{
    Announcement,
    Command, // Digital command to the digital control unit, e.g. Z21.
    Audio,
    ExecutePowerShellScript,
}