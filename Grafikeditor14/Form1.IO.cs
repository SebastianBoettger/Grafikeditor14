// ============================================================
// Form1.IO.cs – Ein- und Ausgabefunktionen für Layoutdaten
// Ziel: TXT-Dateien speichern und laden, Auswahl initialisieren
// .NET 4.0 kompatibel, kommentiert für neue Entwickler
// ============================================================

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        // ====================================================================
        // Speichert alle Felder von panel2 in eine .txt-Datei im Tag-Format
        // ====================================================================
        private void SpeichereFeldStrukturAlsTXT(string pfad)
        {
            using (StreamWriter writer = new StreamWriter(pfad, false, Encoding.UTF8))
            {
                int index = 0;

                foreach (Control ctrl in panel2.Controls)
                {
                    string[] tagArr = ctrl.Tag as string[];
                    if (tagArr == null || tagArr.Length < 15) continue; // nur gültige Tags

                    // Blocknummer
                    writer.WriteLine("[{0}]", index);

                    // Alle Zeilen des Tags ausgeben
                    foreach (string zeile in tagArr)
                        writer.WriteLine(zeile);

                    // Leerzeile zwischen Blöcken zur besseren Lesbarkeit
                    writer.WriteLine();
                    index++;
                }
            }
        }

        // ====================================================================
        // Öffnet Dialog zur Auswahl eines Layouts und lädt die enthaltenen Felder
        // ====================================================================
        private void mnuLayoutLaden_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "TXT-Dateien (*.txt)|*.txt",
                InitialDirectory = _layoutDir // Layout-Ordner vordefiniert
            };

            // Abbruch durch Benutzer
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Vorherige Inhalte vollständig entfernen
                panel2.Controls.Clear();
                _selection.Clear();
                EnsureHighlightBorder();
                highlightBorder.Visible = false;

                // Datei einlesen und Felder aufbauen
                LoadLayoutFromFile(dlg.FileName, panel2);

                // Erstes geladenes Feld automatisch markieren (wenn vorhanden)
                if (panel2.Controls.Count > 0)
                    AddToSelection(panel2.Controls[0]);

                // Fenstertitel aktualisieren
                this.Text = "Grafikeditor – [" + Path.GetFileName(dlg.FileName) + "]";

                // Panel neu zentrieren (mittig oder mit Scrolloffset)
                CenterPanelInTabPage();
            }
            catch (Exception ex)
            {
                // Fehleranzeige bei Dateizugriff
                MessageBox.Show("Laden fehlgeschlagen:\n" + ex.Message,
                                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====================================================================
        // Öffnet den Explorer direkt im Layoutverzeichnis
        // ====================================================================
        private void mnuExplorer_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _layoutDir);
            }
            catch
            {
                // Fehler beim Öffnen (z. B. Pfad existiert nicht)
                MessageBox.Show("Explorer konnte nicht geöffnet werden.",
                                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
