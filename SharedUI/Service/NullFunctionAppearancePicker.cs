// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

/// <summary>
/// No-op picker used by MAUI and headless tests where function symbol editing is unavailable.
/// </summary>
public sealed class NullFunctionAppearancePicker : IFunctionAppearancePicker
{
    /// <inheritdoc />
    public Task<FunctionAppearancePickerResult?> PickAsync(
        FunctionAppearancePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = request;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<FunctionAppearancePickerResult?>(null);
    }
}
