// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Dispatching;
using SharedUI.Interface;

public class UiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue? _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public void InvokeOnUi(Action action)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
            action();
        else if (_dispatcherQueue != null)
            _dispatcherQueue.TryEnqueue(() => action());
        else
            action();
    }
}
