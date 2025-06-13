using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14.Controls
{
    /// <summary>
    /// Transparentes Panel, das nur einen Rahmen zeichnet und
    /// niemals seinen Hintergrund füllt (keine schwarzen Blitzer).
    /// </summary>
    internal sealed class BorderPanel : Panel
    {
        public BorderPanel()
        {
            SetStyle(ControlStyles.Opaque, true);   // verhindert Background-Fill
            BackColor = Color.Transparent;
            Enabled = false;                     // nicht klickbar
        }

        // Hintergrund NICHT malen
        protected override void OnPaintBackground(PaintEventArgs e) { /* nix */ }

        // Rahmen 2 px rot
        protected override void OnPaint(PaintEventArgs e)
        {
            using (var p = new Pen(Color.Red, 2))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        // WS_EX_TRANSPARENT → echte Transparenz
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x20;          // WS_EX_TRANSPARENT
                return cp;
            }
        }
    }
}
