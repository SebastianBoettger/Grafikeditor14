// ============================================================
// Form1.Factory.cs – Erzeugung und Duplizierung von Feldern
// Ziel: Einheitliche, nachvollziehbare Feldinitialisierung
// .NET 4.0 kompatibel, vollständig kommentiert
// ============================================================

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        // ============================================================
        // Erzeugt einen neuen Feldnamen, der eindeutig ist
        // Format: "FeldX" mit X = laufende Nummer
        // ============================================================

        /// <summary>
        /// Erzeugt einen eindeutigen Namen für ein neues Feld im Format "FeldX".
        /// Durchsucht alle bestehenden Felder auf panel2 nach der höchsten Nummer.
        /// </summary>
        /// <returns>Ein neuer, eindeutiger Feldname als String.</returns>
        private string GeneriereNeuenFeldnamen()
        {
            int maxNummer = 0; // höchste bisher verwendete Nummer

            // Alle vorhandenen Steuerelemente auf panel2 durchgehen
            foreach (Control ctrl in panel2.Controls)
            {
                if (ctrl.Name.StartsWith("Feld"))
                {
                    string nummerTeil = ctrl.Name.Substring(4); // nur den Zahlenteil extrahieren
                    int nummer;
                    if (Int32.TryParse(nummerTeil, out nummer))
                    {
                        if (nummer > maxNummer)
                            maxNummer = nummer; // größte Nummer merken
                    }
                }
            }

            // Neue Nummer um 1 erhöht vergeben
            return "Feld" + (maxNummer + 1);
        }

        // ============================================================
        // Dupliziert ein existierendes Steuerelement (Label oder Panel)
        // Die Kopie erhält eine neue Position und eindeutige Eigenschaften
        // ============================================================

        /// <summary>
        /// Erstellt eine Kopie eines bestehenden Feldes (Label oder Panel).
        /// Dabei werden Position, Größe, Farben etc. übernommen – der Name wird neu generiert.
        /// Die Kopie wird leicht versetzt eingefügt und sofort markiert.
        /// </summary>
        /// <param name="original">Das zu kopierende Steuerelement (Label oder Panel).</param>
        private void DupliziereFeld(Control original)
        {
            if (original == null || original.Parent != panel2) return; // nur Felder auf panel2 erlaubt

            Control copy; // neues Feld

            if (original is Label)
            {
                // Eigenschaften vom Original-Label übernehmen
                Label s = (Label)original;
                copy = new Label
                {
                    Text = s.Text,
                    Font = (Font)s.Font.Clone(),
                    Size = s.Size,
                    BackColor = s.BackColor,
                    BorderStyle = s.BorderStyle,
                    ForeColor = s.ForeColor,
                    TextAlign = s.TextAlign,
                    AutoSize = false
                };
            }
            else if (original is Panel)
            {
                // Eigenschaften vom Original-Panel übernehmen
                Panel s = (Panel)original;
                copy = new Panel
                {
                    Size = s.Size,
                    BackColor = s.BackColor,
                    BorderStyle = s.BorderStyle
                };
            }
            else return; // nur Label oder Panel werden unterstützt

            // Neue eindeutige Eigenschaften setzen
            copy.Name = GeneriereNeuenFeldnamen(); // neuer Name
            copy.Location = new Point(original.Left + 10, original.Top + 10); // leicht versetzt
            copy.Tag = BuildTagArray(copy, ""); // Tag-Array neu aufbauen

            // Standard-Events binden
            copy.MouseDown += FeldInPanel_MouseDown;
            copy.MouseMove += FeldInPanel_MouseMove;
            copy.MouseUp += FeldInPanel_MouseUp;
            copy.Click += NeuesFeld_Click;

            // Kopie dem Panel hinzufügen und Auswahl aktualisieren
            panel2.Controls.Add(copy);
            ClearSelection(); // vorherige Selektion aufheben
            AddToSelection(copy); // neue Kopie auswählen + hervorheben
        }
    }
}