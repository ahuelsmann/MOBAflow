// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Solution - Pure Data Object.
/// NO BUSINESS LOGIC! UpdateFrom/Merge logic moved to SolutionService in Backend.
/// </summary>
public class Solution
{
    public Solution()
    {
        Name = "New Solution";
        Projects = [];
        Settings = new Settings();
    }

    public string Name { get; set; }
    public Settings Settings { get; set; }
    public List<Project> Projects { get; set; }
}
