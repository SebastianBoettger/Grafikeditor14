// ============================================================
// Form1.UndoRedo.cs – Rückgängig- und Wiederholen-Funktionen
// Ziel: Benutzeraktionen nachvollziehbar machen und korrigierbar halten
// .NET 4.0 kompatibel, vollständig kommentiert für neue Entwickler
// ============================================================

using System;
using System.Windows.Forms;
using System.Drawing;
using Grafikeditor14.Core; // enthält MoveCommand, ResizeCommand, UndoRedoManager, EditorState

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        // ============================================================
        // Führt den zuletzt durchgeführten Befehl rückgängig aus
        // Hinweis: Funktion aktuell nicht global über Strg+Z eingebunden
        // ============================================================
        private void Undo()
        {
            _undoMgr.Undo();                // Letzten Schritt rückgängig machen
            AktualisiereHighlightRahmen();             // Auswahlrahmen aktualisieren (falls nötig)
        }

        // ============================================================
        // Wiederholt den zuletzt rückgängig gemachten Befehl
        // Hinweis: Funktion aktuell nicht global über Strg+Y eingebunden
        // ============================================================
        private void Redo()
        {
            _undoMgr.Redo();                // Letzten Undo wiederherstellen
            AktualisiereHighlightRahmen();             // Auswahlrahmen erneut anzeigen
        }

        // ============================================================
        // Ändert die Größe aller selektierten Steuerelemente
        // Unterstützt Rastermodus (Snap to Grid) und Undo/Redo
        // ============================================================
        private void VerarbeiteResize(int dxSign, int dySign)
        {
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1; // Rastergröße oder Schrittweite 1

            foreach (Control c in _selection)
            {
                // Neue Zielgröße mit Minimalgrößenprüfung (mindestens 5x5)
                Size ziel = new Size(
                    Math.Max(5, c.Width + dxSign * step),
                    Math.Max(5, c.Height + dySign * step));

                _undoMgr.Do(new ResizeCommand(c, ziel)); // Undo-fähige Größenänderung durchführen
                UpdateSizeTagAndDisplay(c);              // Tag-Einträge und Anzeige aktualisieren
            }

            AktualisiereHighlightRahmen();                          // Auswahlrahmen neu berechnen und anzeigen
        }

        // ============================================================
        // Verschiebt alle selektierten Steuerelemente um eine definierte Schrittweite
        // Unterstützt Rastermodus und Undo/Redo
        // ============================================================
        private void VerarbeiteMove(int dxSign, int dySign)
        {
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1; // Rastergröße oder Schrittweite 1

            foreach (Control c in _selection)
            {
                // Neue Zielposition berechnen
                Point ziel = new Point(
                    c.Left + dxSign * step,
                    c.Top + dySign * step);

                if (_state.RasterAktiv)
                    ziel = SnapToGrid(ziel);             // bei aktivem Raster anpassen

                _undoMgr.Do(new MoveCommand(c, ziel));   // Undo-fähige Bewegung durchführen
                UpdatePosTagAndDisplay(c);               // Tag-Einträge und Anzeige aktualisieren
            }

            AktualisiereHighlightRahmen();                          // Auswahlrahmen neu berechnen und anzeigen
        }
    }
}
