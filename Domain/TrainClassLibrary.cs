// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json;
using System.Text;

/// <summary>
/// Loads German locomotive classes from the master data file (data.json) and provides lookup/parsing functionality.
/// Supports flexible input matching: "110", "BR110", "E 10", "103.1", etc. all resolve to correct series.
/// </summary>
public static class TrainClassLibrary
{
    private static List<LocomotiveSeries> _allLocomotives = new();
    private static bool _isInitialized;

    /// <summary>
    /// Initialize the library by loading from the master data file (data.json).
    /// Call this during app startup after the shared master data file (e.g. data.json) is loaded.
    /// </summary>
    /// <param name="jsonPath">Path to data.json</param>
    public static void Initialize(string jsonPath)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            using var root = JsonDocument.Parse(json);
            // data.json uses lowercase "locomotives"; accept both for compatibility.
            if (!root.RootElement.TryGetProperty("Locomotives", out var locomotivesProp))
                root.RootElement.TryGetProperty("locomotives", out locomotivesProp);

            if (locomotivesProp.ValueKind != JsonValueKind.Array)
                return;

            var loadedLocomotives = new List<LocomotiveSeries>();
            foreach (var categoryElement in locomotivesProp.EnumerateArray())
            {
                if (!categoryElement.TryGetProperty("Series", out var seriesProp))
                    continue;
                foreach (var seriesElement in seriesProp.EnumerateArray())
                {
                    var locomotive = new LocomotiveSeries
                    {
                        Name = seriesElement.TryGetProperty("Name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
                        Vmax = seriesElement.TryGetProperty("Vmax", out var v) ? v.GetInt32() : 0,
                        Type = seriesElement.TryGetProperty("Type", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                        Epoch = seriesElement.TryGetProperty("Epoch", out var e) ? e.GetString() ?? string.Empty : string.Empty,
                        Description = seriesElement.TryGetProperty("Description", out var d) ? d.GetString() ?? string.Empty : string.Empty
                    };

                    loadedLocomotives.Add(locomotive);
                }
            }

            _allLocomotives = loadedLocomotives;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize TrainClassLibrary from {jsonPath}", ex);
        }
    }

    /// <summary>
    /// Tries to find a locomotive series by flexible input matching.
    /// Supports: "110", "BR 110", "BR110", "br110", "E 10", "110.3", "103.1", etc.
    /// Returns first match found, prioritizing exact/prefix matches over partial matches.
    /// </summary>
    /// <param name="classNumber">User input (e.g., "110", "BR 110", "E 10")</param>
    /// <returns>LocomotiveSeries if found; otherwise null</returns>
    public static LocomotiveSeries? TryGetByClassNumber(string classNumber)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TrainClassLibrary not initialized. Call Initialize(jsonPath) during app startup.");

        if (string.IsNullOrWhiteSpace(classNumber))
            return null;

        // Try exact match first
        var exact = FindExactMatch(classNumber);
        if (exact != null)
            return exact;

        // Try prefix match
        var prefix = FindPrefixMatch(classNumber);
        if (prefix != null)
            return prefix;

