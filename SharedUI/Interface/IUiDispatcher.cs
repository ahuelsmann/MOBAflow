// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Stellt Ausführung von Aktionen auf dem UI-Thread sicher (Thread-Marshalling).
/// Wird von ViewModels genutzt, wenn z. B. Events oder Hintergrunddienste vom Nicht-UI-Thread
/// kommen und Properties/Collections aktualisiert werden müssen.
/// </summary>
/// <remarks>
/// Best Practice: Collection-Updates während PropertyChanged-Ketten (z. B. bei Projektwechsel)
/// durch Ersetzen der Collection lösen (neue ObservableCollection zuweisen), nicht durch
/// Clear/Add in place – vermeidet Reentranz und COMException in WinUI-Bindings.
/// </remarks>
public interface IUiDispatcher
{
    /// <summary>
    /// Führt die Aktion auf dem UI-Thread aus. Ist der Aufruf bereits auf dem UI-Thread,
    /// wird synchron ausgeführt; sonst wird auf den UI-Thread gewechselt.
    /// </summary>
    void InvokeOnUi(Action action);

    /// <summary>
    /// Führt eine asynchrone Aktion auf dem UI-Thread aus und wartet auf deren Abschluss.
    /// </summary>
    Task InvokeOnUiAsync(Func<Task> asyncAction);

    /// <summary>
    /// Führt eine asynchrone Funktion auf dem UI-Thread aus und gibt das Ergebnis zurück.
    /// </summary>
    Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc);
}