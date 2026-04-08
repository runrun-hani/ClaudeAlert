using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ClaudeAlert.Core;

namespace ClaudeAlert.Views;

public partial class StatusBarWindow : Window
{
    private readonly ClaudeStatusManager _statusManager;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    public StatusBarWindow(ClaudeStatusManager statusManager, AppSettings settings)
    {
        InitializeComponent();
        _statusManager = statusManager;

        StatusLabel.FontSize = settings.FontSize;
        ElapsedLabel.FontSize = Math.Max(settings.FontSize - 2, 7);

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

        MouseLeftButtonDown += (_, _) => DragMove();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Hide from Alt+Tab
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

        // Position at bottom center of screen
        var screenW = SystemParameters.PrimaryScreenWidth;
        var screenH = SystemParameters.PrimaryScreenHeight;
        Left = (screenW - ActualWidth) / 2;
        Top = screenH - 80;
    }

    public void ApplyFontSize(double fontSize)
    {
        StatusLabel.FontSize = fontSize;
        ElapsedLabel.FontSize = Math.Max(fontSize - 2, 7);
    }

    private readonly List<string> _debugLines = new();

    public void AddDebugLog(string msg)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            _debugLines.Add($"[{time}] {msg}");
            if (_debugLines.Count > 20)
                _debugLines.RemoveAt(0);
            DebugLog.Text = string.Join("\n", _debugLines);
        });
    }
}