        // Fuzzy matching is intended for textual designations. Applying it to
        // unmatched numbers would turn unrelated repeated digits into a match.
        return ExtractNumeric(classNumber).Length == 0
            ? FindFuzzyMatch(classNumber)
            : null;
    }

    /// <summary>
    /// Gets all available locomotive classes for auto-completion or list displays.
    /// </summary>
    public static IReadOnlyCollection<LocomotiveSeries> GetAllClasses()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TrainClassLibrary not initialized. Call Initialize(jsonPath) during app startup.");

        return _allLocomotives.AsReadOnly();
    }

    /// <summary>
    /// Gets all locomotive classes by type filter (e.g., "Electric loco", "Steam loco", "Railcar").
    /// </summary>
    public static IReadOnlyCollection<LocomotiveSeries> GetByType(string type)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TrainClassLibrary not initialized. Call Initialize(jsonPath) during app startup.");

        return _allLocomotives
            .Where(l => l.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Exact match - looks for the exact number in locomotive name.
    /// Example: "110" matches "E 10 / BR 110" (contains "110")
    /// </summary>
    private static LocomotiveSeries? FindExactMatch(string input)
    {
        var numericPart = ExtractNumeric(input);

        // Compare complete numeric designations so a query for "11" does not
        // accidentally become an exact match for "211" before prefix matching runs.
        return _allLocomotives.FirstOrDefault(l =>
            ExtractAllNumbers(l.Name).Any(number =>
                number.Equals(numericPart, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Prefix match - looks for numbers that start with the input.
    /// Example: "1" matches "BR 110", "BR 111", "BR 120", etc.
    /// </summary>
    private static LocomotiveSeries? FindPrefixMatch(string input)
    {
        var numericPart = ExtractNumeric(input);
        if (string.IsNullOrEmpty(numericPart))
            return null;

        // Look for locomotives where any number in the name starts with input
        return _allLocomotives.FirstOrDefault(l =>
        {
            var locoNumbers = ExtractAllNumbers(l.Name);
            return locoNumbers.Any(num => num.StartsWith(numericPart, StringComparison.OrdinalIgnoreCase));
        });
    }

    /// <summary>
    /// Fuzzy match - partial matching for better UX.
    /// Example: "103" finds "BR 103.1" even if user types imprecisely
    /// </summary>
    private static LocomotiveSeries? FindFuzzyMatch(string input)
    {
        var normalized = NormalizeInput(input);

        // Simple fuzzy: find first locomotive containing most characters in sequence
        var candidates = _allLocomotives
            .Where(l => ContainsMostCharacters(l.Name, normalized))
            .OrderByDescending(l => CalculateMatchScore(l.Name, normalized))
            .FirstOrDefault();

        return candidates;
    }

    /// <summary>
    /// Normalizes user input: removes spaces, converts to uppercase, removes BR/E prefixes.
    /// Example: "br 110" → "110", "E 10" → "10", "BR 110.3" → "110.3"
    /// </summary>
    private static string NormalizeInput(string input)
    {
        return input.Trim().ToUpperInvariant().Replace(" ", "");
    }

    /// <summary>
    /// Extracts all numeric sequences from a string.
    /// Example: "E 10 / BR 110" → ["10", "110"]
    /// </summary>
    private static List<string> ExtractAllNumbers(string text)
    {
        var numbers = new List<string>();
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsDigit(c) || c == '.')
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                numbers.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
            numbers.Add(current.ToString());

        return numbers;
    }

    /// <summary>
    /// Extracts the first numeric part from input.
    /// Example: "110" → "110", "BR110" → "110", "E10" → "10"
    /// </summary>
    private static string ExtractNumeric(string text)
    {
        var result = string.Empty;
        foreach (var c in text)
        {
            if (char.IsDigit(c) || c == '.')
                result += c;
            else if (result.Length > 0)
                break;
        }

        return result;
    }

    /// <summary>
    /// Checks if locomotive name contains most characters from input (fuzzy matching).
    /// </summary>
    private static bool ContainsMostCharacters(string locomotiveName, string input)
    {
        var upperName = locomotiveName.ToUpperInvariant();
        var matched = 0;

        foreach (var c in input)
        {
            if (upperName.Contains(c))
                matched++;
        }

        // Match at least 50% of input characters, rounding up for odd lengths.
        return matched >= ((input.Length + 1) / 2);
    }

    /// <summary>
    /// Scores how well a locomotive name matches the input.
    /// Higher score = better match.
    /// </summary>
    private static int CalculateMatchScore(string locomotiveName, string input)
    {
        var score = 0;
        var upperName = locomotiveName.ToUpperInvariant();

        // Exact substring match is best
        if (upperName.Contains(input))
            score += 100;

        // Match each character in order
        foreach (var c in input)
        {
            if (upperName.Contains(c))
                score += 10;
        }

        // Length similarity (closer to input length = better)
        score -= Math.Abs(locomotiveName.Length - input.Length);

        return score;
    }
}
