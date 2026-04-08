using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClaudeAlert.Core;
using ClaudeAlert.Physics;
using ClaudeAlert.Setup;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;

namespace ClaudeAlert.Views;

public partial class OverlayWindow : Window
{
    private readonly PhysicsBody _body;
    private readonly PhysicsEngine _engine;
    private readonly EscalationController _escalation;
    private readonly ClaudeStatusManager _statusManager;
    private bool _isDragging;
    private Point _dragStartMouse;
    private Point _dragStartPos;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    public OverlayWindow(ClaudeStatusManager statusManager, AppSettings settings)
    {
        InitializeComponent();
        _statusManager = statusManager;

        var screenW = SystemParameters.PrimaryScreenWidth;
        var screenH = SystemParameters.PrimaryScreenHeight;

        _body = new PhysicsBody
        {
            Width = 80,
            Height = 100,
            Position = new Point(screenW / 2 - 40, screenH - 48 - 100)
        };

        _engine = new PhysicsEngine(_body);
        _escalation = new EscalationController(_body, _engine, statusManager, settings);

        _engine.Updated += OnPhysicsUpdated;
        _statusManager.PropertyChanged += (_, args) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (args.PropertyName == nameof(ClaudeStatusManager.StatusText))
                    StatusLabel.Text = _statusManager.StatusText;
                if (args.PropertyName == nameof(ClaudeStatusManager.ElapsedText))
                    ElapsedLabel.Text = _statusManager.ElapsedText;
            });
        };
        _statusManager.StateChanged += OnStateChanged;

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMoveHandler;
        MouseLeftButtonUp += OnMouseUp;
        MouseRightButtonUp += OnRightClick;

        Loaded += OnLoaded;
        SyncWindowToBody();
        LoadDefaultImage();
    }

    public EscalationController Escalation => _escalation;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
    }

    private void LoadDefaultImage()
    {
        try
        {
            var settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.CustomImagePath) && System.IO.File.Exists(settings.CustomImagePath))
            {
                LoadImage(settings.CustomImagePath);
                return;
            }

            // Default placeholder: blue circle with face
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawEllipse(
                    new LinearGradientBrush(
                        Color.FromRgb(100, 180, 255),
                        Color.FromRgb(60, 120, 200), 90),
                    new Pen(Brushes.White, 2),
                    new Point(32, 32), 30, 30);
                dc.DrawEllipse(Brushes.White, null, new Point(24, 26), 4, 5);
                dc.DrawEllipse(Brushes.White, null, new Point(40, 26), 4, 5);
                dc.DrawEllipse(Brushes.Black, null, new Point(24, 27), 2, 3);
                dc.DrawEllipse(Brushes.Black, null, new Point(40, 27), 2, 3);
                dc.DrawGeometry(null, new Pen(Brushes.White, 1.5),
                    Geometry.Parse("M 24,38 Q 32,46 40,38"));
            }
            var bmp = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            PetImage.Source = bmp;
        }
        catch { }
    }

    public void LoadImage(string path)
    {
        try
        {
            if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(PetImage, new BitmapImage(new Uri(path)));
            }
            else
            {
                PetImage.Source = new BitmapImage(new Uri(path));
            }
        }
        catch { }
    }

    private void OnStateChanged(ClaudeState oldState, ClaudeState newState)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (newState == ClaudeState.Acknowledged)
            {
                _escalation.Reset();
                _body.Position = new Point(_body.Position.X, _engine.GroundY);
                _body.MakeStatic();
                SyncWindowToBody();
            }
        });
    }

    private void OnPhysicsUpdated()
    {
        Dispatcher.InvokeAsync(SyncWindowToBody);
    }

    private void SyncWindowToBody()
    {
        Left = _body.Position.X;
        Top = _body.Position.Y;
        RotateTransform.Angle = _body.Rotation;
        SquashTransform.ScaleX = _body.ScaleX;
        SquashTransform.ScaleY = _body.ScaleY;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _body.IsDragging = true;
        _dragStartMouse = PointToScreen(e.GetPosition(this));
        _dragStartPos = _body.Position;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMoveHandler(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var currentMouse = PointToScreen(e.GetPosition(this));
        var delta = currentMouse - _dragStartMouse;
        _body.Position = new Point(
            _dragStartPos.X + delta.X,
            _dragStartPos.Y + delta.Y);
        SyncWindowToBody();
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var wasDragging = _isDragging;
        _isDragging = false;
        _body.IsDragging = false;
        ReleaseMouseCapture();

        // Check if it was a click (not a drag)
        var currentMouse = PointToScreen(e.GetPosition(this));
        var moved = (currentMouse - _dragStartMouse).Length;

        if (moved < 5) // it was a click, not a drag
        {
            if (_statusManager.IsEscalating)
            {
                _statusManager.Acknowledge();
                FocusHelper.FocusClaudeCode();
            }
            else
            {
                _escalation.MiniJump();
            }
        }
        else
        {
            // It was a drag - drop with gravity
            _body.IsStatic = false;
            _engine.Start();
        }
        e.Handled = true;
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var settingsItem = new MenuItem { Header = "설정" };
        settingsItem.Click += (_, _) =>
        {
            var settings = AppSettings.Load();
            var win = new SettingsWindow(settings, path => LoadImage(path));
            win.ShowDialog();
        };
        menu.Items.Add(settingsItem);

        var hideItem = new MenuItem { Header = "숨기기" };
        hideItem.Click += (_, _) => Hide();
        menu.Items.Add(hideItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "종료" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        menu.IsOpen = true;
        e.Handled = true;
    }
}
