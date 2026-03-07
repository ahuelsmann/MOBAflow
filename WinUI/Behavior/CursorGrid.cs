namespace Moba.WinUI.Behavior;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

public sealed class CursorGrid : Grid
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
