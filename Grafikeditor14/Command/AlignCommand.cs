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
    /// Befehl zum Ändern der Textausrichtung eines Labels (inkl. Undo/Redo).
    /// Kompatibel mit .NET Framework 4.0 / C# 4.0.
    /// </summary>
    public class AlignCommand : IEditorCommand
    {
        private readonly Label _label;
        private readonly ContentAlignment _oldAlignment;
        private readonly ContentAlignment _newAlignment;

        public AlignCommand(Label label, ContentAlignment newAlignment)
        {
            if (label == null)
                throw new ArgumentNullException("label");   // kein nameof in C# 4

            _label = label;
            _oldAlignment = label.TextAlign;
            _newAlignment = newAlignment;
        }

        public void Execute()
        {
            _label.TextAlign = _newAlignment;
        }

        public void Undo()
        {
            _label.TextAlign = _oldAlignment;
        }
    }
}
