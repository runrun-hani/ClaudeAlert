using System.IO;
using System.Text.Json;

namespace ClaudeAlert.Core;

public class AppSettings
{
    public int StuckThresholdSeconds { get; set; } = 120;
    public int EscalationJumpSeconds { get; set; } = 30;
    public int EscalationRollSeconds { get; set; } = 60;
    public int EscalationBounceSeconds { get; set; } = 180;
    public bool SoundEnabled { get; set; } = true;
    public string? CustomImagePath { get; set; }
    public int ImageSize { get; set; } = 64;
    public double FontSize { get; set; } = 10;
    public string Language { get; set; } = "Korean";
    public bool ShowStatusBar { get; set; } = false;

    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ClaudeAlert");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");
    public static readonly string ImagesDir = Path.Combine(ConfigDir, "images");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(ImagesDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
