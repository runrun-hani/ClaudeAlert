using System.Windows;
using System.Windows.Controls;
using ClaudeAlert.Core;
using ClaudeAlert.Setup;

namespace ClaudeAlert.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action? _onSettingsChanged;
    private bool _isLoading;

    public SettingsWindow(AppSettings settings, Action? onSettingsChanged = null)
    {
        InitializeComponent();
        _settings = settings;
        _onSettingsChanged = onSettingsChanged;
        L10n.LanguageChanged += RefreshLabels;
        Closed += (_, _) => L10n.LanguageChanged -= RefreshLabels;
        LoadSettings();
    }

    private void LoadSettings()
    {
        _isLoading = true;

        PortBox.Text = _settings.Port.ToString();
        StuckThresholdBox.Text = _settings.StuckThresholdSeconds.ToString();
        JumpBox.Text = _settings.EscalationJumpSeconds.ToString();
        RollBox.Text = _settings.EscalationRollSeconds.ToString();
        BounceBox.Text = _settings.EscalationBounceSeconds.ToString();
        SoundCheck.IsChecked = _settings.SoundEnabled;
        AutoStartCheck.IsChecked = AutoStartManager.IsEnabled;
        ImagePathBox.Text = _settings.CustomImagePath ?? "";
        ImageSizeSlider.Value = _settings.ImageSize;
        ImageSizeValue.Text = $"{_settings.ImageSize}px";
        FontSizeSlider.Value = _settings.FontSize;
        FontSizeValue.Text = $"{_settings.FontSize}pt";

        // Language combo
        for (int i = 0; i < LanguageCombo.Items.Count; i++)
        {
            if (LanguageCombo.Items[i] is ComboBoxItem item &&
                item.Tag?.ToString() == _settings.Language)
            {
                LanguageCombo.SelectedIndex = i;
                break;
            }
        }

        UpdateHookStatus();
        RefreshLabels();

        _isLoading = false;
    }

    private void RefreshLabels()
    {
        TitleLabel.Text = L10n.Get("settings.title");
        LanguageLabel.Text = L10n.Get("settings.language");
        PortLabel.Text = L10n.Get("settings.port");
        StuckLabel.Text = L10n.Get("settings.stuck_threshold");
        EscalationLabel.Text = L10n.Get("settings.escalation_timing");
        JumpLabel.Text = L10n.Get("settings.jump");
        RollLabel.Text = L10n.Get("settings.roll");
        BounceLabel.Text = L10n.Get("settings.bounce");
        SoundCheck.Content = L10n.Get("settings.sound");
        AutoStartCheck.Content = L10n.Get("settings.autostart");
        ImageLabel.Text = L10n.Get("settings.custom_image");
        BrowseButton.Content = L10n.Get("settings.browse");
        ImageSizeLabel.Text = L10n.Get("settings.image_size");
        FontSizeLabel.Text = L10n.Get("settings.font_size");
        HookLabel.Text = L10n.Get("settings.hook_status");
        ReconfigureButton.Content = L10n.Get("settings.reconfigure");
        CancelButton.Content = L10n.Get("settings.cancel");
        SaveButton.Content = L10n.Get("settings.save");
        Title = L10n.Get("settings.title");
        UpdateHookStatus();
    }

    private void UpdateHookStatus()
    {
        var configured = HookConfigurator.IsConfigured(_settings.Port);
        HookStatusLabel.Text = configured
            ? L10n.Get("settings.hook_configured")
            : L10n.Get("settings.hook_not_configured");
        HookStatusLabel.Foreground = configured
            ? System.Windows.Media.Brushes.LimeGreen
            : System.Windows.Media.Brushes.OrangeRed;
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            L10n.SetLanguage(lang);
        }
    }

    private void OnImageSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading || ImageSizeValue == null) return;
        ImageSizeValue.Text = $"{(int)ImageSizeSlider.Value}px";
    }

    private void OnFontSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading || FontSizeValue == null) return;
        FontSizeValue.Text = $"{(int)FontSizeSlider.Value}pt";
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = L10n.Get("settings.image_filter"),
            InitialDirectory = AppSettings.ImagesDir
        };
        if (dialog.ShowDialog() == true)
        {
            ImagePathBox.Text = dialog.FileName;
        }
    }

    private void OnReconfigureClick(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(PortBox.Text, out var port))
        {
            HookConfigurator.EnsureHooksConfigured(port);
            _settings.Port = port;
            UpdateHookStatus();
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(PortBox.Text, out var port)) _settings.Port = port;
        if (int.TryParse(StuckThresholdBox.Text, out var stuck)) _settings.StuckThresholdSeconds = stuck;
        if (int.TryParse(JumpBox.Text, out var jump)) _settings.EscalationJumpSeconds = jump;
        if (int.TryParse(RollBox.Text, out var roll)) _settings.EscalationRollSeconds = roll;
        if (int.TryParse(BounceBox.Text, out var bounce)) _settings.EscalationBounceSeconds = bounce;
        _settings.SoundEnabled = SoundCheck.IsChecked == true;
        _settings.CustomImagePath = string.IsNullOrWhiteSpace(ImagePathBox.Text) ? null : ImagePathBox.Text;
        _settings.ImageSize = (int)ImageSizeSlider.Value;
        _settings.FontSize = (int)FontSizeSlider.Value;

        if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            _settings.Language = lang;

        AutoStartManager.SetEnabled(AutoStartCheck.IsChecked == true);
        _settings.Save();

        _onSettingsChanged?.Invoke();
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
