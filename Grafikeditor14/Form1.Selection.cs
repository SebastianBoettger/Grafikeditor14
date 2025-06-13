// ============================================================
// Form1.Selection.cs – Verwaltung der Feldselektion
// Ziel: Auswahl logischer Steuerelemente im Zeichenbereich steuern
// .NET 4.0 kompatibel, vollständig kommentiert
// ============================================================

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        // ==============================================
        // Globale Auswahlverwaltung
        // ==============================================

        /// <summary>
        /// Liste aller aktuell ausgewählten Steuerelemente (z. B. Label oder Panel).
        /// Wird bei Mehrfachauswahl oder beim Wechsel aktualisiert.
        /// </summary>
        private readonly List<Control> _selection = new List<Control>();

        // ============================================================
        // Auswahl hinzufügen
        // ============================================================

        /// <summary>
        /// Markiert das angegebene Steuerelement als aktiv ausgewählt.
        /// Setzt gleichzeitig alle anderen Selektionen zurück.
        /// </summary>
        /// <param name="ctrl">Das hinzuzufügende Steuerelement.</param>
        private void AddToSelection(Control ctrl)
        {
            if (ctrl == null) return;

            ClearSelection();                     // Nur eine Auswahl gleichzeitig erlaubt
            _selection.Add(ctrl);                 // Neues Element hinzufügen
            _state.ActiveControl = ctrl;          // Aktives Element aktualisieren

            ctrl.BringToFront();                  // 🔄 Sicherstellen, dass es ganz oben liegt

            SyncControlsAndTag(ctrl);             // UI-Eingabefelder synchronisieren
            AktualisiereHighlightRahmen();        // Roter Rahmen um neues Element zeichnen
        }

        // ============================================================
        // Auswahl entfernen
        // ============================================================

        /// <summary>
        /// Entfernt ein einzelnes Steuerelement aus der aktuellen Auswahl.
        /// Ist danach kein Element mehr markiert, wird alles zurückgesetzt.
        /// </summary>
        /// <param name="ctrl">Das zu entfernende Steuerelement.</param>
        private void RemoveFromSelection(Control ctrl)
        {
            _selection.Remove(ctrl);              // Element aus Auswahl entfernen

            if (_selection.Count == 0)
            {
                ClearSelection();                 // Alles löschen, wenn keine Übärig bleibt
            }
            else
            {
                AktualisiereHighlightRahmen();    // Neuen Auswahlrahmen berechnen

                if (_selection.Count == 1)
                    DisplayFieldProperties(_selection[0]);  // Tag-Infos anzeigen
                else
                    richTextBox7.Clear();                   // bei Mehrfachauswahl: Anzeige leeren
            }
        }

        // ============================================================
        // Auswahl leeren
        // ============================================================

        /// <summary>
        /// Hebt die aktuelle Auswahl auf, entfernt den Rahmen
        /// und leert die Eigenschaftenanzeige.
        /// </summary>
        private void ClearSelection()
        {
            _selection.Clear();                   // Auswahl komplett entfernen
            _state.ActiveControl = null;          // kein aktives Element mehr
            highlightBorder.Visible = false;      // Rahmen ausblenden
            richTextBox7.Clear();                 // Tag-Darstellung leeren
        }
    }
}
