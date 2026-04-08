using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    private readonly AppSettings _settings;
    private bool _isDragging;
    private Point _dragStartMouse;
    private Point _dragStartPos;
    private readonly DispatcherTimer _bubbleHideTimer;
    private readonly DispatcherTimer _bubbleReshowTimer;

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
        _settings = settings;

        var imgSize = settings.ImageSize;
        ApplyImageSize(imgSize);

        var screenW = SystemParameters.PrimaryScreenWidth;
        var screenH = SystemParameters.PrimaryScreenHeight;

        _body = new PhysicsBody
        {
            Width = imgSize,
            Height = imgSize,
            Position = new Point(screenW / 2 - imgSize / 2.0, screenH - 48 - imgSize)
        };

        _engine = new PhysicsEngine(_body);
        _escalation = new EscalationController(_body, _engine, statusManager, settings);

        _bubbleHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _bubbleHideTimer.Tick += (_, _) => { HideBubble(); _bubbleHideTimer.Stop(); };

        _bubbleReshowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _bubbleReshowTimer.Tick += (_, _) =>
        {
            if (_statusManager.IsEscalating && _escalation.CurrentLevel != EscalationLevel.Bounce)
                ShowBubbleForState(_statusManager.CurrentState);
        };

        _engine.Updated += OnPhysicsUpdated;
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
    public StatusBarWindow? StatusBar { get; set; }

    private void ApplyImageSize(int size)
    {
        PetImage.Width = size;
        PetImage.Height = size;
        var center = size / 2.0;
        SquashTransform.CenterX = center;
        SquashTransform.CenterY = size;
        RotateTransform.CenterX = center;
        RotateTransform.CenterY = center;
    }

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
            if (!string.IsNullOrEmpty(_settings.CustomImagePath) && System.IO.File.Exists(_settings.CustomImagePath))
            {
                LoadImage(_settings.CustomImagePath);
                return;
            }

            // Default placeholder: blue circle with face
            var size = _settings.ImageSize;
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var r = size / 2.0 - 2;
                var cx = size / 2.0;
                var cy = size / 2.0;
                dc.DrawEllipse(
                    new LinearGradientBrush(
                        Color.FromRgb(100, 180, 255),
                        Color.FromRgb(60, 120, 200), 90),
                    new Pen(Brushes.White, 2),
                    new Point(cx, cy), r, r);
                var eyeOffX = size * 0.125;
                var eyeY = cy - size * 0.06;
                dc.DrawEllipse(Brushes.White, null, new Point(cx - eyeOffX, eyeY), size * 0.06, size * 0.08);
                dc.DrawEllipse(Brushes.White, null, new Point(cx + eyeOffX, eyeY), size * 0.06, size * 0.08);
                dc.DrawEllipse(Brushes.Black, null, new Point(cx - eyeOffX, eyeY + 1), size * 0.03, size * 0.05);
                dc.DrawEllipse(Brushes.Black, null, new Point(cx + eyeOffX, eyeY + 1), size * 0.03, size * 0.05);
                var mouthY = cy + size * 0.1;
                dc.DrawGeometry(null, new Pen(Brushes.White, 1.5),
                    Geometry.Parse($"M {cx - eyeOffX},{mouthY} Q {cx},{mouthY + size * 0.12} {cx + eyeOffX},{mouthY}"));
            }
            var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
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

    public void ApplySettings()
    {
        var size = _settings.ImageSize;
        ApplyImageSize(size);
        _body.Width = size;
        _body.Height = size;
        _engine.UpdateScreenBounds();
        LoadDefaultImage();
    }

    private void OnStateChanged(ClaudeState oldState, ClaudeState newState)
    {
        Dispatcher.InvokeAsync(() =>
        {
            ShowBubbleForState(newState);

            if (newState == ClaudeState.Acknowledged)
            {
                _escalation.Reset();
                _body.Position = new Point(_body.Position.X, _engine.GroundY);
                _body.MakeStatic();
                SyncWindowToBody();
                _bubbleReshowTimer.Stop();
            }
            else if (_statusManager.IsEscalating)
            {
                _bubbleReshowTimer.Start();
            }
            else
            {
                _bubbleReshowTimer.Stop();
            }
        });
    }

    private void ShowBubbleForState(ClaudeState state)
    {
        var key = state switch
        {
            ClaudeState.Idle => "bubble.idle",
            ClaudeState.Active => "bubble.active",
            ClaudeState.Done => "bubble.done",
            ClaudeState.WaitingForInput => "bubble.waiting",
            ClaudeState.Stuck => "bubble.stuck",
            ClaudeState.Error => "bubble.error",
            ClaudeState.Acknowledged => "bubble.acknowledged",
            _ => null
        };
        if (key == null) return;

        BubbleText.Text = L10n.Get(key);
        BubbleText.FontSize = Math.Max(11, _settings.ImageSize * 0.18);
        BubblePanel.Visibility = Visibility.Visible;
        _bubbleHideTimer.Stop();
        _bubbleHideTimer.Start();
    }

    private void HideBubble()
    {
        BubblePanel.Visibility = Visibility.Collapsed;
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
        // Counter-rotate bubble so text stays readable
        BubbleCounterRotate.Angle = -_body.Rotation;
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
        _isDragging = false;
        _body.IsDragging = false;
        ReleaseMouseCapture();

        var currentMouse = PointToScreen(e.GetPosition(this));
        var moved = (currentMouse - _dragStartMouse).Length;

        if (moved < 5) // click
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
        else // drag release
        {
            _body.IsStatic = false;
            _engine.Start();
        }
        e.Handled = true;
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var settingsItem = new MenuItem { Header = L10n.Get("menu.settings") };
        settingsItem.Click += (_, _) =>
        {
            var settings = AppSettings.Load();
            var win = new SettingsWindow(settings, () =>
            {
                _settings.ImageSize = settings.ImageSize;
                _settings.FontSize = settings.FontSize;
                _settings.Language = settings.Language;
                _settings.CustomImagePath = settings.CustomImagePath;
                ApplySettings();
                StatusBar?.ApplyFontSize(settings.FontSize);
            });
            win.ShowDialog();
        };
        menu.Items.Add(settingsItem);

        var hideItem = new MenuItem { Header = L10n.Get("menu.hide") };
        hideItem.Click += (_, _) => Hide();
        menu.Items.Add(hideItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = L10n.Get("menu.exit") };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        menu.IsOpen = true;
        e.Handled = true;
    }
}
