namespace Moba.MAUI.Service;

using Moba.SharedUI.Service;

public class UiDispatcher : IUiDispatcher
{
    public void InvokeOnUi(Action action)
    {
#if ANDROID || IOS || MACCATALYST
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action);
#else
        action();
#endif
    }
}
