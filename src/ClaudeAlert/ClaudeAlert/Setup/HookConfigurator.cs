using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClaudeAlert.Setup;

public static class HookConfigurator
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude", "settings.json");

    public static void EnsureHooksConfigured(int port)
    {
        try
        {
            JsonNode? root;
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                root = JsonNode.Parse(json) ?? new JsonObject();
                // Backup
                File.Copy(SettingsPath, SettingsPath + ".bak", true);
            }
            else
            {
                root = new JsonObject();
            }

            var hooks = root["hooks"]?.AsObject() ?? new JsonObject();
            root["hooks"] = hooks;

            var marker = $"localhost:{port}/event";

            EnsureHook(hooks, "Stop", "stop", port, marker);
            EnsureHook(hooks, "PostToolUse", "tool_use", port, marker);
            EnsureNotificationHook(hooks, "permission_prompt", port, marker);
            EnsureNotificationHook(hooks, "idle_prompt", port, marker);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsPath, root.ToJsonString(options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HookConfigurator error: {ex.Message}");
        }
    }

    private static void EnsureHook(JsonObject hooks, string hookType, string eventType, int port, string marker)
    {
        var array = hooks[hookType]?.AsArray() ?? new JsonArray();
        hooks[hookType] = array;

        // Check if already configured
        foreach (var item in array)
        {
            var innerHooks = item?["hooks"]?.AsArray();
            if (innerHooks != null)
            {
                foreach (var h in innerHooks)
                {
                    if (h?["command"]?.GetValue<string>()?.Contains(marker) == true)
                        return; // already exists
                }
            }
        }

        var curlCmd = BuildCurlCommand(eventType, port);
        array.Add(new JsonObject
        {
            ["hooks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = curlCmd,
                    ["timeout"] = 5
                }
            }
        });
    }

    private static void EnsureNotificationHook(JsonObject hooks, string matcher, int port, string marker)
    {
        var array = hooks["Notification"]?.AsArray() ?? new JsonArray();
        hooks["Notification"] = array;

        foreach (var item in array)
        {
            if (item?["matcher"]?.GetValue<string>() == matcher)
            {
                var innerHooks = item?["hooks"]?.AsArray();
                if (innerHooks != null)
                {
                    foreach (var h in innerHooks)
                    {
                        if (h?["command"]?.GetValue<string>()?.Contains(marker) == true)
                            return;
                    }
                }
            }
        }

        var curlCmd = BuildCurlCommand(matcher, port);
        array.Add(new JsonObject
        {
            ["matcher"] = matcher,
            ["hooks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = curlCmd,
                    ["timeout"] = 5
                }
            }
        });
    }

    private static string BuildCurlCommand(string eventType, int port)
    {
        return $"curl -s -X POST http://localhost:{port}/event -H \"Content-Type: application/json\" -d \"{{\\\"type\\\":\\\"{eventType}\\\"}}\"";
    }

    public static bool IsConfigured(int port)
    {
        try
        {
            if (!File.Exists(SettingsPath)) return false;
            var json = File.ReadAllText(SettingsPath);
            var root = JsonNode.Parse(json);
            var marker = $"localhost:{port}/event";
            return root?["hooks"]?["Stop"]?.ToJsonString().Contains(marker) == true;
        }
        catch { return false; }
    }
}
