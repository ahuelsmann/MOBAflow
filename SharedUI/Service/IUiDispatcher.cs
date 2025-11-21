// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

public interface IUiDispatcher
{
    void InvokeOnUi(Action action);
}
