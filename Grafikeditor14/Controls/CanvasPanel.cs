using System;
using System.Drawing;
using System.Windows.Forms;

namespace Grafikeditor14.Controls
{
    /// <summary>
    /// Zeichenfläche mit optionalem Raster, Double-Buffering
    /// und intern gecachter Raster-Bitmap.
    /// .NET 4.0-konforme Version (kein C# 6-Syntax).
    /// </summary>
    public class CanvasPanel : Panel
    {
        private bool _rasterAktiv;
        private int _rasterAbstand = 10;
        private Color _rasterFarbe = Color.LightGray;

        private Bitmap _rasterBmp;     // wird bei Änderungen neu aufgebaut
        private TextureBrush _rasterBrush;

        public bool RasterAktiv
        {
            get { return _rasterAktiv; }
            set
            {
                _rasterAktiv = value;
                Invalidate();                // neu zeichnen
            }
        }

        public int RasterAbstand
        {
            get { return _rasterAbstand; }
            set
            {
                if (value <= 0) return;
                _rasterAbstand = value;
                RebuildRaster();
            }
        }

        public Color RasterFarbe
        {
            get { return _rasterFarbe; }
            set
            {
                _rasterFarbe = value;
                RebuildRaster();
            }
        }

        public CanvasPanel()
        {
            // Flimmerfreies Zeichnen aktivieren
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            RebuildRaster();
        }

        /*-------------------------- Rendering --------------------------*/
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_rasterAktiv && _rasterBrush != null)
            {
                e.Graphics.FillRectangle(_rasterBrush, ClientRectangle);
            }

            base.OnPaint(e); // Kinder zeichnen
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_rasterAktiv) RebuildRaster();
        }

        /*---------------------- Hilfsroutinen --------------------------*/
        private void RebuildRaster()
        {
            if (_rasterBmp != null) _rasterBmp.Dispose();
            if (_rasterBrush != null) _rasterBrush.Dispose();

            // Mini-Bitmap (Abstand × Abstand) anlegen,
            // nur Pixel (0,0) einfärben → später kacheln
            _rasterBmp = new Bitmap(_rasterAbstand, _rasterAbstand);
            using (Graphics g = Graphics.FromImage(_rasterBmp))
            {
                g.Clear(Color.Transparent);
                _rasterBmp.SetPixel(0, 0, _rasterFarbe);
            }

            _rasterBrush = new TextureBrush(_rasterBmp, System.Drawing.Drawing2D.WrapMode.Tile);
            Invalidate();
        }
    }
}