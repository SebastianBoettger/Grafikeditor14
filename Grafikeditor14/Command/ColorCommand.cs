using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grafikeditor14.Core;
using System.Windows.Forms;
using System.Drawing;

namespace Grafikeditor14.Command
{
    public class ColorCommand : IEditorCommand
    {
        private readonly Control _ctrl;
        private readonly Color _oldColor;
        private readonly Color _newColor;
        private readonly bool _isBack;      // true=BackColor, false=ForeColor

        public ColorCommand(Control ctrl, Color newColor, bool isBack)
        {
            _ctrl = ctrl;
            _isBack = isBack;
            _oldColor = isBack ? ctrl.BackColor : ((Label)ctrl).ForeColor;
            _newColor = newColor;
        }

        public void Execute()
        {
            if (_isBack) _ctrl.BackColor = _newColor;
            else ((Label)_ctrl).ForeColor = _newColor;
        }

        public void Undo()
        {
            if (_isBack) _ctrl.BackColor = _oldColor;
            else ((Label)_ctrl).ForeColor = _oldColor;
        }
    }
}
