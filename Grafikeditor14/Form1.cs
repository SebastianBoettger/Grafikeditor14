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
using System.IO;

namespace Grafikeditor14
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }

        private readonly UndoRedoManager _undoMgr = new UndoRedoManager();
        private readonly EditorState _state = new EditorState();
        private readonly List<Control> _selection = new List<Control>();

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

            int offsetX = 2;
            int offsetY = 2;

            Rectangle gripRect = new Rectangle(
                this.ClientSize.Width - cGrip - offsetX,
                this.ClientSize.Height - cGrip - offsetY,
                cGrip,
                cGrip
            );

            ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, gripRect);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);

                Point pos = this.PointToClient(new Point(m.LParam.ToInt32() & 0xFFFF, m.LParam.ToInt32() >> 16));

                int offsetX = 2;
                int offsetY = 2;

                if (pos.X >= this.ClientSize.Width - cGrip - offsetX &&
                    pos.Y >= this.ClientSize.Height - cGrip - offsetY)
                {
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                    return;
                }

                return;
            }

            base.WndProc(ref m);
        }

        Panel scrollPaddingPanel;
        // -------------------------------------------------------- L O A D --------------------------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            scrollPaddingPanel = new Panel();
            scrollPaddingPanel.Size = new Size(1, 1); 
            scrollPaddingPanel.BackColor = Color.Transparent;
            tabPage1.Controls.Add(scrollPaddingPanel);

            CenterPanelInTabPage();
            toolStripDropDownButton2.DropDown.AutoClose = false;

            toolStripTextBox1.Text = panel2.Width.ToString();
            toolStripTextBox2.Text = panel2.Height.ToString();

            toolStripTextBox3.Text = _state.RasterAktiv.ToString();

            highlightBorder = new Panel
            {
                Size = new Size(0, 0),
                BackColor = Color.Transparent,
                Visible = false,
                Enabled = false
            };
            highlightBorder.Paint += (s, pe) =>
            {
                using (Pen p = new Pen(Color.Red, 2))            // 2-Pixel-Rand
                    pe.Graphics.DrawRectangle(
                        p,                                       // Stift
                        0, 0,                                    // links-oben
                        highlightBorder.Width - 1,              // Breite
                        highlightBorder.Height - 1);             // Höhe
            };
            panel2.Controls.Add(highlightBorder);
            highlightBorder.SendToBack();

            toolStripStatusLabel1.Text = panel1.Location.X.ToString() + " " + panel1.Location.Y.ToString() + "  " + panel1.Width.ToString() + " " + panel1.Height.ToString();
        }
        // -------------------------------------------------------------------------------------------------------------------------------------------------------------------

        #endregion

        #region Kopfzeile Funktion
        
        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Anzeigezeile
        
        private void CenterPanelInTabPage()
        {
            if (tabPage1 == null || panel1 == null)
                return;

            int x, y;

            bool horizontalScrollVisible = tabPage1.HorizontalScroll.Visible;

            if (horizontalScrollVisible)
            {
                x = 0;
            }
            else
            {
                x = (tabPage1.ClientSize.Width - panel1.Width) / 2;
            }

            bool verticalScrollVisible = tabPage1.VerticalScroll.Visible;

            if (verticalScrollVisible)
            {
                y = 0;
            }
            else
            {
                y = (tabPage1.ClientSize.Height - panel1.Height) / 2;
            }

            panel1.Location = new Point(x, y);
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

        private readonly int minFormHeight = 1000;
        private readonly int safetyMargin = 5;
        private void panel2_SizeChanged(object sender, EventArgs e)
        {
            panel1.Width = panel2.Width + 6;
            panel1.Height = panel2.Height + 6;
            panel1.Location = new Point(20, 20);

            if (zwischenspeicher_differenz[1] > 0 && panel2.Height >= 313)
            {
                int uiOverhead = zwischenspeicher_differenz[1];

                int desiredHeight = this.Height + uiOverhead;
                Screen currentScreen = Screen.FromHandle(this.Handle);
                Rectangle workingArea = currentScreen.WorkingArea;
                int screenMaxHeight = workingArea.Height - (2 * safetyMargin);

                int newFormHeight = Math.Min(Math.Max(desiredHeight, minFormHeight), screenMaxHeight);
                this.Height = newFormHeight;

                int maxHeightTableLayoutPanel1Row2 = screenMaxHeight - 598;
                
                float currentHeightOfTableLayoutPanel1Row2 = tableLayoutPanel1.RowStyles[2].Height;
                tableLayoutPanel1.RowStyles[2].Height = Math.Min((currentHeightOfTableLayoutPanel1Row2 + (float)uiOverhead), maxHeightTableLayoutPanel1Row2);

                int heightForTabControl = tabControl1.Height + uiOverhead;
                tabControl1.Height = heightForTabControl;

                int newX = (workingArea.Width - this.Width) / 2;
                int newY = (workingArea.Height - this.Height) / 2;
                this.Location = new Point(newX, newY);
                panel1.Location = new Point(20, 20);
            }

            if (zwischenspeicher_differenz[1] < 0 && panel2.Height >= 313)
            {
                if (tabPage1.Height > panel1.Height - 40)
                {
                    int difference = tabPage1.Height - (panel1.Height - 40);
                    this.Height = this.Height - difference;
                    tableLayoutPanel1.RowStyles[2].Height = tableLayoutPanel1.RowStyles[2].Height - difference;
                    tabControl1.Height = tabControl1.Height - difference;
                    tabPage1.Height = tabPage1.Height - difference;

                    // Fenster zentrieren
                    Screen currentScreen = Screen.FromHandle(this.Handle);
                    Rectangle workingArea = currentScreen.WorkingArea;
                    int newX = (workingArea.Width - this.Width) / 2;
                    int newY = (workingArea.Height - this.Height) / 2;
                    this.Location = new Point(newX, newY);
                    panel1.Location = new Point(20, 20);
                }

            }

            zwischenspeicher_differenz.Clear();
            CenterPanelInTabPage();
            toolStripStatusLabel2.Text = "this.Height = " + this.Height + " tabControl1.Height = " + tabControl1.Height.ToString();

            // ---------------------------------!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!-----------------------------------hier wenn scrollbar sichtbar ... !!!!!!!!!!------------------------
            scrollPaddingPanel.Location = new Point(
                panel1.Right + 20,
                panel1.Bottom + 20
            );
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

        private void toolStripButton_NeuesFeldErzeugen_Click(object sender, EventArgs e)
        {
            Label newLabel = new Label();
            string dsMerkmal = (comboBox1.SelectedItem != null)
                   ? comboBox1.SelectedItem.ToString()
                   : "";

            string labelText = string.IsNullOrWhiteSpace(richTextBox1.Text)
                       ? "Neues Label"
                       : richTextBox1.Text;

            newLabel.BackColor = SystemColors.Control;
            newLabel.Font = new Font("Arial", 12f, FontStyle.Bold);
            newLabel.Text = labelText;
            newLabel.TextAlign = currentAlignment;
            newLabel.ForeColor = Color.Black;
            newLabel.AutoSize = false;

            Size txtSize = TextRenderer.MeasureText(labelText, newLabel.Font);
            int padding = 20;
            int minW = 110;
            newLabel.Width = Math.Max(txtSize.Width + padding, minW);
            newLabel.Height = 25;

            newLabel.Location = new Point(
                panel2.Width / 2 - newLabel.Width / 2,
                panel2.Height / 2 - newLabel.Height / 2);

            newLabel.BorderStyle = BorderStyle.FixedSingle;
            newLabel.Name = GeneriereNeuenFeldnamen();

            newLabel.Tag = BuildTagArray(newLabel, dsMerkmal);

            newLabel.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
            newLabel.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
            newLabel.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);
            newLabel.Click += NeuesLabel_Click;

            panel2.Controls.Add(newLabel);
            ClearSelection();
            AddToSelection(newLabel);

            highlightBorder.SendToBack();
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

            _state.ActiveControl = _selection[0];         // Lead synchron halten

            if (e.Button != MouseButtons.Left) return;

            Control lead = _selection[0];
            lead.Left = e.X + lead.Left - _mouseDownLocationL.X;
            lead.Top = e.Y + lead.Top - _mouseDownLocationL.Y;

            if (highlightBorder.Visible)
            {
                highlightBorder.Bounds = new Rectangle(
                    lead.Left - 2, lead.Top - 2, lead.Width + 4, lead.Height + 4);
            }
        }

        private void FeldInPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_selection.Count == 0 || e.Button != MouseButtons.Left) return;

            _state.ActiveControl = _selection[0];
            Control lead = _selection[0];

            Point snapped = SnapToGrid(lead.Location);
            _undoMgr.Do(new MoveCommand(lead, snapped));
            lead.Location = snapped;

            highlightBorder.Bounds = new Rectangle(
                lead.Left - 2, lead.Top - 2, lead.Width + 4, lead.Height + 4);
            highlightBorder.SendToBack();
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
            if (_state.ActiveControl == null)
                return;

            if (_state.ActiveControl is Label && _state.ActiveControl.Parent == panel2)
            {
                Panel panel = new Panel
                {
                    Location = _state.ActiveControl.Location,
                    Size = _state.ActiveControl.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.LightGray,
                    Name = _state.ActiveControl.Name
                };

                panel.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
                panel.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
                panel.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);

                panel2.Controls.Add(panel);
                panel2.Controls.Remove(_state.ActiveControl);
                _state.ActiveControl.Dispose();
                ClearSelection();
                AddToSelection(panel);
            }
            
            else if (_state.ActiveControl is Panel && _state.ActiveControl.Parent == panel2)
            {
                Label label = new Label
                {
                    Location = _state.ActiveControl.Location,
                    Size = _state.ActiveControl.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = SystemColors.Control,
                    AutoSize = false,
                    Name = _state.ActiveControl.Name,
                    TextAlign = currentAlignment
                };

                label.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
                label.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
                label.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);

                panel2.Controls.Add(label);
                panel2.Controls.Remove(_state.ActiveControl);
                _state.ActiveControl.Dispose();
                ClearSelection();
                AddToSelection(label);

            }
        }

        private void NeuesLabel_Click(object sender, EventArgs e)
        {
            ClearSelection();
            AddToSelection(sender as Control);

            DisplayFieldProperties(_state.ActiveControl);
        }

        private Panel highlightBorder;
        private void ZeigeHighlightUm(Control ctrl)
        {
            if (ctrl == null || ctrl.Parent != panel2)
            {
                highlightBorder.Visible = false;
                return;
            }

            highlightBorder.Bounds = new Rectangle(
                ctrl.Left - 2,
                ctrl.Top - 2,
                ctrl.Width + 4,
                ctrl.Height + 4
            );
            highlightBorder.Visible = true;
            highlightBorder.BringToFront();
            ctrl.BringToFront();
        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (panel2.GetChildAtPoint(e.Location) == null)   // Klick ins Leere
                ClearSelection();
        }

        private string GeneriereNeuenFeldnamen()
        {
            int maxNummer = 0;

            foreach (Control ctrl in panel2.Controls)
            {
                if (ctrl.Name.StartsWith("Feld"))
                {
                    string nummerTeil = ctrl.Name.Substring(4);
                    int nummer;
                    if (int.TryParse(nummerTeil, out nummer))
                    {
                        if (nummer > maxNummer)
                            maxNummer = nummer;
                    }
                }
            }

            return "Feld" + (maxNummer + 1);
        }

        private void DupliziereFeld(Control original)
        {
            if (original == null || original.Parent != panel2) return;

            Control kopie;

            if (original is Label)
            {
                Label l = original as Label;
                Label neu = new Label
                {
                    Text = l.Text,
                    Font = l.Font,
                    Size = l.Size,
                    BackColor = l.BackColor,
                    BorderStyle = l.BorderStyle,
                    ForeColor = l.ForeColor,
                    TextAlign = l.TextAlign,
                    AutoSize = false
                };
                kopie = neu;
            }
            else if (original is Panel)
            {
                Panel p = original as Panel;
                Panel neu = new Panel
                {
                    Size = p.Size,
                    BackColor = p.BackColor,
                    BorderStyle = p.BorderStyle
                };
                kopie = neu;
            }
            else
            {
                return;
            }

            kopie.Name = GeneriereNeuenFeldnamen();
            kopie.Location = new Point(original.Left + 10, original.Top + 10);

            kopie.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
            kopie.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
            kopie.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);
            kopie.Click += NeuesLabel_Click;

            panel2.Controls.Add(kopie);
            kopie.BringToFront();
            kopie.Focus();

            ClearSelection();
            AddToSelection(kopie);
        }

        private ContentAlignment currentAlignment = ContentAlignment.MiddleCenter;
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                currentAlignment = ContentAlignment.MiddleLeft;
            else if (radioButton2.Checked)
                currentAlignment = ContentAlignment.MiddleCenter;
            else if (radioButton3.Checked)
                currentAlignment = ContentAlignment.MiddleRight;

            Label lbl = _state.ActiveControl as Label;
            if (lbl != null)
                lbl.TextAlign = currentAlignment;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_state.ActiveControl == null || comboBox1.SelectedItem == null)
                return;

            string auftragsMerkmal = comboBox1.SelectedItem.ToString();

            foreach (Control ctrl in panel2.Controls)
            {
                string[] tagArray = ctrl.Tag as string[];
                if (tagArray != null && tagArray.Length == 15 && tagArray[14] == "DSFeldname=" + auftragsMerkmal)
                {
                    MessageBox.Show(
                        "Das Auftragsmerkmal \"" + auftragsMerkmal + "\" ist bereits dem Feld \"" +
                        ctrl.Name + "\" zugeordnet.",
                        "Hinweis",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
            }

            int alignVal = 32;
            Label lbl = _state.ActiveControl as Label;
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignVal = 16;
                else if (lbl.TextAlign == ContentAlignment.MiddleRight) alignVal = 48;
            }

            int textColor = (lbl != null) ? lbl.ForeColor.ToArgb() : 0;
            float fontSize = (lbl != null) ? lbl.Font.Size : 0f;
            string fontName = (lbl != null) ? lbl.Font.Name : "";
            int fontStyle = (lbl != null) ? (int)lbl.Font.Style : 0;
            string textVal = (lbl != null) ? lbl.Text : "";

            string[] tagData = new string[15];
            tagData[0] = "Alignment=" + alignVal;
            tagData[1] = "PosY=" + _state.ActiveControl.Top;
            tagData[2] = "Fontgröße=" + fontSize;
            tagData[3] = "Text=" + textVal;
            tagData[4] = "Füllzeichen=";                       // (kommt später)
            tagData[5] = "Höhe=" + _state.ActiveControl.Height;
            tagData[6] = "Textfarbe=" + textColor;
            tagData[7] = "Stellen=0";
            tagData[8] = "PosX=" + _state.ActiveControl.Left;
            tagData[9] = "FeldName=" + _state.ActiveControl.Name;
            tagData[10] = "FontName=" + fontName;
            tagData[11] = "Breite=" + _state.ActiveControl.Width;
            tagData[12] = "Fontstyle=" + fontStyle;
            tagData[13] = "Farbe=" + _state.ActiveControl.BackColor.ToArgb();
            tagData[14] = "DSFeldname=" + auftragsMerkmal;

            _state.ActiveControl.Tag = BuildTagArray(_state.ActiveControl, auftragsMerkmal);

            DisplayFieldProperties(_state.ActiveControl);
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
            int alignVal = 32;
            Label lbl = ctrl as Label;
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignVal = 16;
                else if (lbl.TextAlign == ContentAlignment.MiddleRight) alignVal = 48;
            }

            int textColor = (lbl != null) ? lbl.ForeColor.ToArgb() : 0;
            float fontSize = (lbl != null) ? lbl.Font.Size : 0f;
            string fontName = (lbl != null) ? lbl.Font.Name : "";
            int fontStyle = (lbl != null) ? (int)lbl.Font.Style : 0;
            string textVal = (lbl != null) ? lbl.Text : "";

            string[] tagData = new string[15];
            tagData[0] = "Alignment=" + alignVal;
            tagData[1] = "PosY=" + ctrl.Top;
            tagData[2] = "Fontgröße=" + fontSize;
            tagData[3] = "Text=" + textVal;
            tagData[4] = "Füllzeichen=";
            tagData[5] = "Höhe=" + ctrl.Height;
            tagData[6] = "Textfarbe=" + textColor;
            tagData[7] = "Stellen=0";
            tagData[8] = "PosX=" + ctrl.Left;
            tagData[9] = "FeldName=" + ctrl.Name;
            tagData[10] = "FontName=" + fontName;
            tagData[11] = "Breite=" + ctrl.Width;
            tagData[12] = "Fontstyle=" + fontStyle;
            tagData[13] = "Farbe=" + ctrl.BackColor.ToArgb();
            tagData[14] = "DSFeldname=" + auftragsMerkmal;

            return tagData;
        }

        private void FeldInPanel_DoubleClick(object sender, EventArgs e)
        {
            Control clicked = sender as Control;

            if (clicked != null && clicked == _state.ActiveControl)
            {
                _state.ActiveControl = null;
                highlightBorder.Visible = false;
                richTextBox7.Clear();
                toolStripStatusLabel2.Text = "";
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_selection.Count == 0) return base.ProcessCmdKey(ref msg, keyData);

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

        private void VerarbeiteResize(int dxSign, int dySign)
        {
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

            foreach (Control c in _selection)
            {
                Size ziel = new Size(
                    Math.Max(5, c.Width + dxSign * step),
                    Math.Max(5, c.Height + dySign * step));

                _undoMgr.Do(new ResizeCommand(c, ziel));
            }
            RefreshHighlight();
        }

        private void VerarbeiteMove(int dxSign, int dySign)
        {
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

            foreach (Control c in _selection)
            {
                Point ziel = new Point(c.Left + dxSign * step,
                                       c.Top + dySign * step);

                if (_state.RasterAktiv)
                    ziel = SnapToGrid(ziel);

                _undoMgr.Do(new MoveCommand(c, ziel));
            }
            RefreshHighlight();
        }

        private void SetActive(Control ctrl)
        {
            ClearSelection();                // vorherige Auswahl komplett löschen
            if (ctrl != null)
                AddToSelection(ctrl);        // löst ActiveControl + Highlight aus
        }

        private void ClearSelection()
        {
            _selection.Clear();
            _state.ActiveControl = null;     // wichtig für Pfeiltasten-Routing
            highlightBorder.Visible = false;
            richTextBox7.Clear();
        }

        private void RemoveFromSelection(Control ctrl)
        {
            _selection.Remove(ctrl);
            if (_selection.Count == 0)
                ClearSelection();
            else
            {
                RefreshHighlight();
                if (_selection.Count == 1)
                    DisplayFieldProperties(_selection[0]);
                else
                    richTextBox7.Clear();
            }
        }

        private void RefreshHighlight()
        {
            if (_selection.Count == 0)
            {
                highlightBorder.Visible = false;
                return;
            }

            Rectangle r = _selection[0].Bounds;
            foreach (Control c in _selection.Skip(1))
                r = Rectangle.Union(r, c.Bounds);

            // exakt 2-px Abstand rundum
            highlightBorder.Bounds = new Rectangle(
                r.Left - 2,
                r.Top - 2,
                r.Width + 4,
                r.Height + 4);

            highlightBorder.Visible = true;
            highlightBorder.SendToBack();
        }

        /// <summary>
        /// Liest aus dem Tag-Array (Index 14) den Eintrag „DSFeldname=…“
        /// und wählt – falls gefunden – den Eintrag in comboBox1.
        /// Kompatibel mit .NET 4.0 (ohne Null-Conditional / Pattern-Matching).
        /// </summary>
        private void SyncComboBoxWithField(Control ctrl)
        {
            if (ctrl == null) return;

            string feldname = null;

            // Variante 1: Feld.Tag ist ein string[]
            string[] tagArr = ctrl.Tag as string[];
            if (tagArr != null && tagArr.Length > 14)
            {
                feldname = ExtractFieldName(tagArr[14]);
            }
            else
            {
                // Variante 2: Feld.Tag ist ein einzelner String
                string tagStr = ctrl.Tag as string;
                if (!string.IsNullOrEmpty(tagStr))
                {
                    string[] parts = tagStr.Split(
                        new[] { ';', ',', '|' },
                        StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 14)
                        feldname = ExtractFieldName(parts[14]);
                    else
                        feldname = ExtractFieldName(tagStr);
                }
            }

            if (!string.IsNullOrEmpty(feldname))
            {
                int idx = comboBox1.FindStringExact(feldname); // -1, falls nicht gefunden
                comboBox1.SelectedIndex = idx;
            }
        }

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

        private void AddToSelection(Control ctrl)
        {
            if (!_selection.Contains(ctrl))
                _selection.Add(ctrl);

            _state.ActiveControl = ctrl;
            RefreshHighlight();

            if (_selection.Count == 1)
            {
                DisplayFieldProperties(ctrl);
                SyncComboBoxWithField(ctrl);   //  <<< NEU >>>
            }
            else
            {
                richTextBox7.Clear();
            }
        }

        private void Field_Click(object sender, EventArgs e)
        {
            Control field = sender as Control;
            if (field == null) return;

            SyncComboBoxWithField(field);   // erledigt alles Weitere
        }

        private void SpeichereFeldStrukturAlsTXT(string pfad)
        {
            using (StreamWriter writer = new StreamWriter(pfad, false, Encoding.UTF8))
            {
                int index = 0;
                foreach (Control ctrl in panel2.Controls)
                {
                    string[] tagArr = ctrl.Tag as string[];
                    if (tagArr == null || tagArr.Length < 15) continue;

                    writer.WriteLine("[{0}]", index);
                    foreach (string zeile in tagArr)
                    {
                        writer.WriteLine(zeile);
                    }
                    writer.WriteLine(); // Leerzeile zwischen Einträgen
                    index++;
                }
            }
        }

        private void sC_B_Speichern_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "TXT-Dateien (*.txt)|*.txt";
            sfd.FileName = "ExportFeldstruktur.txt";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                SpeichereFeldStrukturAlsTXT(sfd.FileName);
                MessageBox.Show("Datei wurde erfolgreich gespeichert.", "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private enum Arbeitsmodus
        {
            Erzeugen,
            Bearbeiten
        }
    }
}
