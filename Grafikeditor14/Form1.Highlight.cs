// ============================================================
// Form1.Highlight.cs – Darstellung von Auswahlrahmen
// Ziel: Markierte Steuerelemente visuell hervorheben
// .NET 4.0 kompatibel, vollständig kommentiert für neue Entwickler
// ============================================================

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grafikeditor14.Controls; // Für BorderPanel statt einfachem Panel

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        // ==========================================
        // Globale Felder für Highlighting
        // ==========================================

        /// <summary>
        /// Sichtbarer roter Rahmen zur Hervorhebung des ausgewählten Feldes.
        /// Wird dynamisch über dem aktiven oder markierten Steuerelement angezeigt.
        /// </summary>
        private BorderPanel highlightBorder;

        /// <summary>
        /// Zwischenspeicher des zuletzt verwendeten Rahmenbereichs.
        /// Dient zum gezielten Neuzeichnen bei Aktualisierung.
        /// </summary>
        private Rectangle _prevHighlight = Rectangle.Empty;

        // ============================================================
        // Zeigt einen roten Rahmen um das aktive Steuerelement
        // Wird beim Klick oder Fokuswechsel aufgerufen
        // ============================================================

        /// <summary>
        /// Zeigt den Highlight-Rahmen um das übergebene Steuerelement an.
        /// </summary>
        /// <param name="ctrl">Das zu markierende Steuerelement (z. B. Label oder Panel).</param>
        private void ZeigeHighlightUm(Control ctrl)
        {
            if (ctrl == null || ctrl.Parent != panel2)
            {
                highlightBorder.Visible = false; // Rahmen verstecken, wenn kein valides Ziel
                return;
            }

            // Rahmen exakt positionieren (+2 Pixel Abstand außen)
            highlightBorder.Bounds = new Rectangle(
                ctrl.Left - 2,
                ctrl.Top - 2,
                ctrl.Width + 4,
                ctrl.Height + 4);

            highlightBorder.Visible = true; // Rahmen einblenden
        }

        // ============================================================
        // Aktualisiert den Rahmen entsprechend der aktuellen Auswahl
        // Wird nach Bewegung, Größenänderung oder Auswahlwechsel aufgerufen
        // ============================================================

        /// <summary>
        /// Berechnet und setzt den Rahmen neu auf Basis der aktuell markierten Elemente.
        /// </summary>
        private void AktualisiereHighlightRahmen()
        {
            if (_selection.Count == 0)
            {
                // Kein Element ausgewählt → Rahmen ausblenden und alten Bereich löschen
                if (highlightBorder.Visible)
                    panel2.Invalidate(_prevHighlight);

                highlightBorder.Visible = false;
                _prevHighlight = Rectangle.Empty;
                return;
            }

            // Bounding-Box aller markierten Steuerelemente berechnen
            Rectangle r = _selection[0].Bounds;
            foreach (Control c in _selection.Skip(1))
                r = Rectangle.Union(r, c.Bounds);

            r.Inflate(2, 2); // Rahmenzugabe außen

            // Vorherigen Bereich entfernen (Löschen des alten Rahmens)
            if (!_prevHighlight.IsEmpty)
                panel2.Invalidate(_prevHighlight);

            // Neuen Rahmenbereich setzen
            highlightBorder.Bounds = r;
            highlightBorder.Visible = true;

            // Z-Reihenfolge anpassen: Rahmen direkt hinter dem aktiven Element anzeigen
            highlightBorder.SendToBack();
            _selection[0].BringToFront();

            // Aktuelle Position merken für späteres Neuzeichnen
            _prevHighlight = r;
        }

        // ============================================================
        // Stellt sicher, dass der Highlight-Rahmen im Canvas vorhanden ist
        // Wird z. B. nach Controls.Clear() wieder hinzugefügt
        // ============================================================

        /// <summary>
        /// Prüft, ob der Highlight-Rahmen existiert und korrekt im Panel eingebunden ist.
        /// Wenn nicht, wird er erneut hinzugefügt und nach hinten sortiert.
        /// </summary>
        private void EnsureHighlightBorder()
        {
            if (highlightBorder == null || highlightBorder.IsDisposed)
                return; // Noch nicht initialisiert oder bereits gelöscht

            if (!panel2.Controls.Contains(highlightBorder))
            {
                panel2.Controls.Add(highlightBorder);
                highlightBorder.SendToBack(); // Rahmen soll unter allen Feldern liegen
            }
        }
    }
}