// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Request payload for picking a locomotive function button symbol and backlight color.
/// </summary>
/// <param name="InitialColorHex">Current backlight color hex (e.g. "#FFD700").</param>
public sealed record FunctionAppearancePickerRequest(string InitialColorHex);

/// <summary>
/// Result of a function appearance picker dialog.
/// </summary>
/// <param name="IsConfirmed">True when the user confirmed the dialog.</param>
/// <param name="IsSelectionCleared">True when the user explicitly cleared glyph and color.</param>
/// <param name="Glyph">Selected PNG asset filename, or null.</param>
/// <param name="ColorHex">Selected backlight color hex, or null.</param>
public sealed record FunctionAppearancePickerResult(
    bool IsConfirmed,
    bool IsSelectionCleared,
    string? Glyph,
    string? ColorHex);

/// <summary>
/// Platform service for editing locomotive function button appearance (symbol and color).
/// WinUI shows <c>FunctionSymbolPickerWindow</c>; MAUI/tests use a null implementation.
/// </summary>
public interface IFunctionAppearancePicker
{
    /// <summary>
    /// Shows the function appearance picker and returns the user's choice, or null when cancelled/unavailable.
    /// </summary>
    Task<FunctionAppearancePickerResult?> PickAsync(
        FunctionAppearancePickerRequest request,
        CancellationToken cancellationToken = default);
}
