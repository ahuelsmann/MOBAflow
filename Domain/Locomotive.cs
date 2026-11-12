// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

using System.Text.Json.Serialization;

/// <summary>
/// Upcoming feature: Represents a locomotive.
/// </summary>
public class Locomotive
{
    /// <summary>The highest decoder function index (F31); a locomotive has the functions F0 through F31.</summary>
    public const int MaxFunctionIndex = 31;

    /// <summary>
    /// Stored symbol or colour entry that marks a function whose appearance was cleared on purpose, as opposed to an
    /// empty entry that falls back to the default.
    /// </summary>
    public const string ClearedFunctionAppearance = "none";

    /// <summary>
    /// Initializes a new instance of the <see cref="Locomotive"/> class with a new identifier and default name.
    /// </summary>
    public Locomotive()
    {
        Id = Guid.NewGuid();
        Name = "New Locomotive";
    }

    /// <summary>
    /// Gets or sets the unique identifier of the locomotive.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the locomotive.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The position is relevant for double traction and describes the position of the locomotive within a train.
    /// </summary>
    public uint Pos { get; set; } = 1;

    /// <summary>
    /// Gets or sets the unique primary digital address of the locomotive (DCC address).
    /// Multiple traction should coordinate locomotives as a train while retaining separate primary addresses.
    /// </summary>
    public uint? DigitalAddress { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer name of the locomotive model.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the article number of the locomotive model.
    /// </summary>
    public string? ArticleNumber { get; set; }

    /// <summary>
    /// Resolved reference to the locomotive series from the library (Vmax, Type, Epoch, etc.).
    /// Set at runtime when the series name can be matched; not persisted.
    /// </summary>
    [JsonIgnore]
    public LocomotiveSeries? LocomotiveSeriesRef { get; set; }

    /// <summary>
    /// Custom PNG asset filenames for function buttons F0–F31 on the Train Control page.
    /// Each entry is a filename relative to MOBAflow/Assets/FunctionSymbols (e.g. "headlight.png").
    /// Index 0 = F0, 1 = F1, … 31 = F31. Empty or null entries fall back to the default
    /// asset for that function (only F0/F1 have defaults). Legacy Unicode-codepoint
    /// values from earlier versions are ignored on read.
    /// </summary>
    public List<string>? FunctionSymbols { get; set; }

    /// <summary>
    /// Custom button colors for function buttons F0-F31 on the Train Control page.
    /// Each entry is a hex color string (e.g. "#FFD700"). Empty or null entries fall back to the default color.
    /// </summary>
    public List<string>? FunctionColors { get; set; }

    /// <summary>
    /// Manufacturer handbook labels for function buttons F0–F31 (e.g. decoder function mapping from ESU manual).
    /// Index 0 = F0, 1 = F1, … 31 = F31. Empty or null entries have no stored label.
    /// </summary>
    public List<string>? FunctionLabels { get; set; }

    /// <summary>
    /// Gets or sets the primary color scheme of the locomotive.
    /// </summary>
    public ColorScheme? ColorPrimary { get; set; }

    /// <summary>
    /// Gets or sets the secondary color scheme of the locomotive.
    /// </summary>
    public ColorScheme? ColorSecondary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the locomotive is pushing at the end of the train.
    /// </summary>
    public bool IsPushing { get; set; }

    /// <summary>
    /// Gets or sets additional details of the locomotive.
    /// </summary>
    public Details? Details { get; set; }

    /// <summary>
    /// Invoice date for purchase tracking (optional).
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// Delivery date for purchase tracking (optional).
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Relative path to locomotive photo (stored in project photos folder).
    /// Example: "locomotives/{id}.jpg"
    /// </summary>
    public string? PhotoPath { get; set; }

    /// <summary>
    /// Optional installed decoder profile and validated CV backup snapshots.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LocomotiveDecoderProfile? Decoder { get; set; }

    /// <summary>Whether <paramref name="functionIndex"/> names one of the functions F0 through F31.</summary>
    public static bool IsFunctionIndex(int functionIndex) => functionIndex is >= 0 and <= MaxFunctionIndex;

    /// <summary>Whether <paramref name="value"/> has the stored colour form <c>#RRGGBB</c>.</summary>
    public static bool IsStoredFunctionColor(string? value)
    {
        if (value is null || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var position = 1; position < value.Length; position++)
        {
            if (!char.IsAsciiHexDigit(value[position]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Stores the symbol of a function; false for an index outside F0-F31 or an empty symbol.
    /// <see cref="FunctionSymbols"/> grows with empty entries up to the index.
    /// </summary>
    public bool SetFunctionSymbol(int functionIndex, string symbol)
    {
        if (!IsFunctionIndex(functionIndex) || string.IsNullOrEmpty(symbol))
        {
            return false;
        }

        FunctionSymbols = SetAt(FunctionSymbols, functionIndex, symbol);
        return true;
    }

    /// <summary>
    /// Stores the symbol (when given) and the colour (when in the stored form <c>#RRGGBB</c>) of a function;
    /// false for an index outside F0-F31. An invalid colour leaves the stored colours unchanged.
    /// </summary>
    public bool SetFunctionAppearance(int functionIndex, string? symbol, string? colorHex)
    {
        if (!IsFunctionIndex(functionIndex))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(symbol))
        {
            FunctionSymbols = SetAt(FunctionSymbols, functionIndex, symbol);
        }

        if (IsStoredFunctionColor(colorHex))
        {
            FunctionColors = SetAt(FunctionColors, functionIndex, colorHex!);
        }

        return true;
    }

    /// <summary>
    /// Marks a function as having no symbol and no colour with <see cref="ClearedFunctionAppearance"/>;
    /// false for an index outside F0-F31.
    /// </summary>
    public bool ClearFunctionAppearance(int functionIndex)
    {
        if (!IsFunctionIndex(functionIndex))
        {
            return false;
        }

        FunctionSymbols = SetAt(FunctionSymbols, functionIndex, ClearedFunctionAppearance);
        FunctionColors = SetAt(FunctionColors, functionIndex, ClearedFunctionAppearance);
        return true;
    }

    private static List<string> SetAt(List<string>? values, int index, string value)
    {
        values ??= [];
        while (values.Count <= index)
        {
            values.Add(string.Empty);
        }

        values[index] = value;
        return values;
    }
}
