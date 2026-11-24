// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Navigation;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Moba.SharedUI.Configuration;
using Moba.Test.MOBAdemo.Build;

/// <summary>One navigation entry as the WinUI app registers it, or as <c>navigation.json</c> lists it.</summary>
internal sealed record NavigationEntryData(
    string Key,
    string Title,
    string? Icon,
    string Category,
    int Order,
    bool IsBold,
    string? PathIconData,
    string? FeatureToggleKey,
    string? Badge);

/// <summary>
/// Reads the navigation of the WinUI app without the Windows build: the page registration is parsed as text,
/// badges come from the portable <see cref="FeatureToggleRegistry"/> with the labels of the shipped appsettings.
/// The guards catch accidental drift of ordinary registrations, not deliberately unusual code
/// (a duplicate <c>Add</c>, <c>AddRange</c>, <c>RemoveAll</c> or <c>Where</c> over the list).
/// </summary>
internal static partial class NavigationSource
{
    [GeneratedRegex(@"^\s*#", RegexOptions.Multiline)]
    private static partial Regex PreprocessorDirective();

    [GeneratedRegex(@"\bpages\s*\.\s*Add\s*\(")]
    private static partial Regex AddCall();

    [GeneratedRegex(@"\bnew\s+PageMetadata\s*\(")]
    private static partial Regex Construction();

    [GeneratedRegex(@"(?<!record\s)(?<!<)\bPageMetadata\b(?!>)")]
    private static partial Regex OtherUse();

    [GeneratedRegex(@"^\s*(\w+)\s*=\s*(\d+)\s*,?\s*$", RegexOptions.Multiline)]
    private static partial Regex CategoryValue();

    [GeneratedRegex(@"^(\w+)\s*:\s*(.+)$", RegexOptions.Singleline)]
    private static partial Regex NamedArgument();

    [GeneratedRegex(@"\\(?!u[0-9A-Fa-f]{4})")]
    private static partial Regex UnsupportedEscape();

    [GeneratedRegex(@"\\u([0-9A-Fa-f]{4})")]
    private static partial Regex UnicodeEscape();

    [GeneratedRegex(@"^FeatureTogglePages\.(\w+)$")]
    private static partial Regex ToggleReference();

    private static readonly string[] Fields =
        ["Tag", "Title", "Icon", "PageType", "Category", "Order", "FeatureToggleKey", "PathIconData", "IsBold"];

    private static string ServiceFile(string name) => Path.Combine(BuildScript.RepositoryRoot, "MOBAflow", "Service", name);

    internal static string RegistrationText => File.ReadAllText(ServiceFile("NavigationRegistration.cs"));

    internal static string CategoryText => File.ReadAllText(ServiceFile("NavigationCategory.cs"));

    internal static string AppSettingsText =>
        File.ReadAllText(Path.Combine(BuildScript.RepositoryRoot, "MOBAflow", "appsettings.json"));

