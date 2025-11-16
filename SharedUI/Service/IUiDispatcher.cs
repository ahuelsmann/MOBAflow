namespace Moba.SharedUI.Service;

public interface IUiDispatcher
{
    void InvokeOnUi(Action action);
}
