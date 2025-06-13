using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Grafikeditor14.Core;
using Grafikeditor14.Controls;
using Grafikeditor14.Command;
using Grafikeditor14.Klassen;
using System.IO;
using System.Globalization;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        private readonly UndoRedoManager _undoMgr = new UndoRedoManager();
        private readonly EditorState _state = new EditorState();
        //private readonly List<Control> _selection = new List<Control>();
        private readonly PendingFieldProps _pending = new PendingFieldProps();
        private readonly FontDialog _fontDlg = new FontDialog();
        private bool _suspendTagUpdates = false;
        private readonly string _layoutDir =
            System.IO.Path.Combine(Application.StartupPath, "Layouts");
        //private Rectangle _prevHighlight = Rectangle.Empty;
        Panel scrollPaddingPanel;

        private CanvasPanel canvas
        {
            get { return (CanvasPanel)panel2; }
        }

        #region allgemeine Funktion
        
        private bool isMouseDown = false;
        private Point lastLocation;

        public void Form_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            lastLocation = e.Location;
        }

        public void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        public void Form_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }

        private const int WM_NCHITTEST = 0x84;
        private const int HTBOTTOMRIGHT = 17;
        private const int cGrip = 16;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle grip = new Rectangle(
                ClientSize.Width - cGrip,
                ClientSize.Height - cGrip,
                cGrip, cGrip);
            ControlPaint.DrawSizeGrip(e.Graphics, BackColor, grip);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                // Position des Mauszeigers in Client‑Koordinaten ermitteln
                int x = (int)(short)(m.LParam.ToInt64() & 0xFFFF);
                int y = (int)(short)((m.LParam.ToInt64() >> 16) & 0xFFFF);
                Point p = PointToClient(new Point(x, y));

                // Liegt er im Grip‑Quadrat?
                if (p.X >= ClientSize.Width - cGrip &&
                    p.Y >= ClientSize.Height - cGrip)
                {
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                    return;                      // **kein** base‑Aufruf mehr nötig
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Control grip = this.Controls["resizeGrip"];
            if (grip != null)
            {
                grip.Location = new Point(
                    this.ClientSize.Width - grip.Width,
                    this.ClientSize.Height - grip.Height
                );
                grip.BringToFront();
                grip.Visible = true; // sicherstellen!
            }

            CenterPanelInTabPage();
        }

        // -------------------------------------------------------- L O A D -----------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            // Sicherstellen, dass das Layoutverzeichnis vorhanden ist
            if (!Directory.Exists(_layoutDir))
                Directory.CreateDirectory(_layoutDir);

            PopulateSpeicherCombo();
            PopulateLayoutCombo();

            try
            {
                if (!Directory.Exists(_layoutDir))
                    Directory.CreateDirectory(_layoutDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Layout-Ordner konnte nicht angelegt werden:\n" + ex.Message,
                                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            scrollPaddingPanel = new Panel
            {
                Size = new Size(1, 1),
                BackColor = Color.Transparent
            };
            tabPage1.Controls.Add(scrollPaddingPanel);
            tabPage1.AutoScroll = false;
            tabPage1.AutoScroll = true;
            tabPage1.AutoScrollMargin = new Size(0, 0); // Verhindert künstliche Zusatzränder
            //tabPage1.AutoScrollMargin = new Size(20, 20); // 🔧 Neuer Abstandspuffer

            CenterPanelInTabPage();
            toolStripDropDownButton2.DropDown.AutoClose = false;
            toolStripTextBox1.Text = panel2.Width.ToString();
            toolStripTextBox2.Text = panel2.Height.ToString();
            toolStripTextBox3.Text = _state.RasterAktiv.ToString();

            radioButton_zentriert.Checked = true;
            _pending.Alignment = ContentAlignment.MiddleCenter;

            Font stdFont = new Font("Arial", 12f, FontStyle.Bold);
            rTB_schriftart.Text = "Arial, 12, Bold";
            rTB_schriftart.Font = stdFont;
            _pending.Font = stdFont;

            highlightBorder = new BorderPanel
            {
                Size = new Size(0, 0),
                Visible = false
            };

            highlightBorder.Paint += (s, pe) =>
            {
                using (Pen p = new Pen(Color.Red, 2))
                {
                    pe.Graphics.DrawRectangle(
                        p,
                        0, 0,
                        highlightBorder.Width - 1,
                        highlightBorder.Height - 1);
                }
            };
            panel2.Controls.Add(highlightBorder);
            EnsureHighlightBorder();

            toolStripStatusLabel1.Text = panel2.Width.ToString() + " " + panel2.Height.ToString() +
                                          "  " + panel1.Width.ToString() + " " + panel1.Height.ToString() +
                                          "  " + tabPage1.Width.ToString() + " " + tabPage1.Height.ToString();

            // ===============================
            // NEU: Resize-Grip direkt zur Form hinzufügen
            // ===============================
            var grip = new GripControl();
            grip.Name = "resizeGrip"; // für späteren Zugriff
            grip.Location = new Point(
                this.ClientSize.Width - grip.Width,
                this.ClientSize.Height - grip.Height
            );
            this.Controls.Add(grip);
            grip.BringToFront(); // unbedingt VOR dem ersten Rendern
            // ===============================
        }
        // ------------------------------------------------------------------------------------------------------

        #endregion

        #region Kopfzeile Funktion
        
        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        List<int> zwischenspeicher_differenz = new List<int>();
        private void toolStripMenuItemOK_Panelgröße_Click(object sender, EventArgs e)
        {
            zwischenspeicher_differenz.Add(Convert.ToInt32(toolStripTextBox1.Text) - panel2.Width);
            zwischenspeicher_differenz.Add(Convert.ToInt32(toolStripTextBox2.Text) - panel2.Height);
            panel2.Width = Convert.ToInt32(toolStripTextBox1.Text);
            panel2.Height = Convert.ToInt32(toolStripTextBox2.Text);
        }

        // Raster
        private void toolStripMenuItemOK_Rastergröße_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.DropDown.AutoClose = false;
            int abstand = Convert.ToInt32(toolStripTextBox3.Text);
            canvas.RasterAbstand = abstand;       // Canvas baut Raster neu auf
            _state.RasterAbstand = abstand;       // für SnapToGrid & Pfeiltasten
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.DropDown.AutoClose = false;
            canvas.RasterAktiv = true;            // Canvas zeigt Raster selbst an
            _state.RasterAktiv = true;           // für SnapToGrid & Pfeiltasten
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.DropDown.AutoClose = false;
            canvas.RasterAktiv = false;
            _state.RasterAktiv = false;
        }

        private void toolStripMenuItemSchließen_Rastergröße_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.DropDown.AutoClose = true;
            toolStripDropDownButton2.HideDropDown();
        }

        private Point _mouseDownLocationL;
        private void FeldInPanel_MouseDown(object s, MouseEventArgs e)
        {
            Control ctrl = s as Control;

            if (e.Button == MouseButtons.Left && Control.ModifierKeys.HasFlag(Keys.Control))
            {
                DupliziereFeld(ctrl);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                _mouseDownLocationL = e.Location;

                bool multi = (ModifierKeys & Keys.Shift) == Keys.Shift;

                if (!multi)
                {
                    ClearSelection();
                    AddToSelection(ctrl);
                }
                else
                {
                    if (_selection.Contains(ctrl))
                        RemoveFromSelection(ctrl);
                    else
                        AddToSelection(ctrl);
                }

                toolStripStatusLabel2.Text = ctrl.Name;
            }
        }

        private void FeldInPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selection.Count == 0) return;
            if (e.Button != MouseButtons.Left) return;

            Control lead = _selection[0];
            lead.Left = e.X + lead.Left - _mouseDownLocationL.X;
            lead.Top = e.Y + lead.Top - _mouseDownLocationL.Y;

            UpdatePosTagAndDisplay(lead);   // Tag live anpassen
            AktualisiereHighlightRahmen();             // 🔴 Rahmen mitschieben
        }

        private void FeldInPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_selection.Count == 0 || e.Button != MouseButtons.Left) return;

            Control lead = _selection[0];
            Point snapped = SnapToGrid(lead.Location);
            _undoMgr.Do(new MoveCommand(lead, snapped));
            lead.Location = snapped;

            UpdatePosTagAndDisplay(lead);
            AktualisiereHighlightRahmen();            // 🔴 Endposition anzeigen
        }

        private Point SnapToGrid(Point position)
        {
            if (canvas.RasterAktiv)
            {
                int gap = canvas.RasterAbstand;
                int x = (position.X + gap / 2) / gap * gap;
                int y = (position.Y + gap / 2) / gap * gap;
                return new Point(x, y);
            }
            return position;
        }

        private void toolStripButton_ToggleLabelPanel_Click(object sender, EventArgs e)
        {
            if (_selection.Count != 1) return;          // genau EIN Feld muss markiert sein

            Control active = _selection[0];

            /* ──────────────────────────────────────────────────────────────
               LABEL  ➜  PANEL
               ──────────────────────────────────────────────────────────────*/
            if (active is Label)
            {
                Label src = (Label)active;

                /* Auftragsmerkmal retten (Tag-Index 14) */
                string merkmal = "";
                string[] oldTag = src.Tag as string[];
                if (oldTag != null && oldTag.Length > 14)
                    merkmal = ExtractFieldName(oldTag[14]);

                /* neues Panel */
                Panel pnl = new Panel
                {
                    Location = src.Location,
                    Size = src.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = src.BackColor,
                    Name = src.Name
                };

                pnl.Tag = BuildTagArray(pnl, merkmal);     //  ⇦ Tag ohne Label-Eigenschaften

                /* Events */
                pnl.MouseDown += FeldInPanel_MouseDown;
                pnl.MouseMove += FeldInPanel_MouseMove;
                pnl.MouseUp += FeldInPanel_MouseUp;

                /* UI-Austausch */
                panel2.Controls.Add(pnl);
                panel2.Controls.Remove(src);
                src.Dispose();

                ClearSelection();
                AddToSelection(pnl);                       // → SyncControlsAndTag + richTextBox7
            }
            /* ──────────────────────────────────────────────────────────────
               PANEL  ➜  LABEL
               ──────────────────────────────────────────────────────────────*/
            else if (active is Panel)
            {
                Panel src = (Panel)active;

                /* Auftragsmerkmal retten */
                string merkmal = "";
                string[] oldTag = src.Tag as string[];
                if (oldTag != null && oldTag.Length > 14)
                    merkmal = ExtractFieldName(oldTag[14]);

                /* Standard-Label */
                Label lbl = new Label
                {
                    Location = src.Location,
                    Size = src.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = src.BackColor,
                    ForeColor = Color.Black,
                    Font = new Font("Arial", 12f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "",          // bewusst leer
                    AutoSize = false,
                    Name = src.Name
                };

                lbl.Tag = BuildTagArray(lbl, merkmal);     // vollständiger Label-Tag

                /* Events */
                lbl.MouseDown += FeldInPanel_MouseDown;
                lbl.MouseMove += FeldInPanel_MouseMove;
                lbl.MouseUp += FeldInPanel_MouseUp;
                lbl.Click += NeuesFeld_Click;

                panel2.Controls.Add(lbl);
                panel2.Controls.Remove(src);
                src.Dispose();

                ClearSelection();
                AddToSelection(lbl);                       // → SyncControlsAndTag + richTextBox7
            }
        }

        private void NeuesFeld_Click(object sender, EventArgs e)
        {

        }

        //private Panel highlightBorder;
        //private void ZeigeHighlightUm(Control ctrl)
        //{
        //    if (ctrl == null || ctrl.Parent != panel2)
        //    {
        //        highlightBorder.Visible = false;
        //        return;
        //    }

        //    highlightBorder.Bounds = new Rectangle(
        //        ctrl.Left - 2,
        //        ctrl.Top - 2,
        //        ctrl.Width + 4,
        //        ctrl.Height + 4);

        //    highlightBorder.Visible = true;

        //    ///* erst das Feld, dann nochmals den Rahmen nach vorn holen */
        //    //ctrl.BringToFront();
        //    //highlightBorder.BringToFront();
        //}

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (panel2.GetChildAtPoint(e.Location) == null)   // Klick ins Leere
                ClearSelection();
        }

        //private string GeneriereNeuenFeldnamen()
        //{
        //    int maxNummer = 0;

        //    foreach (Control ctrl in panel2.Controls)
        //    {
        //        if (ctrl.Name.StartsWith("Feld"))
        //        {
        //            string nummerTeil = ctrl.Name.Substring(4);
        //            int nummer;
        //            if (int.TryParse(nummerTeil, out nummer))
        //            {
        //                if (nummer > maxNummer)
        //                    maxNummer = nummer;
        //            }
        //        }
        //    }

        //    return "Feld" + (maxNummer + 1);
        //}

        //private void DupliziereFeld(Control original)
        //{
        //    if (original == null || original.Parent != panel2) return;

        //    Control copy;

        //    if (original is Label)
        //    {
        //        Label s = (Label)original;
        //        copy = new Label
        //        {
        //            Text = s.Text,
        //            Font = (Font)s.Font.Clone(),
        //            Size = s.Size,
        //            BackColor = s.BackColor,
        //            BorderStyle = s.BorderStyle,
        //            ForeColor = s.ForeColor,
        //            TextAlign = s.TextAlign,
        //            AutoSize = false
        //        };
        //    }
        //    else if (original is Panel)
        //    {
        //        Panel s = (Panel)original;
        //        copy = new Panel
        //        {
        //            Size = s.Size,
        //            BackColor = s.BackColor,
        //            BorderStyle = s.BorderStyle
        //        };
        //    }
        //    else return;

        //    copy.Name = GeneriereNeuenFeldnamen();
        //    copy.Location = new Point(original.Left + 10, original.Top + 10);
        //    copy.Tag = BuildTagArray(copy, "");

        //    /* Events */
        //    copy.MouseDown += FeldInPanel_MouseDown;
        //    copy.MouseMove += FeldInPanel_MouseMove;
        //    copy.MouseUp += FeldInPanel_MouseUp;
        //    copy.Click += NeuesFeld_Click;

        //    panel2.Controls.Add(copy);
        //    ClearSelection();
        //    AddToSelection(copy);           // Highlight + Tag-Sync
        //}

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (_suspendRadioEvents) return;

            if (radioButton_linksbündig.Checked) _pending.Alignment = ContentAlignment.MiddleLeft;
            if (radioButton_zentriert.Checked) _pending.Alignment = ContentAlignment.MiddleCenter;
            if (radioButton_rechtsbündig.Checked) _pending.Alignment = ContentAlignment.MiddleRight;

            if (_selection.Count == 1 && _selection[0] is Label)
                ((Label)_selection[0]).TextAlign = _pending.Alignment;

            UpdateTagAndDisplay(_selection.Count == 1 ? _selection[0] : null);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendTagUpdates) return;          // Rekursion verhindern

            _pending.AuftragMerkmal = (comboBox1.SelectedItem != null)
                                      ? comboBox1.SelectedItem.ToString()
                                      : "";

            UpdateTagAndDisplay(_selection.Count == 1 ? _selection[0] : null);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            _pending.Text = rTB_vorgabetext.Text;
        }

        private void DisplayFieldProperties(Control ctrl)
        {
            if (ctrl == null || richTextBox7 == null)
                return;

            string[] tagArr = ctrl.Tag as string[];
            if (tagArr != null && tagArr.Length == 15)
            {
                richTextBox7.Lines = tagArr;
                return;
            }

            List<string> lines = new List<string>();

            int alignVal = 32;
            Label lbl = ctrl as Label;
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignVal = 16;
                else if (lbl.TextAlign == ContentAlignment.MiddleRight) alignVal = 48;
            }

            lines.Add("Alignment=" + alignVal);
            lines.Add("PosY=" + ctrl.Top);
            lines.Add("Fontgröße=" + ((lbl != null) ? lbl.Font.Size : 0));
            lines.Add("Text=" + ((lbl != null) ? lbl.Text : ""));
            lines.Add("Füllzeichen=");
            lines.Add("Höhe=" + ctrl.Height);
            lines.Add("Textfarbe=" + ((lbl != null) ? lbl.ForeColor.ToArgb() : 0));
            lines.Add("Stellen=0");
            lines.Add("PosX=" + ctrl.Left);
            lines.Add("FeldName=" + ctrl.Name);
            lines.Add("FontName=" + ((lbl != null) ? lbl.Font.Name : ""));
            lines.Add("Breite=" + ctrl.Width);
            lines.Add("Fontstyle=" + ((lbl != null) ? (int)lbl.Font.Style : 0));
            lines.Add("Farbe=" + ctrl.BackColor.ToArgb());
            lines.Add("DSFeldname=");

            richTextBox7.Lines = lines.ToArray();
        }

        private string[] BuildTagArray(Control ctrl, string auftragsMerkmal)
        {
            Label lbl = ctrl as Label;

            int alignCode = 32;                // Default = Mitte
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignCode = 16;
                if (lbl.TextAlign == ContentAlignment.MiddleRight) alignCode = 48;
            }

            // unbedingt deutsches Zahlenformat verwenden
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

            string[] tag = new string[15];

            tag[0] = "Alignment=" + alignCode;
            tag[1] = "PosY=" + ctrl.Top;
            tag[2] = "Fontgröße=" + ((lbl != null) ? lbl.Font.Size.ToString(de) : "0");
            tag[3] = "Text=" + ((lbl != null) ? lbl.Text : "");
            tag[4] = "Füllzeichen=";                    // immer vorhanden
            tag[5] = "Höhe=" + ctrl.Height;
            tag[6] = "Textfarbe=" + ((lbl != null) ? lbl.ForeColor.ToArgb() : 0);
            tag[7] = "Stellen=0";                       // immer vorhanden
            tag[8] = "PosX=" + ctrl.Left;
            tag[9] = "FeldName=" + ctrl.Name;
            tag[10] = "FontName=" + ((lbl != null) ? lbl.Font.Name : "");
            tag[11] = "Breite=" + ctrl.Width;
            tag[12] = "Fontstyle=" + ((lbl != null) ? (int)lbl.Font.Style : 0);
            tag[13] = "Farbe=" + ctrl.BackColor.ToArgb();
            tag[14] = "DSFeldname=" + auftragsMerkmal;    // kann leer sein

            return tag;
        }

        //private void FeldInPanel_DoubleClick(object sender, EventArgs e)
        //{
        //    Control clicked = sender as Control;

        //    if (clicked != null && clicked == _state.ActiveControl)
        //    {
        //        _state.ActiveControl = null;
        //        highlightBorder.Visible = false;
        //        richTextBox7.Clear();
        //        toolStripStatusLabel2.Text = "";
        //    }
        //}

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_selection.Count == 0) return base.ProcessCmdKey(ref msg, keyData);

            /*  FOKUS IMMER AUF FORM HOLEN – wichtig nach Klick auf Panel  */
            this.Select();

            bool ctrl = (keyData & Keys.Control) == Keys.Control;
            Keys arrow = keyData & ~Keys.Control;

            int dx = 0, dy = 0;
            if (arrow == Keys.Left) dx = -1;
            if (arrow == Keys.Right) dx = 1;
            if (arrow == Keys.Up) dy = -1;
            if (arrow == Keys.Down) dy = 1;
            if (dx == 0 && dy == 0) return base.ProcessCmdKey(ref msg, keyData);

            if (ctrl) VerarbeiteResize(dx, dy);
            else VerarbeiteMove(dx, dy);

            ZeigeHighlightUm(_state.ActiveControl);
            return true;
        }

        //private void VerarbeiteResize(int dxSign, int dySign)
        //{
        //    int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

        //    foreach (Control c in _selection)
        //    {
        //        Size ziel = new Size(
        //            Math.Max(5, c.Width + dxSign * step),
        //            Math.Max(5, c.Height + dySign * step));

        //        _undoMgr.Do(new ResizeCommand(c, ziel));
        //        UpdateSizeTagAndDisplay(c);             // Tag aktualisieren
        //    }
        //    AktualisiereHighlightRahmen();                         // 🔴 Rahmen neu zeichnen
        //}

        //private void VerarbeiteMove(int dxSign, int dySign)
        //{
        //    int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

        //    foreach (Control c in _selection)
        //    {
        //        Point ziel = new Point(c.Left + dxSign * step,
        //                               c.Top + dySign * step);

        //        if (_state.RasterAktiv) ziel = SnapToGrid(ziel);

        //        _undoMgr.Do(new MoveCommand(c, ziel));
        //        UpdatePosTagAndDisplay(c);              // Tag anpassen
        //    }
        //    AktualisiereHighlightRahmen();                         // 🔴 Rahmen folgt
        //}

        private void SetActive(Control ctrl)
        {
            ClearSelection();
            if (ctrl != null)
                AddToSelection(ctrl);
        }

        //private void ClearSelection()
        //{
        //    _selection.Clear();
        //    _state.ActiveControl = null;
        //    highlightBorder.Visible = false;
        //    richTextBox7.Clear();
        //}

        //private void RemoveFromSelection(Control ctrl)
        //{
        //    _selection.Remove(ctrl);
        //    if (_selection.Count == 0)
        //        ClearSelection();
        //    else
        //    {
        //        AktualisiereHighlightRahmen();
        //        if (_selection.Count == 1)
        //            DisplayFieldProperties(_selection[0]);
        //        else
        //            richTextBox7.Clear();
        //    }
        //}

        //private void AktualisiereHighlightRahmen()
        //{
        //    /* ---------- keine Selektion ---------- */
        //    if (_selection.Count == 0)
        //    {
        //        if (highlightBorder.Visible)                   // alten Rahmen wegwischen
        //            panel2.Invalidate(_prevHighlight);

        //        highlightBorder.Visible = false;
        //        _prevHighlight = Rectangle.Empty;
        //        return;
        //    }

        //    /* ---------- Bounding-Rectangle berechnen ---------- */
        //    Rectangle r = _selection[0].Bounds;
        //    foreach (Control c in _selection.Skip(1))
        //        r = Rectangle.Union(r, c.Bounds);

        //    r.Inflate(2, 2);          // 2-px Rahmenzugabe

        //    /* ---------- alte Position löschen ---------- */
        //    if (!_prevHighlight.IsEmpty)
        //        panel2.Invalidate(_prevHighlight);

        //    /* ---------- neuen Rahmen setzen ---------- */
        //    highlightBorder.Bounds = r;
        //    highlightBorder.Visible = true;

        //    // Reihenfolge: Rahmen direkt hinter der Lead-Control
        //    highlightBorder.SendToBack();
        //    _selection[0].BringToFront();

        //    _prevHighlight = r;       // neue Position merken
        //}

        /// <summary>
        /// Entfernt optionalen Prefix „DSFeldname=“.
        /// </summary>
        private static string ExtractFieldName(string entry)
        {
            const string prefix = "DSFeldname=";
            return entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                   ? entry.Substring(prefix.Length)
                   : entry;
        }

        //private void AddToSelection(Control ctrl)
        //{
        //    ClearSelection();
        //    if (ctrl != null) _selection.Add(ctrl);
        //    _state.ActiveControl = ctrl;

        //    AktualisiereHighlightRahmen();            // Rahmen berechnen
        //    //highlightBorder.BringToFront(); // ← sicherheitshalber

        //    SyncControlsAndTag(ctrl);
        //}

        private void Field_Click(object sender, EventArgs e)
        {
            Control field = sender as Control;
            if (field == null) return;
        }

        //private void SpeichereFeldStrukturAlsTXT(string pfad)
        //{
        //    using (StreamWriter writer = new StreamWriter(pfad, false, Encoding.UTF8))
        //    {
        //        int index = 0;
        //        foreach (Control ctrl in panel2.Controls)
        //        {
        //            string[] tagArr = ctrl.Tag as string[];
        //            if (tagArr == null || tagArr.Length < 15) continue;

        //            writer.WriteLine("[{0}]", index);
        //            foreach (string zeile in tagArr)
        //            {
        //                writer.WriteLine(zeile);
        //            }
        //            writer.WriteLine(); // Leerzeile zwischen Einträgen
        //            index++;
        //        }
        //    }
        //}

        private void sC_B_Speichern_Click(object sender, EventArgs e)
        {
            string name = (comboBox3_ansichtSpeichern.SelectedItem as string).Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Bitte zuerst eine Zieldatei auswählen.",
                                "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Endung sicherstellen (robust gegen Groß-/Kleinschreibung oder manuelle Eingaben)
            if (!name.ToLowerInvariant().EndsWith(".txt"))
                name += ".txt";

            string fullPath = Path.Combine(_layoutDir, name);

            try
            {
                SpeichereFeldStrukturAlsTXT(fullPath);
                MessageBox.Show("Layout gespeichert in:\n" + name,
                                "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Speichern fehlgeschlagen:\n" + ex.Message,
                                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private enum Arbeitsmodus
        {
            Erzeugen,
            Bearbeiten
        }

        private void sC_Anwenden_Click(object sender, EventArgs e)
        {
            if (_state.ActiveControl == null) return;

            Control ctrl = _state.ActiveControl;
            Label lbl = ctrl as Label;
            Panel pnl = ctrl as Panel;

            // Undo-Gruppe eröffnen
            _undoMgr.Do(new MoveCommand(ctrl, ctrl.Location)); // Dummy zum Starten einer Gruppe

            // Hintergrund
            if (ctrl.BackColor != _pending.BackColor)
                _undoMgr.Do(new ColorCommand(ctrl, _pending.BackColor, true)); // ColorCommand selbst anlegen

            // Text / Schrift / Ausrichtung nur bei Label
            if (lbl != null)
            {
                if (lbl.Text != _pending.Text)
                    _undoMgr.Do(new TextCommand(lbl, _pending.Text));

                if (!lbl.Font.Equals(_pending.Font))
                    _undoMgr.Do(new FontCommand(lbl, _pending.Font));

                if (lbl.ForeColor != _pending.ForeColor)
                    _undoMgr.Do(new ColorCommand(lbl, _pending.ForeColor, false));

                if (lbl.TextAlign != _pending.Alignment)
                    _undoMgr.Do(new AlignCommand(lbl, _pending.Alignment));
            }

            // Auftragsmerkmal ins Tag-Array übernehmen
            if (!string.IsNullOrEmpty(_pending.AuftragMerkmal))
                ctrl.Tag = BuildTagArray(ctrl, _pending.AuftragMerkmal);

            // Highlight & Properties-Anzeige neu zeichnen
            DisplayFieldProperties(ctrl);
            AktualisiereHighlightRahmen();
        }

        private readonly ColorDialog _colorDlg = new ColorDialog();

        /* ---------------- Hintergrundfarbe wählen ---------------- */
        private void tsbBackColor_Click(object sender, EventArgs e)
        {
            if (_colorDlg.ShowDialog() == DialogResult.OK)
            {
                Color pickedColor = _colorDlg.Color;

                // 1) Zwischenspeicher setzen  →  sC_Anwenden_Click liest das später aus
                _pending.BackColor = pickedColor;

                // 2) Optional: Live-Vorschau auf dem aktuell markierten Feld
                if (_state.ActiveControl != null)
                    _state.ActiveControl.BackColor = pickedColor;
            }
        }

        /* ---------------- Schriftfarbe wählen ---------------- */
        private void tsbForeColor_Click(object sender, EventArgs e)
        {
            if (_colorDlg.ShowDialog() == DialogResult.OK)
            {
                Color pickedColor = _colorDlg.Color;

                _pending.ForeColor = pickedColor;

                Label lbl = _state.ActiveControl as Label;
                if (lbl != null)                       // Live-Vorschau nur für Labels
                    lbl.ForeColor = pickedColor;
            }
        }

        private void sC_B_hintergrundfarbe_Click(object sender, EventArgs e)
        {
            if (_colorDlg.ShowDialog() != DialogResult.OK) return;

            _pending.BackColor = _colorDlg.Color;
            p_hintergrundfarbe.BackColor = _colorDlg.Color;

            if (_selection.Count == 1)
                _selection[0].BackColor = _colorDlg.Color;

            UpdateTagAndDisplay(_selection.Count == 1 ? _selection[0] : null);
        }

        private void sC_B_textfarbe_Click(object sender, EventArgs e)
        {
            if (_colorDlg.ShowDialog() != DialogResult.OK) return;

            _pending.ForeColor = _colorDlg.Color;
            p_textfarbe.BackColor = _colorDlg.Color;

            if (_selection.Count == 1 && _selection[0] is Label)
                ((Label)_selection[0]).ForeColor = _colorDlg.Color;

            UpdateTagAndDisplay(_selection.Count == 1 ? _selection[0] : null);
        }

        private void sC_B_Schriftart_Click(object sender, EventArgs e)
        {
            if (_fontDlg.ShowDialog() != DialogResult.OK) return;

            _pending.Font = _fontDlg.Font;
            rTB_schriftart.Font = _fontDlg.Font;
            rTB_schriftart.Text = string.Format("{0}, {1}, {2}",
                                                _fontDlg.Font.Name,
                                                (int)_fontDlg.Font.Size,
                                                (_fontDlg.Font.Style == FontStyle.Bold) ? "Bold" :
                                                (_fontDlg.Font.Style == FontStyle.Italic) ? "Italic" : "Regular");

            if (_selection.Count == 1 && _selection[0] is Label)
                ((Label)_selection[0]).Font = (Font)_fontDlg.Font.Clone();

            UpdateTagAndDisplay(_selection[0]);
        }

        /// <summary>
        /// Setzt alle Eingabe-Controls und die Pending-Werte
        /// auf die Standard­einstellungen zurück.
        /// </summary>
        private void ResetInputControls()
        {
            /* ---------- ComboBox ------------------------------ */
            comboBox1.SelectedIndex = -1;
            _pending.AuftragMerkmal = "";

            /* ---------- Vorgabetext --------------------------- */
            rTB_vorgabetext.Clear();
            _pending.Text = "";

            /* ---------- Hintergrundfarbe ---------------------- */
            Color defaultBack = SystemColors.Control;
            p_hintergrundfarbe.BackColor = defaultBack;
            _pending.BackColor = defaultBack;

            /* ---------- Textfarbe ----------------------------- */
            p_textfarbe.BackColor = Color.Black;
            _pending.ForeColor = Color.Black;

            /* ---------- Schriftart ---------------------------- */
            Font stdFont = new Font("Arial", 12f, FontStyle.Bold);
            rTB_schriftart.Text = "Arial, 12, Bold";
            rTB_schriftart.Font = stdFont;
            _pending.Font = stdFont;

            /* ---------- Ausrichtung (RadioButtons) ------------ */
            radioButton_linksbündig.Checked = false;
            radioButton_zentriert.Checked = true;   // Standard
            radioButton_rechtsbündig.Checked = false;
            _pending.Alignment = ContentAlignment.MiddleCenter;
        }

        private void sC_B_Zurücksetzen_Click(object sender, EventArgs e)
        {
            ResetInputControls();
        }

        //private void sC_B_NeuFelErz_Anw_Click(object sender, EventArgs e)
        //{
        //    string newName = GeneriereNeuenFeldnamen();
        //    Label lbl = FieldFactory.CreateDefaultLabel(panel2, newName);

        //    lbl.MouseDown += FeldInPanel_MouseDown;
        //    lbl.MouseMove += FeldInPanel_MouseMove;
        //    lbl.MouseUp += FeldInPanel_MouseUp;
        //    lbl.Click += NeuesFeld_Click;

        //    /* Auswahl + Highlight */
        //    ClearSelection();
        //    AddToSelection(lbl);          // AktualisiereHighlightRahmen() wird dabei aufgerufen
        //    // und setzt Z-Reihenfolge schon korrekt
        //}

        /// <summary>
        /// Schreibt die aktuellen Left/Top-Koordinaten in das Tag-Array des Feldes
        /// und zeigt sie sofort in richTextBox7 an.
        /// </summary>
        private void UpdatePosTagAndDisplay(Control ctrl)
        {
            UpdateTagAndDisplay(ctrl);
            ValidateAndSyncSelected();
        }

        /// <summary>
        /// Schreibt aktuelle Breite & Höhe in das Tag-Array
        /// und zeigt es sofort in richTextBox7 an.
        /// </summary>
        private void UpdateSizeTagAndDisplay(Control ctrl)
        {
            UpdateTagAndDisplay(ctrl);
            ValidateAndSyncSelected();
        }

        private void LoadControlsFromTag(Control ctrl)
        {
            if (ctrl == null) return;

            string[] tagArr = ctrl.Tag as string[];
            if (tagArr == null || tagArr.Length < 15) return;

            /* Auftragsmerkmal → ComboBox */
            string ds = ExtractFieldName(tagArr[14]);
            int idx = comboBox1.FindStringExact(ds);
            comboBox1.SelectedIndex = idx;

            /* Text */
            Label lbl = ctrl as Label;
            rTB_vorgabetext.Text = (lbl != null) ? lbl.Text : "";

            /* Farben */
            p_hintergrundfarbe.BackColor = ctrl.BackColor;
            p_textfarbe.BackColor = (lbl != null) ? lbl.ForeColor : Color.Black;

            /* Schrift */
            Font fnt = (lbl != null) ? lbl.Font : new Font("Arial", 12f, FontStyle.Bold);
            rTB_schriftart.Text = string.Format("{0}, {1}, {2}",
                                  fnt.Name, (int)fnt.Size,
                                  (fnt.Style == FontStyle.Bold ? "Bold" :
                                   (fnt.Style == FontStyle.Italic ? "Italic" : "Regular")));
            rTB_schriftart.Font = fnt;
            _pending.Font = fnt;

            /* Ausrichtung */
            ContentAlignment al = (lbl != null) ? lbl.TextAlign : ContentAlignment.MiddleCenter;
            radioButton_linksbündig.Checked = (al == ContentAlignment.MiddleLeft);
            radioButton_zentriert.Checked = (al == ContentAlignment.MiddleCenter);
            radioButton_rechtsbündig.Checked = (al == ContentAlignment.MiddleRight);
            _pending.Alignment = al;

            /* Tag-Array in richTextBox7 anzeigen */
            richTextBox7.Lines = tagArr;
        }

        private void rTB_vorgabetext_TextChanged(object sender, EventArgs e)
        {
            _pending.Text = rTB_vorgabetext.Text;

            if (_selection.Count == 1 && _selection[0] is Label)
                ((Label)_selection[0]).Text = _pending.Text;

            UpdateTagAndDisplay(_selection[0]);
        }

        /// <summary>
        /// Schreibt sämtliche 15 Tag-Einträge aus den aktuellen Eingabe-Controls
        /// in das selektierte Feld und spiegelt sie sofort in richTextBox7.
        /// Aufruf nur bei exakt EINEM markierten Feld.
        /// </summary>
        private void WriteControlsToTag()
        {
            if (_selection.Count != 1) return;
            Control ctrl = _selection[0];
            string[] tag = ctrl.Tag as string[];
            if (tag == null || tag.Length < 15) return;

            // ▸ 0  Alignment
            Label lbl = ctrl as Label;
            ContentAlignment al = _pending.Alignment;
            if (lbl != null) lbl.TextAlign = al;
            tag[0] = "Alignment=" +
                     (al == ContentAlignment.MiddleLeft ? 16 :
                      al == ContentAlignment.MiddleRight ? 48 : 32);

            // ▸ 1 / 8  Position
            tag[1] = "PosY=" + ctrl.Top;
            tag[8] = "PosX=" + ctrl.Left;

            // ▸ 2 / 10 / 12  Schrift
            float fs = _pending.Font.Size;
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            tag[2] = "Fontgröße=" + fs.ToString(de);
            tag[10] = "FontName=" + _pending.Font.Name;
            tag[12] = "Fontstyle=" + (int)_pending.Font.Style;
            if (lbl != null) lbl.Font = (Font)_pending.Font.Clone();

            // ▸ 3  Text
            tag[3] = "Text=" + _pending.Text;
            if (lbl != null) lbl.Text = _pending.Text;

            // ▸ 4 / 7  Platzhalter sichern, falls jemand sie löscht
            if (!tag[4].StartsWith("Füllzeichen")) tag[4] = "Füllzeichen=";
            if (!tag[7].StartsWith("Stellen")) tag[7] = "Stellen=0";

            // ▸ 5 / 11 Größe
            tag[5] = "Höhe=" + ctrl.Height;
            tag[11] = "Breite=" + ctrl.Width;

            // ▸ 6 / 13 Farben
            ctrl.BackColor = _pending.BackColor;
            tag[13] = "Farbe=" + ctrl.BackColor.ToArgb();

            if (lbl != null)
            {
                lbl.ForeColor = _pending.ForeColor;
                tag[6] = "Textfarbe=" + lbl.ForeColor.ToArgb();
            }
            else
            {
                tag[6] = "Textfarbe=0";
            }

            // ▸ 14 DS-Feldname
            tag[14] = "DSFeldname=" + _pending.AuftragMerkmal;

            /* sofortige Visualisierung */
            richTextBox7.Lines = tag;
        }

        /// <summary>
        /// Zeigt ausschließlich den Tag-Inhalt eines Feldes in richTextBox7.
        /// Keine Änderung an ComboBox, Farb-Panels, Schrift-Box oder RadioButtons.
        /// </summary>
        private void ShowTagOnly(Control ctrl)
        {
            if (ctrl == null || richTextBox7 == null) return;

            string[] tagArr = ctrl.Tag as string[];
            if (tagArr != null && tagArr.Length == 15)
                richTextBox7.Lines = tagArr;
            else
                richTextBox7.Clear();
        }

        /// <summary>
        /// Synchronisiert *sämtliche* Eingabe-Controls + Tag-Anzeige
        /// mit den Eigenschaften eines Feldes.
        /// Wird nur verwendet, wenn exakt EIN Feld selektiert ist.
        /// </summary>
        private bool _suspendRadioEvents = false;   // Klassenfeld

        private void SyncControlsAndTag(Control ctrl)
        {
            if (ctrl == null || _selection.Count != 1) return;

            string[] tag = ctrl.Tag as string[];
            if (tag == null || tag.Length < 15) return;

            Label lbl = ctrl as Label;

            _suspendTagUpdates = true;            //  ▼ Handler kurzfristig blockieren
            _suspendRadioEvents = true;

            /* -------- Auftragsmerkmal → ComboBox ---------------------------- */
            string ds = ExtractFieldName(tag[14]);
            comboBox1.SelectedIndex = comboBox1.FindStringExact(ds);
            _pending.AuftragMerkmal = ds;

            /* -------- Text --------------------------------------------------- */
            string text = (lbl != null) ? lbl.Text : "";
            rTB_vorgabetext.Text = text;
            _pending.Text = text;

            /* -------- Farben ------------------------------------------------- */
            p_hintergrundfarbe.BackColor = ctrl.BackColor;
            p_textfarbe.BackColor = (lbl != null) ? lbl.ForeColor : Color.Black;
            _pending.BackColor = ctrl.BackColor;
            _pending.ForeColor = p_textfarbe.BackColor;

            /* -------- Schrift ------------------------------------------------ */
            Font f = (lbl != null) ? (Font)lbl.Font.Clone()
                                   : new Font("Arial", 12f, FontStyle.Bold);
            _pending.Font = (Font)f.Clone();
            rTB_schriftart.Font = f;
            rTB_schriftart.Text = String.Format("{0}, {1}, {2}",
                                                f.Name, (int)f.Size,
                                                (f.Style == FontStyle.Bold) ? "Bold" :
                                                (f.Style == FontStyle.Italic) ? "Italic" : "Regular");

            /* -------- Ausrichtung ------------------------------------------- */
            ContentAlignment al = (lbl != null) ? lbl.TextAlign : ContentAlignment.MiddleCenter;
            _pending.Alignment = al;

            radioButton_linksbündig.Checked = (al == ContentAlignment.MiddleLeft);
            radioButton_zentriert.Checked = (al == ContentAlignment.MiddleCenter);
            radioButton_rechtsbündig.Checked = (al == ContentAlignment.MiddleRight);

            _suspendRadioEvents = false;
            _suspendTagUpdates = false;        //  ▲ Handler wieder freigeben

            richTextBox7.Lines = tag;             // Tag sofort anzeigen
        }

        private void ApplyPendingToField()
        {
            if (_selection.Count != 1) return;

            Control ctrl = _selection[0];
            string[] tag = ctrl.Tag as string[];
            if (tag == null || tag.Length < 15) return;

            /* Text */
            Label lbl = ctrl as Label;
            if (lbl != null)
                lbl.Text = _pending.Text;
            tag[3] = "Text=" + _pending.Text;

            /* Auftragsmerkmal */
            tag[14] = "DSFeldname=" + _pending.AuftragMerkmal;

            /* Farben */
            ctrl.BackColor = _pending.BackColor;
            tag[13] = "Farbe=" + ctrl.BackColor.ToArgb();

            if (lbl != null)
            {
                lbl.ForeColor = _pending.ForeColor;
                tag[6] = "Textfarbe=" + lbl.ForeColor.ToArgb();

                lbl.Font = (Font)_pending.Font.Clone();
                tag[10] = "FontName=" + lbl.Font.Name;
                tag[2] = "Fontgröße=" + (int)lbl.Font.Size;
                tag[12] = "Fontstyle=" + (int)lbl.Font.Style;

                lbl.TextAlign = _pending.Alignment;
                int aval = (_pending.Alignment == ContentAlignment.MiddleLeft) ? 16 :
                           (_pending.Alignment == ContentAlignment.MiddleRight) ? 48 : 32;
                tag[0] = "Alignment=" + aval;
            }

            /* Tag-Anzeige erneuern */
            richTextBox7.Lines = tag;
        }

        /// <summary>
        /// Schreibt ALLE 15 Tag-Zeilen des angegebenen Controls neu
        /// (nach seinem aktuellen Zustand) und zeigt sie sofort in
        /// richTextBox7 an.
        /// </summary>
        private void UpdateTagAndDisplay(Control ctrl)
        {
            if (ctrl == null) return;

            /* vorhandenes Tag oder leeres Grundgerüst */
            string[] tag = ctrl.Tag as string[];
            if (tag == null || tag.Length < 15)
                tag = BuildTagArray(ctrl, "");          // liefert Array[15]

            Label lbl = ctrl as Label;

            /* 0  Alignment */
            int aval = (lbl == null) ? 32 :
                       (lbl.TextAlign == ContentAlignment.MiddleLeft) ? 16 :
                       (lbl.TextAlign == ContentAlignment.MiddleRight) ? 48 : 32;
            tag[0] = "Alignment=" + aval;

            /* 1 / 8  Position */
            tag[1] = "PosY=" + ctrl.Top;
            tag[8] = "PosX=" + ctrl.Left;

            /* 5 / 11  Größe */
            tag[5] = "Höhe=" + ctrl.Height;
            tag[11] = "Breite=" + ctrl.Width;

            /* 13  BackColor (immer vorhanden) */
            tag[13] = "Farbe=" + ctrl.BackColor.ToArgb();

            /* 14  Auftragsmerkmal – kommt aus _pending */
            tag[14] = "DSFeldname=" + _pending.AuftragMerkmal;

            /* Label-spezifische Felder */
            if (lbl != null)
            {
                tag[3] = "Text=" + lbl.Text;
                tag[6] = "Textfarbe=" + lbl.ForeColor.ToArgb();
                tag[10] = "FontName=" + lbl.Font.Name;
                tag[2] = "Fontgröße=" + (int)lbl.Font.Size;
                tag[12] = "Fontstyle=" + (int)lbl.Font.Style;
            }
            else   /* Panel → leeren */
            {
                tag[3] = "Text=";
                tag[6] = "Textfarbe=0";
                tag[10] = "FontName=";
                tag[2] = "Fontgröße=0";
                tag[12] = "Fontstyle=0";
            }

            /* Tag zurücklegen & richTextBox anzeigen */
            ctrl.Tag = tag;
            if (_selection.Count == 1 && _selection[0] == ctrl && richTextBox7 != null)
                richTextBox7.Lines = tag;
        }

        /// <summary>
        /// Prüft, ob Tag, Anzeige (richTextBox7) und Eingabe-Controls mit den
        /// Eigenschaften des aktuell selektierten Feldes übereinstimmen.
        /// Stellt bei Abweichungen sofort Konsistenz her.
        ///
        /// Wird nur ausgeführt, wenn genau EIN Feld markiert ist.
        /// </summary>
        private void ValidateAndSyncSelected()
        {
            if (_selection.Count != 1) return;

            Control ctrl = _selection[0];
            string[] tag = ctrl.Tag as string[];
            if (tag == null || tag.Length < 15) return;

            /* ---------- 1. Tag-Daten gegen Control prüfen ---------- */
            bool tagDirty = false;

            /* Position / Größe */
            if (tag[1] != "PosY=" + ctrl.Top) tagDirty = true;
            if (tag[8] != "PosX=" + ctrl.Left) tagDirty = true;
            if (tag[5] != "Höhe=" + ctrl.Height) tagDirty = true;
            if (tag[11] != "Breite=" + ctrl.Width) tagDirty = true;
            if (tag[13] != "Farbe=" + ctrl.BackColor.ToArgb()) tagDirty = true;

            /* Label-spezifisch */
            Label lbl = ctrl as Label;
            if (lbl != null)
            {
                if (tag[3] != "Text=" + lbl.Text) tagDirty = true;
                if (tag[6] != "Textfarbe=" + lbl.ForeColor.ToArgb()) tagDirty = true;
                if (tag[10] != "FontName=" + lbl.Font.Name) tagDirty = true;
                if (tag[2] != "Fontgröße=" + (int)lbl.Font.Size) tagDirty = true;
                if (tag[12] != "Fontstyle=" + (int)lbl.Font.Style) tagDirty = true;
            }

            /* Tag anpassen, falls nötig */
            if (tagDirty)
                UpdateTagAndDisplay(ctrl);   // schreibt Tag-Zeilen komplett neu

            /* ---------- 2. Anzeige in richTextBox7 prüfen ---------- */
            if (!Enumerable.SequenceEqual(richTextBox7.Lines, (string[])ctrl.Tag))
                richTextBox7.Lines = (string[])ctrl.Tag;

            /* ---------- 3. Eingabe-Controls synchronisieren -------- */

            SyncControlsAndTag(ctrl);        // setzt ComboBox, Farben, Schrift, Radio-Buttons
        }

        /// <summary>
        /// Vergleicht Tag-Daten, Feld-Eigenschaften und die Anzeige
        /// in richTextBox7.  Abweichungen werden farbig markiert.
        /// </summary>
        private void CheckTagConsistency(Control ctrl)
        {
            string[] tag = ctrl.Tag as string[];
            if (tag == null || tag.Length < 15) return;

            List<string> diff = new List<string>();

            /* Beispiel: Alignment */
            int alignTag = int.Parse(tag[0].Substring(10));
            int alignActual = 32;
            Label lbl = ctrl as Label;
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignActual = 16;
                if (lbl.TextAlign == ContentAlignment.MiddleRight) alignActual = 48;
            }
            if (alignTag != alignActual) diff.Add("Alignment");

            /* … prüfen Sie hier beliebig weitere Felder … */

            if (diff.Count == 0)
                MessageBox.Show("Alles konsistent!", "Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Abweichungen: " + String.Join(", ", diff), "Check",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Repräsentiert die 15 Zeilen eines Tag-Blocks.
        /// </summary>
        private sealed class TagBlock
        {
            public string[] Lines;          // exakt 15 Einträge, unverändert
            public int Align;
            public int PosX, PosY;
            public int Width, Height;
            public string Text;
            public string FeldName;
            public Font Font;
            public Color BackColor;
            public Color ForeColor;
            public string AuftragMerkmal;
        }

        /// <summary>
        /// Liest eine TXT-Layoutdatei im bekannten Format,
        /// erzeugt alle Felder auf <paramref name="canvas"/> und
        /// gibt sie – falls benötigt – als Liste zurück.
        /// </summary>
        private List<Control> LoadLayoutFromFile(string filePath, Panel canvas)
        {
            List<Control> list = new List<Control>();

            using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
            {
                List<string> buf = new List<string>();
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.StartsWith("[") && line.EndsWith("]"))      // neuer Block
                    {
                        if (buf.Count > 0)
                            list.Add(BuildControlFromTag(ParseTagBlock(buf), canvas));
                        buf.Clear();
                    }
                    else if (line.Length > 0 && line.Contains("="))
                        buf.Add(line);
                }
                if (buf.Count > 0)
                    list.Add(BuildControlFromTag(ParseTagBlock(buf), canvas));
            }

            AutoResizeCanvas(canvas);    //  <<<  neu
            return list;
        }

        /// <summary>
        /// Zerlegt 15 Zeilen in ein TagBlock-Objekt.
        /// </summary>
        private TagBlock ParseTagBlock(List<string> lines)
        {
            /* Schlüssel-Wert-Tabelle aufbauen – Reihenfolge egal */
            Dictionary<string, string> dict =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string l in lines)
            {
                int eq = l.IndexOf('=');
                if (eq > 0)
                {
                    string k = l.Substring(0, eq).Trim();
                    string v = l.Substring(eq + 1).Trim();
                    dict[k] = v;
                }
            }

            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");

            TagBlock t = new TagBlock();
            t.Lines = lines.ToArray();
            t.Align = DictParseInt(dict, "Alignment", 32);
            t.PosX = DictParseInt(dict, "PosX", 0);
            t.PosY = DictParseInt(dict, "PosY", 0);
            t.Width = DictParseInt(dict, "Breite", 110);
            t.Height = DictParseInt(dict, "Höhe", 25);
            t.Text = DictGet(dict, "Text");
            t.FeldName = DictGet(dict, "FeldName");
            t.AuftragMerkmal = DictGet(dict, "DSFeldname");

            /* Farben */
            t.BackColor = DictParseColor(dict, "Farbe", SystemColors.Control);
            t.ForeColor = DictParseColor(dict, "Textfarbe", Color.Black);

            /* Fontdaten */
            string fName = DictGet(dict, "FontName");
            if (string.IsNullOrEmpty(fName)) fName = "Arial";

            float fSize;
            if (!Single.TryParse(DictGet(dict, "Fontgröße"),
                                 NumberStyles.Any, de, out fSize))
                fSize = 10f;

            int styleInt;
            FontStyle style = Int32.TryParse(DictGet(dict, "Fontstyle"), out styleInt)
                              ? (FontStyle)styleInt
                              : FontStyle.Regular;

            t.Font = new Font(fName, fSize, style);

            return t;
        }

        /// <summary>
        /// Baut aus einem TagBlock ein vollständig initialisiertes Label
        /// (inkl. 15-Zeilen-Tag) und fügt es dem Canvas hinzu.
        /// </summary>
        private Control BuildControlFromTag(TagBlock t, Panel canvas)
        {
            Label lbl = new Label
            {
                Text = t.Text,
                Font = (Font)t.Font.Clone(),
                ForeColor = t.ForeColor,
                BackColor = t.BackColor,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = false,
                Width = t.Width > 0 ? t.Width : 110,
                Height = t.Height > 0 ? t.Height : 25,
                Left = t.PosX,
                Top = t.PosY,
                Name = String.IsNullOrEmpty(t.FeldName)
                              ? GeneriereNeuenFeldnamen()
                              : t.FeldName
            };

            // Alignment-Code (16 / 32 / 48)
            switch (t.Align)
            {
                case 16: lbl.TextAlign = ContentAlignment.MiddleLeft; break;
                case 48: lbl.TextAlign = ContentAlignment.MiddleRight; break;
                default: lbl.TextAlign = ContentAlignment.MiddleCenter; break;
            }

            /* ► EINHEITLICHES 15-Zeilen-Tag – damit alle späteren Routinen greifen */
            lbl.Tag = BuildTagArray(lbl, t.AuftragMerkmal);

            /* Events */
            lbl.MouseDown += FeldInPanel_MouseDown;
            lbl.MouseMove += FeldInPanel_MouseMove;
            lbl.MouseUp += FeldInPanel_MouseUp;
            lbl.Click += NeuesFeld_Click;

            canvas.Controls.Add(lbl);
            return lbl;
        }

        //private void mnuLayoutLaden_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog dlg = new OpenFileDialog
        //    {
        //        Filter = "TXT-Dateien (*.txt)|*.txt",
        //        InitialDirectory = _layoutDir
        //    };
        //    if (dlg.ShowDialog() != DialogResult.OK) return;

        //    panel2.Controls.Clear();
        //    _selection.Clear();
        //    EnsureHighlightBorder();
        //    highlightBorder.Visible = false;

        //    try
        //    {
        //        LoadLayoutFromFile(dlg.FileName, panel2);

        //        if (panel2.Controls.Count > 0)
        //            AddToSelection(panel2.Controls[0]);

        //        this.Text = "Grafikeditor – [" + Path.GetFileName(dlg.FileName) + "]";

        //        ResizeFormToFitLayout();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Laden fehlgeschlagen:\\n" + ex.Message,
        //                        "Fehler", MessageBoxButtons.OK,
        //                        MessageBoxIcon.Error);
        //    }
        //}

        private static string DictGet(Dictionary<string, string> dict, string key)
        {
            string val;
            return dict.TryGetValue(key, out val) ? val : string.Empty;
        }

        private static int DictParseInt(Dictionary<string, string> dict,
                                        string key, int defaultValue)
        {
            int n;
            return Int32.TryParse(DictGet(dict, key), out n) ? n : defaultValue;
        }

        private static Color DictParseColor(Dictionary<string, string> dict,
                                            string key, Color defaultColor)
        {
            int argb;
            return Int32.TryParse(DictGet(dict, key), out argb)
                   ? Color.FromArgb(argb)
                   : defaultColor;
        }

        //private void mnuExplorer_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        System.Diagnostics.Process.Start("explorer.exe", _layoutDir);
        //    }
        //    catch { }
        //}

        private void PopulateLayoutCombo()
        {
            comboBox2_ansicht.Items.Clear();

            if (!Directory.Exists(_layoutDir)) return;

            string[] files = Directory.GetFiles(_layoutDir, "*.txt");
            foreach (string path in files)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                comboBox2_ansicht.Items.Add(name);
            }
        }

        private void comboBox2_ansicht_DropDown(object sender, EventArgs e)
        {
            PopulateLayoutCombo();
        }

        //private void sC_B_ansichauswahl_Click(object sender, EventArgs e)
        //{
        //    if (comboBox2_ansicht.SelectedItem == null)
        //    {
        //        MessageBox.Show("Bitte zuerst eine Ansicht wählen.",
        //                        "Hinweis", MessageBoxButtons.OK,
        //                        MessageBoxIcon.Information);
        //        return;
        //    }

        //    string fileName = comboBox2_ansicht.SelectedItem + ".txt";
        //    string full = Path.Combine(_layoutDir, fileName);

        //    if (!File.Exists(full))
        //    {
        //        MessageBox.Show("Datei nicht gefunden:\\n" + full,
        //                        "Fehler", MessageBoxButtons.OK,
        //                        MessageBoxIcon.Error);
        //        return;
        //    }

        //    panel2.Controls.Clear();
        //    _selection.Clear();
        //    EnsureHighlightBorder();
        //    highlightBorder.Visible = false;

        //    LoadLayoutFromFile(full, panel2);

        //    if (panel2.Controls.Count > 0)
        //        AddToSelection(panel2.Controls[0]);

        //    ResizeFormToFitLayout();

        //    this.Text = "Grafikeditor – [" + fileName + "]";
        //}

        /// <summary>
        /// passt panel2 so an, dass alle enthaltenen Controls sichtbar sind
        /// (10 px Rand).
        /// </summary>
        private void AutoResizeCanvas(Panel canvas)
        {
            if (canvas.Controls.Count == 0) return;

            Rectangle bounds = canvas.Controls[0].Bounds;
            foreach (Control c in canvas.Controls) bounds = Rectangle.Union(bounds, c.Bounds);

            const int margin = 10;
            canvas.Width = bounds.Right + margin;
            canvas.Height = bounds.Bottom + margin;

            CenterPanelInTabPage();
        }

        private void sC_B_ansichauswahl_Click(object sender, EventArgs e)
        {
            if (comboBox2_ansicht.SelectedItem == null)
            {
                MessageBox.Show("Bitte zuerst eine Ansicht wählen.",
                                "Hinweis", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Möchten Sie das aktuelle Layout vor dem Laden speichern?",
                "Layout speichern?",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
                return;

            if (result == DialogResult.Yes)
            {
                string name = comboBox3_ansichtSpeichern.SelectedItem as string;

                if (!string.IsNullOrEmpty(name))
                {
                    name = name.Trim();
                    if (!name.ToLowerInvariant().EndsWith(".txt"))
                        name += ".txt";

                    string fullPath = Path.Combine(_layoutDir, name);

                    try
                    {
                        SpeichereFeldStrukturAlsTXT(fullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Speichern fehlgeschlagen:\n" + ex.Message,
                                        "Fehler", MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Kein gültiger Dateiname zum Speichern vorhanden.",
                                    "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string fileName = comboBox2_ansicht.SelectedItem + ".txt";
            string full = Path.Combine(_layoutDir, fileName);

            if (!File.Exists(full))
            {
                MessageBox.Show("Datei nicht gefunden:\n" + full,
                                "Fehler", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }

            // 🔄 Bestehende Felder löschen
            panel2.Controls.Clear();
            ClearSelection();               // Auswahl löschen
            EnsureHighlightBorder();        // Rahmen wieder sicherstellen
            highlightBorder.Visible = false;

            LoadLayoutFromFile(full, panel2);

            if (panel2.Controls.Count > 0)
                AddToSelection(panel2.Controls[0]);

            ResizeFormToFitLayout();
            tabPage1.AutoScrollPosition = new Point(0, 0);
            this.Text = "Grafikeditor – [" + fileName + "]";
        }

        /// <summary>
        /// Stellt sicher, dass der rote Highlight-Rahmen im Canvas existiert.
        /// Wird er gelöscht (z. B. durch panel2.Controls.Clear),
        /// wird er automatisch neu hinzugefügt.
        /// </summary>
        //private void EnsureHighlightBorder()
        //{
        //    if (highlightBorder == null || highlightBorder.IsDisposed)
        //        return;                                  // wurde noch gar nicht erzeugt

        //    if (!panel2.Controls.Contains(highlightBorder))
        //    {
        //        panel2.Controls.Add(highlightBorder);
        //        highlightBorder.SendToBack();            // zunächst hinter alles legen
        //    }
        //}

        private void PopulateSpeicherCombo()
        {
            comboBox3_ansichtSpeichern.Items.Clear();

            if (!Directory.Exists(_layoutDir)) return;

            string[] files = Directory.GetFiles(_layoutDir, "*.txt");

            HashSet<string> uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in files)
            {
                string name = Path.GetFileNameWithoutExtension(path).Trim();
                if (!string.IsNullOrEmpty(name) && uniqueNames.Add(name))
                    comboBox3_ansichtSpeichern.Items.Add(name);
            }

            // optional alphabetisch sortieren
            comboBox3_ansichtSpeichern.Sorted = true;
        }

        private void sC_B_ansichtSpeichernFür_Click(object sender, EventArgs e)
        {
            string eingabe = Microsoft.VisualBasic.Interaction.InputBox(
                "Bitte Dateinamen für neue Layout-Datei angeben (ohne .txt):",
                "Neues Layout benennen", "");

            if (string.IsNullOrWhiteSpace(eingabe)) return;

            string name = eingabe.Trim();

            // Endung ergänzen, falls Benutzer sie eintippt oder vergisst
            if (!name.ToLowerInvariant().EndsWith(".txt"))
                name += ".txt";

            string fullPath = Path.Combine(_layoutDir, name);

            if (File.Exists(fullPath))
            {
                MessageBox.Show("Datei existiert bereits.", "Hinweis",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                SpeichereFeldStrukturAlsTXT(fullPath); // direkt initial speichern
                PopulateSpeicherCombo();
                comboBox3_ansichtSpeichern.SelectedItem = Path.GetFileNameWithoutExtension(name); // ohne .txt anzeigen
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Anlegen:\n" + ex.Message,
                                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void neuToolStripMenuItem_Neu_Click(object sender, EventArgs e)
        {
            // 1. Abfrage: Speichern?
            DialogResult result = MessageBox.Show(
                "Möchten Sie das aktuelle Layout speichern, bevor ein neues begonnen wird?",
                "Layout speichern?",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
                return;

            if (result == DialogResult.Yes)
            {
                string name = comboBox3_ansichtSpeichern.SelectedItem as string;

                if (!string.IsNullOrEmpty(name))
                {
                    name = name.Trim();
                    if (!name.ToLowerInvariant().EndsWith(".txt"))
                        name += ".txt";

                    string fullPath = Path.Combine(_layoutDir, name);

                    try
                    {
                        SpeichereFeldStrukturAlsTXT(fullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Speichern fehlgeschlagen:\n" + ex.Message,
                                        "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Kein gültiger Dateiname zum Speichern vorhanden.",
                                    "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // 2. Layout zurücksetzen
            panel2.Controls.Clear();              // Alle Felder entfernen
            ClearSelection();                     // Auswahl und Rahmen löschen
            EnsureHighlightBorder();              // Rahmen sicherstellen
            highlightBorder.Visible = false;      // Sichtbarkeit zurücksetzen

            // 3. panel2-Größe auf Standardgröße setzen
            panel2.Width = 1140;
            panel2.Height = 313;

            panel1.Width = panel2.Width + 6;
            panel1.Height = panel2.Height + 6;
            panel1.Location = new Point(20, 20);

            // 4. Eingabesteuerelemente zurücksetzen
            ResetInputControls();

            // 5. Oberfläche synchronisieren
            toolStripTextBox1.Text = panel2.Width.ToString();
            toolStripTextBox2.Text = panel2.Height.ToString();
            comboBox1.SelectedIndex = -1;

            // Status und Scrollbereich aktualisieren
            scrollPaddingPanel.Location = new Point(panel1.Right + 20, panel1.Bottom + 20);
            toolStripStatusLabel2.Text = string.Format("panel2: {0} x {1}  panel1: {2} x {3}",
                panel2.Width, panel2.Height, panel1.Width, panel1.Height);

            // 6. Zeichenfläche anpassen
            //if (panel2.Controls.Count > 0)
            //    AutoResizeCanvas(panel2);
            ResizeFormToFitLayout();
            tabPage1.AutoScrollPosition = new Point(0, 0);

            // 7. Fenstertitel aktualisieren
            this.Text = "Grafikeditor – [Neues Layout]";

            toolStripStatusLabel1.Text = panel2.Width.ToString() + " " + panel2.Height.ToString() +
                                          "  " + panel1.Width.ToString() + " " + panel1.Height.ToString() +
                                          "  " + tabPage1.Width.ToString() + " " + tabPage1.Height.ToString();
        }
    }
}
