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

        Panel resizeOverlay;

        private void ResizeOverlay_Paint(object sender, PaintEventArgs e)
        {
            Rectangle gripRect = new Rectangle(resizeOverlay.Width - cGrip, resizeOverlay.Height - cGrip, cGrip, cGrip);
            ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, gripRect);
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

            toolStripTextBox3.Text = rasterAbstand.ToString();

            highlightBorder = new Panel
            {
                Size = new Size(0, 0),
                BackColor = Color.Red,
                Visible = false,
                Enabled = false
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
            rasterAbstand = Convert.ToInt32(toolStripTextBox3.Text);
            if (rasterActive == true)
            {
                RedrawRaster(rasterFarbe);
            }
        }

        private bool rasterActive;
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.DropDown.AutoClose = false;
            rasterActive = true;
            using (Graphics g = panel2.CreateGraphics())
            {
                DrawPoints(g, rasterFarbe, rasterAbstand);
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.DropDown.AutoClose = false;
            rasterActive = false;
            ClearRasterOnPanel();
        }

        private List<Point> rasterPoints = new List<Point>();
        private int rasterAbstand = 10;
        private Color rasterFarbe = Color.White;
        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            if (rasterActive == true)
            {
                using (Graphics g = panel2.CreateGraphics())
                {
                    DrawPoints(g, rasterFarbe, rasterAbstand);
                }
            }
        }

        private void DrawPoints(Graphics g, Color color, int pixelSize)
        {
            SolidBrush brush = new SolidBrush(color);
            rasterPoints.Clear();
            for (int x = 0; x < panel2.Width; x += pixelSize)
            {
                for (int y = 0; y < panel2.Height; y += pixelSize)
                {
                    g.FillRectangle(brush, x, y, 1, 1);
                    rasterPoints.Add(new Point(x, y));
                }
            }
        }

        private void ClearRasterOnPanel()
        {
            panel2.Invalidate();
            panel2.Update();
        }

        private void RedrawRaster(Color color)
        {
            ClearRasterOnPanel();
            using (Graphics g = panel2.CreateGraphics())
            {
                SolidBrush brush = new SolidBrush(color);
                foreach (Point point in rasterPoints)
                {
                    g.FillRectangle(brush, point.X, point.Y, 1, 1);
                }
            }
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

            _state.ActiveControl = _selection[0];

            if (e.Button != MouseButtons.Left || _selection[0] == null)
                return;

            _selection[0].Left = e.X + _selection[0].Left - _mouseDownLocationL.X;
            _selection[0].Top = e.Y + _selection[0].Top - _mouseDownLocationL.Y;

            if (highlightBorder.Visible)
            {
                highlightBorder.Bounds = new Rectangle(
                    _selection[0].Left - 2,
                    _selection[0].Top - 2,
                    _selection[0].Width + 4,
                    _selection[0].Height + 4
                );
            }
        }

        private void FeldInPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_selection.Count == 0) return;

            if (e.Button != MouseButtons.Left || _selection[0] == null)
                return;

            Point snapped = SnapToGrid(_selection[0].Location);
            var cmd = new MoveCommand(_selection[0], snapped);
            _undoMgr.Do(cmd);
            _selection[0].Location = snapped;

            highlightBorder.Bounds = new Rectangle(
                _selection[0].Left - 2,
                _selection[0].Top - 2,
                _selection[0].Width + 4,
                _selection[0].Height + 4);
            highlightBorder.SendToBack();

            Control clicked = sender as Control;
            
            int dx = Math.Abs(e.Location.X - _mouseDownLocationL.X);
            int dy = Math.Abs(e.Location.Y - _mouseDownLocationL.Y);

            bool isSimpleClick = dx < SystemInformation.DoubleClickSize.Width / 2 &&
                                 dy < SystemInformation.DoubleClickSize.Height / 2;
            _state.ActiveControl = _selection[0];
        }

        private Point SnapToGrid(Point position)
        {
            if (rasterActive)
            {
                int x = (position.X + rasterAbstand / 2) / rasterAbstand * rasterAbstand;
                int y = (position.Y + rasterAbstand / 2) / rasterAbstand * rasterAbstand;

                return new Point(x, y);
            }
            else
            {
                return new Point(position.X, position.Y);
            }
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
            if (panel2.GetChildAtPoint(e.Location) == null)
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

            MessageBox.Show(
                "Auftragsmerkmal \"" + auftragsMerkmal +
                "\" wurde Feld \"" + _state.ActiveControl.Name + "\" zugeordnet.",
                "Erfolgreich",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

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
            Control c = _state.ActiveControl;
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

            Size ziel = new Size(
                Math.Max(5, c.Width + dxSign * step),
                Math.Max(5, c.Height + dySign * step));

            _undoMgr.Do(new ResizeCommand(c, ziel));
            ZeigeHighlightUm(c);
        }

        private void VerarbeiteMove(int dxSign, int dySign)
        {
            Control c = _state.ActiveControl;
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

            Point ziel = new Point(c.Left + dxSign * step,
                                   c.Top + dySign * step);

            if (_state.RasterAktiv)
                ziel = SnapToGrid(ziel);

            _undoMgr.Do(new MoveCommand(c, ziel));
            ZeigeHighlightUm(c);
        }

        private void SetActive(Control ctrl)
        {
            ClearSelection();
            if (ctrl != null) AddToSelection(ctrl);
        }

        private void ClearSelection()
        {
            _selection.Clear();
            _state.ActiveControl = null;
            highlightBorder.Visible = false;
            richTextBox7.Clear();
        }

        private void AddToSelection(Control ctrl)
        {
            if (!_selection.Contains(ctrl))
                _selection.Add(ctrl);
            _state.ActiveControl = ctrl;
            RefreshHighlight();
            if (_selection.Count == 1)
                DisplayFieldProperties(ctrl);
            else
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
            if (_selection.Count == 0) { highlightBorder.Visible = false; return; }

            Rectangle r = _selection[0].Bounds;
            foreach (Control c in _selection.Skip(1))
                r = Rectangle.Union(r, c.Bounds);

            highlightBorder.Bounds = new Rectangle(r.Left - 2, r.Top - 2, r.Width + 4, r.Height + 4);
            highlightBorder.Visible = true;
            highlightBorder.BringToFront();
        }

        private void tSB_Auswahlen_off_Click(object sender, EventArgs e)
        {
            ClearSelection();
        }
    }
}
