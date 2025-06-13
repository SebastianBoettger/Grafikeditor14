// ============================================================
// Form1.Fields.cs – Logik für Feld-Handling und Tag-Verwaltung
// Ziel: Erzeugen, duplizieren, markieren, Eigenschaften lesen/schreiben
// .NET 4.0 kompatibel, kommentiert für neue Entwickler
// ============================================================

using System;
using System.Drawing;
using System.Windows.Forms;
using Grafikeditor14.Klassen;
using System.Collections.Generic;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        // Liste aller aktuell markierten Felder (z. B. zum Verschieben)
        //private List<Control> _selection = new List<Control>();

        // ====================================================================
        // Neues Feld erzeugen und sofort markieren
        // ====================================================================
        private void sC_B_NeuFelErz_Anw_Click(object sender, EventArgs e)
        {
            // 1. Eindeutigen Namen generieren (z. B. „Feld7“)
            string newName = GeneriereNeuenFeldnamen();

            // 2. Standard-Label mit Namen erzeugen (zentrale Fabrikmethode)
            Label lbl = FieldFactory.CreateDefaultLabel(panel2, newName);

            // 3. Maus-Events zuweisen (für Ziehen, Markieren, Klick etc.)
            lbl.MouseDown += FeldInPanel_MouseDown;
            lbl.MouseMove += FeldInPanel_MouseMove;
            lbl.MouseUp += FeldInPanel_MouseUp;
            lbl.Click += NeuesFeld_Click;

            // 4. Auswahl setzen (Highlight + Anzeige)
            ClearSelection();
            AddToSelection(lbl);

            // 5. Sichtbares Highlight anzeigen
            AktualisiereHighlightRahmen();
        }

        //// ====================================================================
        //// Eindeutigen Feldnamen erzeugen („Feld1“, „Feld2“ …)
        //// ====================================================================
        //private string GeneriereNeuenFeldnamen()
        //{
        //    int maxNummer = 0;

        //    // Alle vorhandenen Felder durchsuchen
        //    foreach (Control ctrl in panel2.Controls)
        //    {
        //        if (ctrl.Name.StartsWith("Feld"))
        //        {
        //            int nummer;
        //            if (int.TryParse(ctrl.Name.Substring(4), out nummer))
        //                maxNummer = Math.Max(maxNummer, nummer);
        //        }
        //    }

        //    // Nächsten freien Namen zurückgeben
        //    return "Feld" + (maxNummer + 1);
        //}

        // ====================================================================
        // Auswahl löschen (inkl. Rahmen und Eigenschaftsanzeige)
        // ====================================================================
        //private void ClearSelection()
        //{
        //    _selection.Clear();                         // Liste leeren
        //    _state.ActiveControl = null;                // internen Zustand zurücksetzen
        //    highlightBorder.Visible = false;            // Rahmen ausblenden
        //    richTextBox7.Clear();                       // Tag-Anzeige löschen
        //}

        // ====================================================================
        // Genau ein Control zur Auswahl hinzufügen (ersetzt vorherige)
        // ====================================================================
        //private void AddToSelection(Control ctrl)
        //{
        //    if (ctrl == null) return;

        //    ClearSelection();                           // alte Auswahl löschen
        //    _selection.Add(ctrl);                       // neues Feld eintragen
        //    _state.ActiveControl = ctrl;                // merken für weitere Operationen

        //    AktualisiereHighlightRahmen();                         // Rahmen anzeigen
        //    SyncControlsAndTag(ctrl);                   // Tag-Daten → UI synchronisieren
        //}

        // ====================================================================
        // Auswahlrahmen um alle markierten Felder anzeigen
        // ====================================================================
        //private void RefreshHighlight()
        //{
        //    if (_selection.Count == 0)
        //    {
        //        highlightBorder.Visible = false;        // nichts ausgewählt
        //        return;
        //    }

        //    // Start mit erstem Element
        //    Rectangle r = _selection[0].Bounds;

        //    // Alle weiteren integrieren
        //    foreach (Control c in _selection)
        //        r = Rectangle.Union(r, c.Bounds);

        //    r.Inflate(2, 2);                             // Rahmenpuffer außen
        //    highlightBorder.Bounds = r;                 // Rahmen setzen
        //    highlightBorder.Visible = true;             // anzeigen
        //}

        // ====================================================================
        // Doppelklick → Auswahl aufheben, Rahmen und Tag-Anzeige löschen
        // ====================================================================
        private void FeldInPanel_DoubleClick(object sender, EventArgs e)
        {
            Control clicked = sender as Control;
            if (clicked != null && clicked == _state.ActiveControl)
            {
                _state.ActiveControl = null;
                highlightBorder.Visible = false;
                richTextBox7.Clear();
            }
        }
    }
}