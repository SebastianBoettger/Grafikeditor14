using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14.Core
{
    /// <summary>
    /// Zentraler Speicher für gemeinsam genutzte Flags
    /// und aktuell ausgewähltes Steuerelement.
    /// </summary>
    public class EditorState
    {
        public Control ActiveControl { get; set; }
        public bool RasterAktiv { get; set; }
        public int RasterAbstand { get; set; }
        public Panel HighlightBorder { get; set; }

        // ← Initialwerte im Konstruktor setzen
        public EditorState()
        {
            RasterAbstand = 10;
        }
    }
}
