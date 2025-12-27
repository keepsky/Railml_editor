using System;
using System.Collections.Generic;
using RailmlEditor.ViewModels;

namespace RailmlEditor
{
    public interface IUndoableAction
    {
        void Undo();
        void Redo();
    }

    public class StateSnapshotAction : IUndoableAction
    {
        private readonly List<BaseElementViewModel> _oldState;
        private readonly List<BaseElementViewModel> _newState;
        private readonly Action<List<BaseElementViewModel>> _restoreAction;

        public StateSnapshotAction(List<BaseElementViewModel> oldState, List<BaseElementViewModel> newState, Action<List<BaseElementViewModel>> restoreAction)
        {
            _oldState = oldState;
            _newState = newState;
            _restoreAction = restoreAction;
        }

        public void Undo() => _restoreAction(_oldState);
        public void Redo() => _restoreAction(_newState);
    }

    public class UndoRedoManager
    {
        private readonly Stack<IUndoableAction> _undoStack = new();
        private readonly Stack<IUndoableAction> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event EventHandler StateChanged;

        public void Execute(IUndoableAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
