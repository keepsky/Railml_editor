using System;
using System.Collections.Generic;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Utils
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

    /// <summary>
    /// 문서에서 일어난 작업 내역을 기억해두었다가, Ctrl+Z(되돌리기)나 Ctrl+Y(다시 실행)를 눌렀을 때 
    /// 이전 상태 또는 다음 상태로 화면을 복구해주는 핵심 관리자입니다.
    /// 메모장이나 포토샵의 '작업 내역(History)' 창과 같은 역할을 합니다.
    /// </summary>
    public class UndoRedoManager
    {
        private readonly Stack<IUndoableAction> _undoStack = new();
        private readonly Stack<IUndoableAction> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event EventHandler? StateChanged;

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



