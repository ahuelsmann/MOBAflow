// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.TrackPlan;

/// <summary>Bounded, framework-independent snapshot history for editor state.</summary>
public sealed class UndoRedoService<T>
{
    private readonly Stack<T> _undo = [];
    private readonly Stack<T> _redo = [];

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Record(T previous)
    {
        _undo.Push(previous);
        _redo.Clear();
    }

    public bool TryUndo(T current, out T previous)
    {
        if (!_undo.TryPop(out previous!))
            return false;
        _redo.Push(current);
        return true;
    }

    public bool TryRedo(T current, out T next)
    {
        if (!_redo.TryPop(out next!))
            return false;
        _undo.Push(current);
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
