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
    /// Undo/Redo-Befehl zum Ändern der Schrift eines Controls.
    /// Kompatibel mit .NET Framework 4.0 / C# 4.0.
    /// </summary>
    public class FontCommand : IEditorCommand
    {
        private readonly Control _ctrl;
        private readonly Font _oldFont;
        private readonly Font _newFont;

        public FontCommand(Control ctrl, Font newFont)
        {
            if (ctrl == null)
                throw new ArgumentNullException("ctrl");     // kein nameof in C# 4
            if (newFont == null)
                throw new ArgumentNullException("newFont");

            _ctrl = ctrl;
            _oldFont = ctrl.Font;
            _newFont = newFont;
        }

        public void Execute()
        {
            _ctrl.Font = _newFont;
        }

        public void Undo()
        {
            _ctrl.Font = _oldFont;
        }
    }
}