    internal static string NavigationJsonPath =>
        Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "app", "src", "examples", "navigation.json");

    /// <summary>All entries of the registration in navigation order (category, then order).</summary>
    internal static IReadOnlyList<NavigationEntryData> FromApp(string registration, string categories, string appSettings)
    {
        registration = StripComments(registration);
        if (PreprocessorDirective().IsMatch(registration))
        {
            throw new InvalidOperationException("Registration shape not understood: preprocessor directives are not supported.");
        }

        var categoryValues = ParseCategories(categories);
        var settings = ParseSettings(appSettings);
        var entries = ParseRegistration(registration, categoryValues, settings);

        // Guard against registrations the parser cannot see: every Add call and every use of the record
        // must be one of the parsed entries.
        var addCalls = AddCall().Count(registration);
        var constructions = Construction().Count(registration);
        var otherUses = OtherUse().Count(registration);
        if (addCalls != entries.Count || constructions != entries.Count || otherUses != entries.Count)
        {
            throw new InvalidOperationException(
                $"Registration shape not understood: parsed {entries.Count} entries, but found {addCalls} 'pages.Add(' calls, " +
                $"{constructions} 'new PageMetadata(' constructions and {otherUses} other 'PageMetadata' uses.");
        }

        // Pages the shipped appsettings switch off are hidden, as MainWindowNavigationBootstrapper.IsPageVisible does.
        return [.. entries
            .Where(e => e.FeatureToggleKey is null
                || FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, e.FeatureToggleKey))
            .OrderBy(e => categoryValues[e.Category])
            .ThenBy(e => e.Order)];
    }

    /// <summary>Entries of <c>navigation.json</c> in file order, with the demo flag read separately.</summary>
    internal static (IReadOnlyList<NavigationEntryData> Entries, IReadOnlyList<string> DemoKeys) FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var entries = new List<NavigationEntryData>();
        var demo = new List<string>();
        foreach (var item in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            var key = item.GetProperty("key").GetString()!;
            entries.Add(new NavigationEntryData(
                key,
                item.GetProperty("title").GetString()!,
                OptionalString(item, "icon"),
                item.GetProperty("category").GetString()!,
                item.GetProperty("order").GetInt32(),
                item.GetProperty("isBold").GetBoolean(),
                OptionalString(item, "pathIconData"),
                OptionalString(item, "featureToggleKey"),
                OptionalString(item, "badge")));
            if (item.GetProperty("demoPage").GetBoolean())
            {
                demo.Add(key);
            }
        }

        return (entries, demo);
    }

    /// <summary>A string or an explicit null; a missing property or any other kind is a drift, not a null.</summary>
    private static string? OptionalString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value))
        {
            throw new InvalidOperationException($"navigation.json entry lacks '{name}'.");
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => throw new InvalidOperationException($"navigation.json '{name}' must be a string or null."),
        };
    }

    /// <summary>Removes line and block comments outside string literals, so a commented-out registration does not count.</summary>
    internal static string StripComments(string text)
    {
        var result = new StringBuilder(text.Length);
        var inString = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                result.Append(c);
                if (c == '\\' && i + 1 < text.Length)
                {
                    result.Append(text[++i]);
                }
                else if (c == '"')
                {
                    inString = false;
                }
            }
            else if (c == '"')
            {
                inString = true;
                result.Append(c);
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n')
                {
                    i++;
                }

                result.Append('\n');
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 1;
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private static Dictionary<string, int> ParseCategories(string text)
    {
        var values = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in CategoryValue().Matches(text))
        {
            values[match.Groups[1].Value] = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        }

        return values;
    }

    private static FeatureToggleSettings ParseSettings(string appSettings)
    {
        var settings = new FeatureToggleSettings();
        using var document = JsonDocument.Parse(appSettings);
        foreach (var page in document.RootElement.GetProperty("FeatureToggles").GetProperty("Pages").EnumerateObject())
        {
            settings.Pages[page.Name] = new FeatureTogglePageSettings
            {
                IsAvailable = page.Value.GetProperty("IsAvailable").GetBoolean(),
                Label = page.Value.TryGetProperty("Label", out var label) ? label.GetString() : null,
            };
        }

        return settings;
    }

    private static List<NavigationEntryData> ParseRegistration(
        string text, Dictionary<string, int> categoryValues, FeatureToggleSettings settings)
    {
        var entries = new List<NavigationEntryData>();
        foreach (Match start in Construction().Matches(text))
        {
            var arguments = SplitArguments(text, start.Index + start.Length);
            var values = BindArguments(arguments);
            var category = values["Category"].Split('.')[^1];
            if (!categoryValues.ContainsKey(category))
            {
                throw new InvalidOperationException($"Unknown navigation category '{category}'.");
            }

            var toggle = Reference(values["FeatureToggleKey"]);
            entries.Add(new NavigationEntryData(
                Literal(values["Tag"])!,
                Literal(values["Title"])!,
                Literal(values["Icon"]),
                category,
                int.Parse(values["Order"], CultureInfo.InvariantCulture),
                bool.Parse(values["IsBold"]),
                Literal(values["PathIconData"]),
                toggle,
                FeatureToggleRegistry.GetBadgeLabel(settings, toggle)));
        }

        return entries;
    }

    private static Dictionary<string, string> BindArguments(List<string> arguments)
    {
        if (arguments.Count != Fields.Length)
        {
            throw new InvalidOperationException($"PageMetadata call has {arguments.Count} arguments, expected {Fields.Length}.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < arguments.Count; index++)
        {
            var named = NamedArgument().Match(arguments[index]);
            var name = named.Success ? named.Groups[1].Value : Fields[index];
            values[name] = (named.Success ? named.Groups[2].Value : arguments[index]).Trim();
        }

        foreach (var field in Fields)
        {
            if (!values.ContainsKey(field))
            {
                throw new InvalidOperationException($"PageMetadata call lacks the argument '{field}'.");
            }
        }

        return values;
    }

    /// <summary>Splits the arguments of a call at top-level commas, up to its closing parenthesis.</summary>
    private static List<string> SplitArguments(string text, int index)
    {
        var arguments = new List<string>();
        var current = new StringBuilder();
        var depth = 0;
        var inString = false;
        for (; index < text.Length; index++)
        {
            var c = text[index];
            if (inString)
            {
                current.Append(c);
                if (c == '\\')
                {
                    current.Append(text[++index]);
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    current.Append(c);
                    break;
                case '(':
                    depth++;
                    current.Append(c);
                    break;
                case ')' when depth == 0:
                    arguments.Add(current.ToString().Trim());
                    return arguments;
                case ')':
                    depth--;
                    current.Append(c);
                    break;
                case ',' when depth == 0:
                    arguments.Add(current.ToString().Trim());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        throw new InvalidOperationException("Unterminated PageMetadata call.");
    }

    private static string? Literal(string value)
    {
        if (value == "null")
        {
            return null;
        }

        if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
        {
            throw new InvalidOperationException($"Not a string literal: {value}");
        }

        var body = value[1..^1];

        // Only \uXXXX is decoded; any other escape (such as \t) would compare differently in JSON, so it fails loudly.
        if (UnsupportedEscape().IsMatch(body))
        {
            throw new InvalidOperationException($"Unsupported escape sequence in {value}");
        }

        return UnicodeEscape().Replace(
            body,
            m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());
    }

    /// <summary>The page key behind <c>FeatureTogglePages.Name</c> (the constants are <c>nameof</c> of themselves).</summary>
    private static string? Reference(string value)
    {
        if (value == "null")
        {
            return null;
        }

        var match = ToggleReference().Match(value);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Not a FeatureTogglePages reference: {value}");
        }

        // The registry and the settings use the constant's value, which is not necessarily its name.
        var field = typeof(FeatureTogglePages).GetField(match.Groups[1].Value)
            ?? throw new InvalidOperationException($"Unknown FeatureTogglePages constant: {value}");
        return (string)field.GetRawConstantValue()!;
    }
}
