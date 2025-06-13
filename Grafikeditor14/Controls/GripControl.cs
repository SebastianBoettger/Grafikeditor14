using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Grafikeditor14.Controls
{
    public class GripControl : Control
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTBOTTOMRIGHT = 17;

        public GripControl()
        {
            this.Size = new Size(16, 16); // Größe wie bei nativen Grips
            this.Cursor = Cursors.SizeNWSE;
            this.BackColor = SystemColors.Control;
            this.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Sichtbarkeit im Designer unterdrücken
            this.Visible = !this.DesignMode;

            // ToolTip anzeigen
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this, "Hier klicken und ziehen, um das Fenster zu vergrößern.");
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.FindForm().Handle, WM_NCLBUTTONDOWN, (IntPtr)HTBOTTOMRIGHT, IntPtr.Zero);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawSizeGrip(e.Graphics, SystemColors.ControlDark, this.ClientRectangle);
        }
    }
}