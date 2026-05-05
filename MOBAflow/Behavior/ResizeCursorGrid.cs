// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Behavior;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

public sealed class ResizeCursorGrid : Grid
{
    private static readonly InputCursor ResizeCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

    public void ShowDefaultCursor()
    {
        ProtectedCursor = null;
    }

    public void ShowResizeCursor()
    {
        ProtectedCursor = ResizeCursor;
    }
}