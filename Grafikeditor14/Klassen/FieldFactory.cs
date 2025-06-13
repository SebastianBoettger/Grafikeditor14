using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Grafikeditor14.Controls;

namespace Grafikeditor14.Klassen
{
    public static class FieldFactory
    {
        public static Label CreateLabel(
            PendingFieldProps pending,
            Panel panel,
            string labelText,
            string fieldName,
            string auftragsMerkmal,
            int? posX = null,
            int? posY = null,
            int? width = null,
            int? height = null,
            int padding = 20)
        {
            var lbl = new Label
            {
                TextAlign = pending.Alignment,
                Font = (Font)pending.Font.Clone(),
                Text = String.IsNullOrWhiteSpace(labelText) ? "" : labelText,
                Height = height ?? 25,
                ForeColor = pending.ForeColor,
                BackColor = pending.BackColor,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Name = fieldName
            };

            /* Größe */
            Size sz = TextRenderer.MeasureText(lbl.Text, lbl.Font);
            lbl.Width = width ?? Math.Max(sz.Width + padding, 110);

            /* Position (zentriert, wenn nicht übergeben) */
            Point scr = panel.AutoScrollPosition;
            int x = posX ?? scr.X + (panel.ClientSize.Width - lbl.Width) / 2;
            int y = posY ?? scr.Y + (panel.ClientSize.Height - lbl.Height) / 2;
            lbl.Location = new Point(x, y);

            /* Tag */
            lbl.Tag = BuildTagArray(lbl, auftragsMerkmal);

            panel.Controls.Add(lbl);
            return lbl;
        }

        // ───────────────────────────────────────────────────────────────
        // 2) Kurze Methode – erzeugt Label IMMER mit festen Standardwerten
        // ───────────────────────────────────────────────────────────────
        public static Label CreateLabel(
            Panel panel,
            string labelText,
            string fieldName,
            string auftragsMerkmal)
        {
            var standard = new PendingFieldProps
            {
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                Font = new Font("Arial", 12f, FontStyle.Bold),
                Alignment = ContentAlignment.MiddleCenter
            };

            // ► voll qualifiziert, damit NICHT diese Methode erneut gewählt wird
            return Grafikeditor14.Klassen.FieldFactory.CreateLabel(
                       standard,
                       panel,
                       labelText,
                       fieldName,
                       auftragsMerkmal);
        }

        // ───────────────────────────────────────────────────────────────
        // 3) Tag-Array
        // ───────────────────────────────────────────────────────────────
        private static string[] BuildTagArray(Control ctrl, string auftragsMerkmal)
        {
            Label l = ctrl as Label;
            int al = 32;
            if (l != null)
            {
                if (l.TextAlign == ContentAlignment.MiddleLeft) al = 16;
                if (l.TextAlign == ContentAlignment.MiddleRight) al = 48;
            }

            int txtColor = (l != null) ? l.ForeColor.ToArgb() : 0;
            int fSize = (l != null) ? (int)l.Font.Size : 0;

            return new[]
            {
                "Alignment="  + al,
                "PosY="       + ctrl.Top,
                "Fontgröße="  + fSize,
                "Text="       + (l != null ? l.Text : ""),
                "Füllzeichen=",
                "Höhe="       + ctrl.Height,
                "Textfarbe="  + txtColor,
                "Stellen=0",
                "PosX="       + ctrl.Left,
                "FeldName="   + ctrl.Name,
                "FontName="   + (l != null ? l.Font.Name : ""),
                "Breite="     + ctrl.Width,
                "Fontstyle="  + (l != null ? (int)l.Font.Style : 0),
                "Farbe="      + ctrl.BackColor.ToArgb(),
                "DSFeldname=" + auftragsMerkmal
            };
        }

        public static Label CreateDefaultLabel(Panel canvas, string fieldName)
        {
            var lbl = new Label
            {
                /* Standard-Layout */
                Text = string.Empty,
                Font = new Font("Arial", 12f, FontStyle.Bold),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 110,
                Height = 25,
                Name = fieldName
            };

            /* mittig im sichtbaren Bereich platzieren */
            Point scr = canvas.AutoScrollPosition;
            int x = scr.X + (canvas.ClientSize.Width - lbl.Width) / 2;
            int y = scr.Y + (canvas.ClientSize.Height - lbl.Height) / 2;
            lbl.Location = new Point(x, y);

            /* minimaler Tag-Block, sonst greifen spätere Routinen nicht */
            lbl.Tag = new[]
            {
                "Alignment=32",           // Mitte
                "PosY="   + y,
                "Fontgröße=12",
                "Text=",
                "Füllzeichen=",
                "Höhe="   + lbl.Height,
                "Textfarbe=" + lbl.ForeColor.ToArgb(),
                "Stellen=0",
                "PosX="   + x,
                "FeldName=" + fieldName,
                "FontName=Arial",
                "Breite=" + lbl.Width,
                "Fontstyle=" + (int)FontStyle.Bold,
                "Farbe="  + lbl.BackColor.ToArgb(),
                "DSFeldname="
            };

            canvas.Controls.Add(lbl);
            return lbl;
        }
    }
}
