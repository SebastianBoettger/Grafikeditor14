using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grafikeditor14.Core;
using System.Windows.Forms;
using System.Drawing;

namespace Grafikeditor14.Command
{
    /// <summary>
    /// Undo/Redo-Befehl zum Ändern des Textes eines beliebigen Controls.
    /// Kompatibel mit .NET Framework 4.0 / C# 4.0.
    /// </summary>
    public class TextCommand : IEditorCommand
    {
        private readonly Control _ctrl;
        private readonly string _oldText;
        private readonly string _newText;

        public TextCommand(Control ctrl, string newText)
        {
            if (ctrl == null)
                throw new ArgumentNullException("ctrl");   // kein nameof in C# 4

            _ctrl = ctrl;
            _oldText = ctrl.Text;
            _newText = newText ?? string.Empty;
        }

        public void Execute()
        {
            _ctrl.Text = _newText;
        }

        public void Undo()
        {
            _ctrl.Text = _oldText;
        }
    }
}
