// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interop;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// Cross-platform helpers for WinUI <c>Grid</c> / <c>ColumnDefinition</c> without referencing WinUI assemblies from SharedUI.
/// Uses reflection on the live instances passed from the host (type identity matches the XAML runtime).
/// </summary>
internal static class WinUiGridInterop
{
    /// <summary>
    /// Tries to read <c>Grid.ColumnDefinitions</c> as a non-generic list.
    /// </summary>
    public static bool TryGetColumnDefinitions(object grid, [NotNullWhen(true)] out IList? columns)
    {
        columns = null;
        var property = grid.GetType().GetProperty("ColumnDefinitions", BindingFlags.Public | BindingFlags.Instance);
        if (property?.GetValue(grid) is not IList list)
            return false;
        columns = list;
        return true;
    }
}
