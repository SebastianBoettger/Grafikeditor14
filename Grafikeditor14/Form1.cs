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

        #region allgemeine Funktion
        // Fenster bewegen
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

        // Resize-Griff zeichnen
        Panel resizeOverlay;

        private void InitializeResizeOverlay()
        {
            resizeOverlay = new Panel();
            resizeOverlay.Width = 20;
            resizeOverlay.Height = 20;
            resizeOverlay.BackColor = Color.Transparent;
            resizeOverlay.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            resizeOverlay.Location = new Point(this.ClientSize.Width - resizeOverlay.Width, this.ClientSize.Height - resizeOverlay.Height);
            resizeOverlay.Paint += ResizeOverlay_Paint;
            resizeOverlay.Cursor = Cursors.SizeNWSE;
            resizeOverlay.MouseDown += ResizeOverlay_MouseDown;
            resizeOverlay.MouseMove += ResizeOverlay_MouseMove;
            resizeOverlay.MouseUp += ResizeOverlay_MouseUp;

            this.Controls.Add(resizeOverlay);
            resizeOverlay.BringToFront();
        }

        private void ResizeOverlay_Paint(object sender, PaintEventArgs e)
        {
            Rectangle gripRect = new Rectangle(resizeOverlay.Width - cGrip, resizeOverlay.Height - cGrip, cGrip, cGrip);
            ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, gripRect);
        }

        private bool resizing = false;
        private Point lastMouse;

        private void ResizeOverlay_MouseDown(object sender, MouseEventArgs e)
        {
            resizing = true;
            lastMouse = Cursor.Position;
        }

        private void ResizeOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                Point current = Cursor.Position;
                int dx = current.X - lastMouse.X;
                int dy = current.Y - lastMouse.Y;

                this.Width += dx;
                this.Height += dy;

                lastMouse = current;
            }
            CenterPanelInTabPage();
        }

        private void ResizeOverlay_MouseUp(object sender, MouseEventArgs e)
        {
            resizing = false;
        }

        // Fenstergröße anpassen
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

                // Gleicher Offset wie oben
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

            InitializeResizeOverlay();
            CenterPanelInTabPage();
            toolStripDropDownButton2.DropDown.AutoClose = false;

            toolStripTextBox1.Text = panel2.Width.ToString();
            toolStripTextBox2.Text = panel2.Height.ToString();

            toolStripTextBox3.Text = rasterAbstand.ToString();

            // Roter Rahmen zur Hervorhebung vorbereiten
            highlightBorder = new Panel
            {
                Size = new Size(0, 0),
                BackColor = Color.Red,
                Visible = false,
                Enabled = false // verhindert Interaktionen
            };
            panel2.Controls.Add(highlightBorder);
            highlightBorder.SendToBack(); // Damit es unter dem aktiven Feld liegt

            toolStripStatusLabel1.Text = panel1.Location.X.ToString() + " " + panel1.Location.Y.ToString() + "  " + panel1.Width.ToString() + " " + panel1.Height.ToString();
        }
        // -------------------------------------------------------------------------------------------------------------------------------------------------------------------

        #endregion

        #region Kopfzeile Funktion
        // Datei Beenden
        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Anzeigezeile
        // Zentrieren horizontal und vertikal von panel1 in tabPage1
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

        // Panelgröße
        List<int> zwischenspeicher_differenz = new List<int>();
        private void toolStripMenuItemOK_Panelgröße_Click(object sender, EventArgs e)
        {
            zwischenspeicher_differenz.Add(Convert.ToInt32(toolStripTextBox1.Text) - panel2.Width);
            zwischenspeicher_differenz.Add(Convert.ToInt32(toolStripTextBox2.Text) - panel2.Height);
            panel2.Width = Convert.ToInt32(toolStripTextBox1.Text);
            panel2.Height = Convert.ToInt32(toolStripTextBox2.Text);
        }

        private readonly int minFormHeight = 1000; // oder: this.Height beim Start merken
        private readonly int safetyMargin = 5;
        private void panel2_SizeChanged(object sender, EventArgs e)
        {
            panel1.Width = panel2.Width + 6;
            panel1.Height = panel2.Height + 6;
            panel1.Location = new Point(20, 20);

            if (zwischenspeicher_differenz[1] > 0 && panel2.Height >= 313)
            {
                // Höhe berechnen (inkl. Menü, ToolStrip, StatusStrip und Puffersumme)
                int uiOverhead = zwischenspeicher_differenz[1];

                int desiredHeight = this.Height + uiOverhead;
                Screen currentScreen = Screen.FromHandle(this.Handle);
                Rectangle workingArea = currentScreen.WorkingArea;
                int screenMaxHeight = workingArea.Height - (2 * safetyMargin);

                // Begrenzung zwischen Mindesthöhe und Bildschirmhöhe
                int newFormHeight = Math.Min(Math.Max(desiredHeight, minFormHeight), screenMaxHeight);
                this.Height = newFormHeight;

                // 3 tableLayputPanel1-Row2-Höhe anpassen
                    // 3.1 Begrenzung für tableLayputPanel1-Row2-Höhe
                // 3.1
                int maxHeightTableLayoutPanel1Row2 = screenMaxHeight - 598;
                // 3
                float currentHeightOfTableLayoutPanel1Row2 = tableLayoutPanel1.RowStyles[2].Height;
                tableLayoutPanel1.RowStyles[2].Height = Math.Min((currentHeightOfTableLayoutPanel1Row2 + (float)uiOverhead), maxHeightTableLayoutPanel1Row2);

                // tabControl-Höhe anpassen
                int heightForTabControl = tabControl1.Height + uiOverhead;
                tabControl1.Height = heightForTabControl;

                // Fenster zentrieren
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
            // scrollPaddingPanel „hinter“ panel1 positionieren (für mehr Scrollfläche)
            scrollPaddingPanel.Location = new Point(
                panel1.Right + 20,  // 20 px weiter rechts
                panel1.Bottom + 20  // 20 px weiter unten
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

        // Neues Feld - Label erzeugen
        Label newLabel = new Label();
        private void toolStripButton_NeuesFeldErzeugen_Click(object sender, EventArgs e)
        {
            string dsMerkmal = (comboBox1.SelectedItem != null)
                   ? comboBox1.SelectedItem.ToString()
                   : "";

            string labelText = string.IsNullOrWhiteSpace(richTextBox1.Text)
                       ? "Neues Label"
                       : richTextBox1.Text;

            newLabel = new Label();
            newLabel.BackColor = SystemColors.Control;
            newLabel.Font = new Font("Arial", 12f, FontStyle.Bold);
            newLabel.Text = labelText;
            newLabel.TextAlign = currentAlignment;
            newLabel.ForeColor = Color.Black;
            newLabel.AutoSize = false;

            /* NEU: Breite auf Textlänge anpassen */
            Size txtSize = TextRenderer.MeasureText(labelText, newLabel.Font);
            int padding = 20;                       // kleiner Puffer links/rechts
            int minW = 110;                      // Mindestbreite wie bisher
            newLabel.Width = Math.Max(txtSize.Width + padding, minW);
            newLabel.Height = 25;                    // Höhe bleibt unverändert

            // Position zentrieren (jetzt mit variabler Breite)
            newLabel.Location = new Point(
                panel2.Width / 2 - newLabel.Width / 2,
                panel2.Height / 2 - newLabel.Height / 2);

            newLabel.BorderStyle = BorderStyle.FixedSingle;
            newLabel.Name = GeneriereNeuenFeldnamen();

            // Name zuweisen
            newLabel.Name = GeneriereNeuenFeldnamen();

            newLabel.Tag = BuildTagArray(newLabel, dsMerkmal);   // Tag sofort setzen
            DisplayFieldProperties(newLabel);                    // sofort in richTextBox7

            newLabel.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
            newLabel.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
            newLabel.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);
            //newLabel.PreviewKeyDown += new PreviewKeyDownEventHandler(FeldInPanel_PreviewKeyDown);
            newLabel.Click += NeuesLabel_Click;

            panel2.Controls.Add(newLabel);
            activeControl = newLabel;
            newLabel.Focus();
            activeControl.BringToFront();

            ZeigeHighlightUm(activeControl);
            highlightBorder.SendToBack();

            DisplayFieldProperties(activeControl);
        }

        // Variablen für Drag & Drop
        private Point _mouseDownLocationL;

        // Ereignishandler für das Drücken der Maustaste
        private Control activeControl;
        private void FeldInPanel_MouseDown(object sender, MouseEventArgs e)
        {
            ///* Strg-Klick zum Duplizieren bleibt unverändert */
            //if (e.Button == MouseButtons.Left && Control.ModifierKeys.HasFlag(Keys.Control))
            //{
            //    DupliziereFeld(sender as Control);
            //    return;
            //}

            //if (e.Button == MouseButtons.Left)
            //{
            //    _mouseDownLocationL = e.Location;      // Position für Bewegungsprüfung
            //    activeControl = sender as Control;
            //    activeControl.Focus();

            //    ZeigeHighlightUm(activeControl);
            //    highlightBorder.SendToBack();
            //    toolStripStatusLabel2.Text = activeControl.Name;

            //    DisplayFieldProperties(activeControl); // Eigenschaften anzeigen
            //}

            if (e.Button == MouseButtons.Left)
            {
                _state.ActiveControl = sender as Control;  // ← Setzen
                ZeigeHighlightUm(_state.ActiveControl);
            }
        }

        // Ereignishandler für das Bewegen der Maus
        private void FeldInPanel_MouseMove(object sender, MouseEventArgs e)
        {
            _state.ActiveControl = sender as Control;

            if (e.Button != MouseButtons.Left || activeControl == null)
                return;

            activeControl.Left = e.X + activeControl.Left - _mouseDownLocationL.X;
            activeControl.Top = e.Y + activeControl.Top - _mouseDownLocationL.Y;

            // Rote Umrandung synchronisieren, wenn sichtbar
            if (highlightBorder.Visible && activeControl != null)
            {
                highlightBorder.Bounds = new Rectangle(
                    activeControl.Left - 2,
                    activeControl.Top - 2,
                    activeControl.Width + 4,
                    activeControl.Height + 4
                );
            }
        }

        private void FeldInPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || activeControl == null)
                return;

            /* 1) Raster-Snapping wie bisher */
            //Point snapped = SnapToGrid(activeControl.Location);
            Point snapped = SnapToGrid(activeControl.Location);
            var cmd = new MoveCommand(activeControl, snapped);
            _undoMgr.Do(cmd);
            activeControl.Location = snapped;

            highlightBorder.Bounds = new Rectangle(
                activeControl.Left - 2,
                activeControl.Top - 2,
                activeControl.Width + 4,
                activeControl.Height + 4);
            highlightBorder.SendToBack();

            /* 2) NEU: Abwählen bei einfachem Klick (MouseUp) auf bereits markiertes Feld */
            Control clicked = sender as Control;

            // Bewegungstoleranz (< Hälfte der DoubleClickSize)
            int dx = Math.Abs(e.Location.X - _mouseDownLocationL.X);
            int dy = Math.Abs(e.Location.Y - _mouseDownLocationL.Y);
            bool isSimpleClick = dx < SystemInformation.DoubleClickSize.Width / 2 &&
                                 dy < SystemInformation.DoubleClickSize.Height / 2;

            if (clicked == activeControl && isSimpleClick)
            {
                activeControl = null;            // Auswahl aufheben
                highlightBorder.Visible = false; // roten Rahmen verbergen
                richTextBox7.Clear();            // Eigenschaftenanzeige leeren
                toolStripStatusLabel2.Text = ""; // Status zurücksetzen
            }
        }

        //private void FeldInPanel_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        //{
        //    if (e.Control && !isCtrlPressed)
        //    {
        //        isCtrlPressed = true;
        //        //StartResizeTimer();
        //    }
        //}

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

        //private bool isCtrlPressed = false;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                //isCtrlPressed = true;
                if (activeControl != null)
                {
                    //StartResizeTimer();
                }
            }
        }

        //private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        //{
        //    if (e.Control && !isCtrlPressed)
        //    {
        //        isCtrlPressed = true;
        //        //StartResizeTimer();
        //    }
        //}

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                //isCtrlPressed = false;
                //StopResizeTimer();
            }
        }

        //private System.Windows.Forms.Timer resizeTimer;
        //private void StartResizeTimer()
        //{
        //    if (resizeTimer == null)
        //    {
        //        resizeTimer = new System.Windows.Forms.Timer();
        //        resizeTimer.Interval = 100; // 100 ms
        //        resizeTimer.Tick += (s, e) => ResizeActiveControl();
        //        resizeTimer.Start();
        //    }
        //}

        //private void StopResizeTimer()
        //{
        //    if (resizeTimer != null)
        //    {
        //        resizeTimer.Stop();
        //        resizeTimer.Dispose();
        //        resizeTimer = null;
        //    }
        //}

        private void ResizeActiveControl()
        {
            if (activeControl == null) return;

            // 1) Feldgröße anpassen
            if (Keyboard.IsKeyDown(Keys.Up))
                activeControl.Height = Math.Max(5, activeControl.Height - 1);
            else if (Keyboard.IsKeyDown(Keys.Down))
                activeControl.Height += 1;

            if (Keyboard.IsKeyDown(Keys.Left))
                activeControl.Width = Math.Max(5, activeControl.Width - 1);
            else if (Keyboard.IsKeyDown(Keys.Right))
                activeControl.Width += 1;

            /* 2) 🆕 Roten Auswahlrahmen synchronisieren */
            if (highlightBorder.Visible)
            {
                highlightBorder.Bounds = new Rectangle(
                    activeControl.Left - 2,
                    activeControl.Top - 2,
                    activeControl.Width + 4,
                    activeControl.Height + 4);
                highlightBorder.SendToBack(); // bleibt hinter dem Feld
            }

        }

        private void toolStripButton_ToggleLabelPanel_Click(object sender, EventArgs e)
        {
            if (activeControl == null)
                return;

            // Label → Panel (ohne Text)
            if (activeControl is Label && activeControl.Parent == panel2)
            {
                Panel panel = new Panel
                {
                    Location = activeControl.Location,
                    Size = activeControl.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.LightGray,
                    Name = activeControl.Name // 👈 Name übernehmen
                };

                panel.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
                panel.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
                panel.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);
                //panel.PreviewKeyDown += new PreviewKeyDownEventHandler(FeldInPanel_PreviewKeyDown);

                panel2.Controls.Add(panel);
                panel2.Controls.Remove(activeControl);
                activeControl.Dispose();
                activeControl = panel;
                activeControl.BringToFront();

                ZeigeHighlightUm(activeControl);
                highlightBorder.SendToBack();
            }
            // Panel → Label (leer)
            else if (activeControl is Panel && activeControl.Parent == panel2)
            {
                Label label = new Label
                {
                    Location = activeControl.Location,
                    Size = activeControl.Size,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = SystemColors.Control,
                    AutoSize = false,
                    Name = activeControl.Name, // Name übernehmen
                    TextAlign = currentAlignment
                };

                label.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
                label.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
                label.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);
                //label.PreviewKeyDown += new PreviewKeyDownEventHandler(FeldInPanel_PreviewKeyDown);

                panel2.Controls.Add(label);
                panel2.Controls.Remove(activeControl);
                activeControl.Dispose();
                activeControl = label;
                activeControl.BringToFront();

                ZeigeHighlightUm(activeControl);
                highlightBorder.SendToBack();
            }
        }

        private void NeuesLabel_Click(object sender, EventArgs e)
        {
            activeControl = sender as Control;
            activeControl.Focus(); // Optional, wenn Tasteneingaben érforderlich

            activeControl = sender as Control;
            activeControl.Focus();
            DisplayFieldProperties(activeControl);
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
            ctrl.BringToFront(); // Das Feld bleibt über dem Rahmen sichtbar
        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            // Klick auf leeren Bereich
            Control clicked = panel2.GetChildAtPoint(e.Location);
            if (clicked == null)
            {
                activeControl = null;
                highlightBorder.Visible = false;
            }
        }

        private string GeneriereNeuenFeldnamen()
        {
            int maxNummer = 0;

            foreach (Control ctrl in panel2.Controls)
            {
                if (ctrl.Name.StartsWith("Feld"))
                {
                    string nummerTeil = ctrl.Name.Substring(4);
                    int nummer; // Deklaration separat
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

            kopie.Name = GeneriereNeuenFeldnamen(); // eindeutiger Name
            kopie.Location = new Point(original.Left + 10, original.Top + 10);

            // Events zuweisen
            kopie.MouseDown += new MouseEventHandler(FeldInPanel_MouseDown);
            kopie.MouseMove += new MouseEventHandler(FeldInPanel_MouseMove);
            kopie.MouseUp += new MouseEventHandler(FeldInPanel_MouseUp);
            //kopie.PreviewKeyDown += new PreviewKeyDownEventHandler(FeldInPanel_PreviewKeyDown);
            kopie.Click += NeuesLabel_Click;

            panel2.Controls.Add(kopie);
            kopie.BringToFront();
            kopie.Focus();

            // Auswahlrahmen anzeigen
            activeControl = kopie;
            ZeigeHighlightUm(kopie);
            highlightBorder.SendToBack();

            DisplayFieldProperties(activeControl);
        }

        private ContentAlignment currentAlignment = ContentAlignment.MiddleCenter;  // Standard = zentriert
        // Form1.cs  –  Event-Handler für alle drei RadioButtons ◄◄
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)      // links
                currentAlignment = ContentAlignment.MiddleLeft;
            else if (radioButton2.Checked) // zentriert
                currentAlignment = ContentAlignment.MiddleCenter;
            else if (radioButton3.Checked) // rechts
                currentAlignment = ContentAlignment.MiddleRight;

            Label lbl = activeControl as Label;
            if (lbl != null)
                lbl.TextAlign = currentAlignment;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (activeControl == null || comboBox1.SelectedItem == null)
                return;

            // Gewähltes Auftragsmerkmal
            string auftragsMerkmal = comboBox1.SelectedItem.ToString();

            /* 1) Eindeutigkeit sicherstellen ---------------------------------------------------- */
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

            /* 2) Werte des aktiven Feldes einsammeln ------------------------------------------- */
            // Alignment ermitteln
            int alignVal = 32;                                   // Default zentriert
            Label lbl = activeControl as Label;
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignVal = 16;
                else if (lbl.TextAlign == ContentAlignment.MiddleRight) alignVal = 48;
            }

            // Textfarbe (falls Label), Fontinfos usw.
            int textColor = (lbl != null) ? lbl.ForeColor.ToArgb() : 0;
            float fontSize = (lbl != null) ? lbl.Font.Size : 0f;
            string fontName = (lbl != null) ? lbl.Font.Name : "";
            int fontStyle = (lbl != null) ? (int)lbl.Font.Style : 0;
            string textVal = (lbl != null) ? lbl.Text : "";

            /* 3) String-Array mit 15 Feldern aufbauen ----------------------------------------- */
            string[] tagData = new string[15];
            tagData[0] = "Alignment=" + alignVal;
            tagData[1] = "PosY=" + activeControl.Top;
            tagData[2] = "Fontgröße=" + fontSize;
            tagData[3] = "Text=" + textVal;
            tagData[4] = "Füllzeichen=";                       // (kommt später)
            tagData[5] = "Höhe=" + activeControl.Height;
            tagData[6] = "Textfarbe=" + textColor;
            tagData[7] = "Stellen=0";
            tagData[8] = "PosX=" + activeControl.Left;
            tagData[9] = "FeldName=" + activeControl.Name;
            tagData[10] = "FontName=" + fontName;
            tagData[11] = "Breite=" + activeControl.Width;
            tagData[12] = "Fontstyle=" + fontStyle;
            tagData[13] = "Farbe=" + activeControl.BackColor.ToArgb();
            tagData[14] = "DSFeldname=" + auftragsMerkmal;

            /* 4) Tag schreiben ---------------------------------------------------------------- */
            activeControl.Tag = BuildTagArray(activeControl, auftragsMerkmal);

            MessageBox.Show(
                "Auftragsmerkmal \"" + auftragsMerkmal +
                "\" wurde Feld \"" + activeControl.Name + "\" zugeordnet.",
                "Erfolgreich",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DisplayFieldProperties(activeControl);
        }

        private void DisplayFieldProperties(Control ctrl)
        {
            if (ctrl == null || richTextBox7 == null)
                return;

            /* Tag-Daten vorhanden? */
            string[] tagArr = ctrl.Tag as string[];
            if (tagArr != null && tagArr.Length == 15)
            {
                richTextBox7.Lines = tagArr;              // jede Eigenschaft in eine Zeile
                return;
            }

            /* Fallback: Werte zur Laufzeit ermitteln ----------------------- */
            List<string> lines = new List<string>();

            // Alignment
            int alignVal = 32;                                // zentriert
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
            lines.Add("Füllzeichen=");                               // Platzhalter
            lines.Add("Höhe=" + ctrl.Height);
            lines.Add("Textfarbe=" + ((lbl != null) ? lbl.ForeColor.ToArgb() : 0));
            lines.Add("Stellen=0");
            lines.Add("PosX=" + ctrl.Left);
            lines.Add("FeldName=" + ctrl.Name);
            lines.Add("FontName=" + ((lbl != null) ? lbl.Font.Name : ""));
            lines.Add("Breite=" + ctrl.Width);
            lines.Add("Fontstyle=" + ((lbl != null) ? (int)lbl.Font.Style : 0));
            lines.Add("Farbe=" + ctrl.BackColor.ToArgb());
            lines.Add("DSFeldname=");                                // Platzhalter

            richTextBox7.Lines = lines.ToArray();
        }

        private string[] BuildTagArray(Control ctrl, string auftragsMerkmal)
        {
            // Alignment ermitteln
            int alignVal = 32;
            Label lbl = ctrl as Label;
            if (lbl != null)
            {
                if (lbl.TextAlign == ContentAlignment.MiddleLeft) alignVal = 16;
                else if (lbl.TextAlign == ContentAlignment.MiddleRight) alignVal = 48;
            }

            // Text- & Font-Infos (nur Label)
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

            // Nur abwählen, wenn das Feld bereits ausgewählt ist
            if (clicked != null && clicked == activeControl)
            {
                activeControl = null;            // Auswahl aufheben
                highlightBorder.Visible = false; // roten Rahmen ausblenden
                richTextBox7.Clear();            // Eigenschaftenanzeige leeren
                toolStripStatusLabel2.Text = ""; // Status zurücksetzen
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //if (_state.ActiveControl == null)            // kein Feld ausgewählt
            //    return base.ProcessCmdKey(ref msg, keyData);

            //bool ctrl = (keyData & Keys.Control) == Keys.Control;
            //Keys arrows = keyData & ~Keys.Control;       // Modifier herausfiltern

            //switch (arrows)
            //{
            //    case Keys.Left: HandleArrow(ctrl, -1, 0); return true;
            //    case Keys.Right: HandleArrow(ctrl, 1, 0); return true;
            //    case Keys.Up: HandleArrow(ctrl, 0, -1); return true;
            //    case Keys.Down: HandleArrow(ctrl, 0, 1); return true;
            //}
            //return base.ProcessCmdKey(ref msg, keyData);

            System.Diagnostics.Debug.WriteLine("KeyData=" + keyData);

            if (_state.ActiveControl == null)
                return base.ProcessCmdKey(ref msg, keyData);

            bool ctrl = (keyData & Keys.Control) == Keys.Control;
            Keys arrow = keyData & ~Keys.Control;

            int dx = 0, dy = 0;
            switch (arrow)
            {
                case Keys.Left: dx = -1; break;
                case Keys.Right: dx = 1; break;
                case Keys.Up: dy = -1; break;
                case Keys.Down: dy = 1; break;
                default: return base.ProcessCmdKey(ref msg, keyData);
            }

            if (ctrl)
                VerarbeiteResize(dx, dy);
            else
                VerarbeiteMove(dx, dy);

            return true;  // Taste verarbeitet
        }

        private void VerarbeiteResize(int dxSign, int dySign)
        {
            Control c = _state.ActiveControl;
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

            Size ziel = new Size(
                Math.Max(5, c.Width + dxSign * step),
                Math.Max(5, c.Height + dySign * step));

            IEditorCommand cmd = new ResizeCommand(c, ziel);
            _undoMgr.Do(cmd);

            ZeigeHighlightUm(c);
        }

        private void VerarbeiteMove(int dxSign, int dySign)
        {
            Control c = _state.ActiveControl;
            int step = _state.RasterAktiv ? _state.RasterAbstand : 1;

            Point ziel = new Point(
                c.Left + dxSign * step,
                c.Top + dySign * step);

            if (_state.RasterAktiv)
                ziel = SnapToGrid(ziel);

            IEditorCommand cmd = new MoveCommand(c, ziel);
            _undoMgr.Do(cmd);

            ZeigeHighlightUm(c);
        }

        private void HandleArrow(bool ctrl, int dxSign, int dySign)
        {
            Control c = _state.ActiveControl;

            // Verschieben
            if (!ctrl)
            {
                int step = _state.RasterAktiv ? _state.RasterAbstand : 1;
                Point newPos = new Point(c.Left + dxSign * step,
                                         c.Top + dySign * step);

                // Raster-Snap erzwingen, wenn aktiv
                if (_state.RasterAktiv)
                    newPos = SnapToGrid(newPos);

                var cmd = new MoveCommand(c, newPos);
                _undoMgr.Do(cmd);
            }
            // Skalieren
            else
            {
                int step = _state.RasterAktiv ? _state.RasterAbstand : 1;
                Size newSize = new Size(
                    Math.Max(5, c.Width + dxSign * step),   // Links/Rechts → Breite
                    Math.Max(5, c.Height + dySign * step));  // Oben/Unten  → Höhe

                var cmd = new ResizeCommand(c, newSize);
                _undoMgr.Do(cmd);
            }

            ZeigeHighlightUm(c);          // roten Rahmen anpassen
        }
    }
}
