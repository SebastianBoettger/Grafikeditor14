// ============================================================
// Form1.Layout.cs – Layoutlogik und automatische Größenanpassung
// Ziel: Panel- und Fenstergrößen dynamisch anpassen, Layout zentrieren
// .NET 4.0 kompatibel, kommentiert für neue Entwickler
// ============================================================

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        private readonly int minFormHeight = 1000;
        private readonly int safetyMargin = 5;

        // --------------------------
        // Panel zentrieren in TabPage
        // --------------------------
        private void CenterPanelInTabPage()
        {
            //if (tabPage1 == null || panel1 == null)
            //    return;

            //int padding = 20;
            //int x, y;

            //// Sichtbarkeit der Scrollleisten prüfen
            //bool horizontalScrollVisible = tabPage1.HorizontalScroll.Visible;
            //bool verticalScrollVisible = tabPage1.VerticalScroll.Visible;

            //// Horizontal zentrieren oder linksbündig mit Puffer
            //x = horizontalScrollVisible ? padding : (tabPage1.ClientSize.Width - panel1.Width) / 2;
            //// Vertikal zentrieren oder obenbündig mit Puffer
            //y = verticalScrollVisible ? padding : (tabPage1.ClientSize.Height - panel1.Height) / 2;

            //panel1.Location = new Point(x, y);

            if (tabPage1 == null || panel1 == null)
                return;

            int clientW = tabPage1.ClientSize.Width;
            int clientH = tabPage1.ClientSize.Height;

            int x = (clientW - panel1.Width) / 2;
            int y = (clientH - panel1.Height) / 2;

            if (x < 0) x = 0;  // niemals negativ
            if (y < 0) y = 0;

            panel1.Location = new Point(x, y);

            // Manuelles Triggern der Layout-Aktualisierung ohne Scrollbars zu erzwingen
            tabPage1.PerformLayout();
            tabPage1.AutoScrollPosition = new Point(0, 0);
        }

        // --------------------------
        // Formgröße automatisch anpassen
        // --------------------------
        private void ResizeFormToFitLayout()
        {
            //int padding = 20;
            //int minWidth = 1200;
            //int layoutOverheadWidth = 14;    // Ausgleich für tabPage1/TabControl-Breitenverlust
            //int layoutOverheadHeight = 10;   // Zusätzliche vertikale Differenz durch Layouts

            //Rectangle wa = Screen.FromHandle(this.Handle).WorkingArea;

            //// Höhe aller UI-Elemente summieren
            //int uiHeight = skinControl1.Height + menuStrip1.Height + toolStrip1.Height + statusStrip1.Height;

            //// Zielgröße auf Basis von panel1-Größe + Rändern + Overhead
            ////int desiredWidth = panel1.Width + padding * 2 + layoutOverheadWidth;
            //int desiredWidth = panel1.Location.X + panel1.Width + padding + layoutOverheadWidth;
            ////int desiredHeight = panel1.Height + padding * 2 + uiHeight + layoutOverheadHeight;
            //int desiredHeight = panel1.Location.Y + panel1.Height + uiHeight + layoutOverheadHeight;

            //// Begrenzungen für Bildschirmfläche
            //int maxWidth = wa.Width - (2 * safetyMargin);
            //int maxHeight = wa.Height - (2 * safetyMargin);

            //// Endgültige Formgröße mit Grenzen anwenden
            //int finalWidth = Math.Min(Math.Max(desiredWidth, minWidth), maxWidth);
            //int finalHeight = Math.Min(Math.Max(desiredHeight, minFormHeight), maxHeight);

            //this.Width = finalWidth;
            //this.Height = finalHeight;

            //// Form im Bildschirm zentrieren
            //this.Location = new Point(
            //    wa.Left + (wa.Width - this.Width) / 2,
            //    wa.Top + (wa.Height - this.Height) / 2
            //);

            //// Scrollbereich unten/rechts ergänzen, damit kein Balken nötig wird
            //scrollPaddingPanel.Location = new Point(
            //    panel1.Left + panel1.Width + (padding - panel1.Left),
            //    panel1.Top + panel1.Height + (padding - panel1.Top)
            //);

            //// Panel zentrieren
            //CenterPanelInTabPage();

            int padding = 20;
            int minWidth = 1200;
            int layoutOverheadWidth = 14;    // Ausgleich für tabPage1/TabControl-Breitenverlust
            int layoutOverheadHeight = 10;   // Zusätzliche vertikale Differenz durch Layouts
            int safetyMargin = 5;

            // Bildschirmfläche ermitteln
            Rectangle wa = Screen.FromHandle(this.Handle).WorkingArea;

            // Höhe aller UI-Komponenten berechnen
            int uiHeight = skinControl1.Height + menuStrip1.Height + toolStrip1.Height + statusStrip1.Height;

            // Zielgröße inkl. Margin-Anteil (z. B. 20 px Rand) exakt berechnen
            int marginW = tabPage1.AutoScrollMargin.Width;
            int marginH = tabPage1.AutoScrollMargin.Height;

            int desiredWidth = panel1.Location.X + panel1.Width + marginW + layoutOverheadWidth;
            int desiredHeight = panel1.Location.Y + panel1.Height + marginH + uiHeight + layoutOverheadHeight;

            // Maximalgröße bezogen auf Bildschirm
            int maxWidth = wa.Width - 2 * safetyMargin;
            int maxHeight = wa.Height - 2 * safetyMargin;

            // Final begrenzte Werte anwenden
            int finalWidth = Math.Min(Math.Max(desiredWidth, minWidth), maxWidth);
            int finalHeight = Math.Min(Math.Max(desiredHeight, minFormHeight), maxHeight);

            // Fenstergröße setzen
            this.Width = finalWidth;
            this.Height = finalHeight;

            // Fenster zentrieren
            this.Location = new Point(
                wa.Left + (wa.Width - this.Width) / 2,
                wa.Top + (wa.Height - this.Height) / 2
            );

            // Scrollbereich absichern – exakt +20 px rechts/unten
            scrollPaddingPanel.Location = new Point(
                panel1.Right + (padding - panel1.Left),
                panel1.Bottom + (padding - panel1.Top)
            );

            // Scrollposition zurücksetzen (verhindert unnötige Scrollbars)
            tabPage1.AutoScrollPosition = new Point(0, 0);

            // Panel zentrieren
            CenterPanelInTabPage();

            tabPage1.AutoScrollPosition = new Point(0, 0);   // Scrollbalken zurücksetzen
            tabPage1.PerformLayout();                       // Layout sofort berechnen
        }

        // --------------------------
        // Dynamische Reaktion auf Panelgrößenänderung
        // --------------------------
        private void panel2_SizeChanged(object sender, EventArgs e)
        {
            int padding = 20;

            // panel1 erhält einen kleinen Rahmenabstand zu panel2
            panel1.Width = panel2.Width + 6;
            panel1.Height = panel2.Height + 6;
            panel1.Location = new Point(padding, padding);

            // Falls Änderung aus manueller Größenanpassung erfolgt:
            if (zwischenspeicher_differenz.Count == 2)
            {
                int diffWidth = zwischenspeicher_differenz[0];
                int diffHeight = zwischenspeicher_differenz[1];

                Rectangle wa = Screen.FromHandle(this.Handle).WorkingArea;

                // Höhe vergrößern bei Bedarf und genügend Platz
                if (diffHeight > 0 && panel2.Height >= 313)
                {
                    int desiredHeight = this.Height + diffHeight;
                    int screenMaxH = wa.Height - (2 * safetyMargin);
                    this.Height = Math.Min(Math.Max(desiredHeight, minFormHeight), screenMaxH);

                    int maxRow2 = screenMaxH - 598;
                    tableLayoutPanel1.RowStyles[2].Height =
                        Math.Min(tableLayoutPanel1.RowStyles[2].Height + diffHeight, maxRow2);

                    tabControl1.Height += diffHeight;
                }

                // Höhe verkleinern (sofern sinnvoll)
                if (diffHeight < 0 && panel2.Height >= 313)
                {
                    if (tabPage1.Height > panel1.Height - 40)
                    {
                        int diff = tabPage1.Height - (panel1.Height - 40);

                        this.Height -= diff;
                        tableLayoutPanel1.RowStyles[2].Height -= diff;
                        tabControl1.Height -= diff;
                        tabPage1.Height -= diff;
                    }
                }

                // Breite vergrößern bei Bedarf
                if (diffWidth > 0)
                {
                    int desiredWidth = this.Width + diffWidth;
                    int screenMaxW = wa.Width - (2 * safetyMargin);
                    this.Width = Math.Min(desiredWidth, screenMaxW);
                }

                // Breite verkleinern mit Mindestgrenze
                if (diffWidth < 0)
                {
                    int newWidth = this.Width + diffWidth;
                    int minWidth = 1000;
                    int screenMaxW = wa.Width - (2 * safetyMargin);
                    this.Width = Math.Max(Math.Min(newWidth, screenMaxW), minWidth);
                }

                // Form erneut zentrieren
                int newX = (wa.Width - this.Width) / 2;
                int newY = (wa.Height - this.Height) / 2;
                this.Location = new Point(newX, newY);

                // Puffer zurücksetzen
                zwischenspeicher_differenz.Clear();
            }

            // Scrollverhalten aktivieren und Ränder absichern
            tabPage1.AutoScroll = true;
            tabPage1.AutoScrollMargin = new Size(padding, padding);
            tabPage1.AutoScrollPosition = new Point(0, 0);

            // Scrollgrenzen unten/rechts sichern
            scrollPaddingPanel.Location = new Point(panel2.Right + 6 + padding, panel2.Bottom + 6 + padding);

            // Statusleiste aktualisieren (nur Text)
            toolStripStatusLabel2.Text = string.Format(
                "panel2: {0} x {1}  panel1: {2} x {3}",
                panel2.Width, panel2.Height, panel1.Width, panel1.Height
            );
        }
    }
}
