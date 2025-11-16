namespace Moba.WinUI.Service;

using Microsoft.UI.Dispatching;
using Moba.SharedUI.Service;

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
