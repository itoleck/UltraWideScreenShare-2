using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace UltraWideScreenShare.WinForms
{
    public partial class MainWindow : Form
    {
        
        private double _fpsrate = 30.0;
        private Timer _dispatcherTimer = new Timer(1000.0 / 30.0); // 30fps
        private Point _tittleBarLocation = new Point();
        private Magnifier _magnifier;
        private bool _isTransparent = false;
        private Color _frameColor = Color.FromArgb(255, 53, 89, 224); //#3559E0
        const int _borderWidth = 6;
        private bool _showMagnifierScheduled = true;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private int _frameCount = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TitleText
        {
            get => titleButton.Text;
            set
            {
                titleButton.Text = value;
                this.Text = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            TitleBar.BringToFront();
            InitializePaddingsForBorders();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            this.InitializeMainWindowStyle();
        }

        private void InitializePaddingsForBorders()
        {
            Padding = new Padding(_borderWidth, _borderWidth, _borderWidth, _borderWidth);
            TitleBar.Width += (_borderWidth * 2);
            TitleBar.Height += (_borderWidth);
            TitleBar.Padding = new Padding(_borderWidth, 0, _borderWidth, _borderWidth);
        }

        protected override void OnMove(EventArgs e)
        {
            MaximizedBounds = new Rectangle(Point.Empty, Screen.GetWorkingArea(Location).Size);
            base.OnMove(e);
        }
        private void MainWindow_Load(object sender, EventArgs e)
        {
            _magnifier = new Magnifier(magnifierPanel.Handle);
            _dispatcherTimer.SynchronizingObject = this;
            _dispatcherTimer.Start();
            _dispatcherTimer.Elapsed += (s, a) =>
            {
                _frameCount++;
                double elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
                if (elapsedSeconds >= 3.0)
                {
                    TitleText = $"Ultra Wide Screen Share 2.0 ({(int)Math.Round(_frameCount / elapsedSeconds)} fps)";
                    _frameCount = 0;
                    _fpsStopwatch.Restart();
                }
                _magnifier.UpdateMagnifierWindow();
                if (magnifierPanel.Bounds.Contains(PointToClient(Cursor.Position)) && !TitleBar.Bounds.Contains(PointToClient(Cursor.Position)))
                {
                    if (!_isTransparent)
                    { this.SetTransparency(_isTransparent = true); Trace.WriteLine("enter"); }
                }
                else
                {
                    if (_isTransparent)
                    { this.SetTransparency(_isTransparent = false); Trace.WriteLine("leave"); }
                }
                if (_showMagnifierScheduled)
                {
                    _magnifier.ShowMagnifier();
                    _showMagnifierScheduled = false;
                }
            };
        }

        private void MainWindow_ResizeBegin(object sender, EventArgs e) => _magnifier.HideMagnifier();

        private void MainWindow_ResizeEnd(object sender, EventArgs e) => _showMagnifierScheduled = true;


        private void TittleButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Drag();
                SetupMaximizeButton();
            }
        }


        const int WM_NCCALCSIZE = 0x0083;
        const int WM_NCACTIVATE = 0x0086;
        const int WM_NCHITTEST = 0x0084;
        protected override void WndProc(ref Message m)
        {
            var message = m.Msg;
            if (message == WM_NCCALCSIZE)
            {
                return;
            }

            if (message == WM_NCACTIVATE)
            {
                m.Result = new IntPtr(-1);
                return;
            }

            base.WndProc(ref m);

            if (message == WM_NCHITTEST)
            {
                this.TryResize(ref m, _borderWidth);
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                _frameColor, _borderWidth, ButtonBorderStyle.Solid,
                _frameColor, _borderWidth, ButtonBorderStyle.Solid,
                _frameColor, _borderWidth, ButtonBorderStyle.Solid,
                _frameColor, _borderWidth, ButtonBorderStyle.Solid);
        }

        private void TitleBar_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, TitleBar.ClientRectangle,
               _frameColor, _borderWidth, ButtonBorderStyle.Solid,
               _frameColor, 0, ButtonBorderStyle.Solid,
               _frameColor, _borderWidth, ButtonBorderStyle.Solid,
               _frameColor, _borderWidth, ButtonBorderStyle.Solid);
        }

        private void closeButton_Click(object sender, EventArgs e) => Close();

        protected override void OnClosing(CancelEventArgs e)
        {
            _dispatcherTimer.Stop();
            _dispatcherTimer.Dispose();
            base.OnClosing(e);
        }
        private void minimizeButton_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;

        private void maximizeButton_Click(object sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
            SetupMaximizeButton();
        }

        private void SetupMaximizeButton()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                maximizeButton.Image = Properties.Resources.restore;
            }
            else
            {
                maximizeButton.Image = Properties.Resources.maximize;
            }
        }

        private void DragButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _tittleBarLocation = e.Location;
            }
        }

        private void DragButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TitleBar.Left = Math.Clamp(value: e.X + TitleBar.Left - _tittleBarLocation.X,
                    min: 0, max: Width - TitleBar.Width);

            }
        }

        private void LBFrameRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LBFrameRate.SelectedItem != null)
            {
                string selectedText = LBFrameRate.SelectedItem.ToString().ToLower().Replace(" fps", "").Replace("fps", "").Trim();
                if (double.TryParse(selectedText, out double newFps) && newFps > 0)
                {
                    _fpsrate = newFps;
                    _dispatcherTimer.Interval = 1000.0 / _fpsrate;
                }
            }
        }
    }
}
