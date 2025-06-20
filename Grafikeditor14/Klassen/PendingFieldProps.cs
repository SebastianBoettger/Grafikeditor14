﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14.Klassen
{
    /// Temporärer Container für bearbeitete Feldeigenschaften
    public class PendingFieldProps
    {
        public string       AuftragMerkmal;
        public string       Text;
        public Color        BackColor;
        public Color        ForeColor;
        public Font         Font;
        public ContentAlignment Alignment;

        public PendingFieldProps()
        {
            AuftragMerkmal  = "";
            Text            = "";
            BackColor       = SystemColors.Control;
            ForeColor       = Color.Black;
            Font            = new Font("Arial", 12f, FontStyle.Bold);
            Alignment       = ContentAlignment.MiddleCenter;   // <- gültiger Enum-Wert
        }

        public void CopyFrom(Control src)
        {
            Label lbl = src as Label;
            Panel pnl = src as Panel;

            Text        = (lbl != null) ? lbl.Text          : "";
            BackColor   = src.BackColor;
            ForeColor   = (lbl != null) ? lbl.ForeColor     : Color.Black;
            Font        = (lbl != null) ? lbl.Font          : SystemFonts.DefaultFont;
            Alignment   = (lbl != null) ? lbl.TextAlign     : ContentAlignment.MiddleCenter;
            AuftragMerkmal = "";   // wird separat aus Tag-Array gezogen
        }
    }
}
