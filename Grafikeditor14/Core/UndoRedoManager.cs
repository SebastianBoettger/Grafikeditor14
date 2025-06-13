using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grafikeditor14.Core
{
    /// <summary>
    /// Verwaltet zwei Stacks für Undo und Redo.
    /// </summary>
    public class UndoRedoManager
    {
        private readonly Stack<IEditorCommand> _undo = new Stack<IEditorCommand>();
        private readonly Stack<IEditorCommand> _redo = new Stack<IEditorCommand>();

        public void Do(IEditorCommand cmd)
        {
            cmd.Execute();
            _undo.Push(cmd);
            _redo.Clear();
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;

            IEditorCommand cmd = _undo.Pop();
            cmd.Undo();
            _redo.Push(cmd);
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;

            IEditorCommand cmd = _redo.Pop();
            cmd.Execute();
            _undo.Push(cmd);
        }
    }
}
