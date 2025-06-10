using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14.Core
{
    public class MoveCommand : IEditorCommand
    {
        private readonly Control _ctrl;
        private readonly Point _oldPos;
        private readonly Point _newPos;

        public MoveCommand(Control ctrl, Point newPos)
        {
            _ctrl = ctrl;
            _oldPos = ctrl.Location;
            _newPos = newPos;
        }

        public void Execute()
        {
            _ctrl.Location = _newPos;
        }

        public void Undo()
        {
            _ctrl.Location = _oldPos;
        }
    }
}
