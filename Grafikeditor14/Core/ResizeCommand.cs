using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14.Core
{
    public class ResizeCommand : IEditorCommand
    {
        private readonly Control _ctrl;
        private readonly Size _alt;
        private readonly Size _neu;

        public ResizeCommand(Control ctrl, Size neu)
        {
            _ctrl = ctrl;
            _alt = ctrl.Size;
            _neu = neu;
        }

        public void Execute() { _ctrl.Size = _neu; }
        public void Undo() { _ctrl.Size = _alt; }
    }
}
