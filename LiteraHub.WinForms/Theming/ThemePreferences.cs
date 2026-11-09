
using System;
using System.IO;
using System.Text.Json;

namespace LiteraHub.WinForms.Theming;

public static class ThemePreferences
{
    private sealed class AppearanceSettings
    {
        public string? ApplicationPreset { get; set; }
    }

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "LiteraHub",
            "appearance.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            var json = File.ReadAllText(SettingsPath);

            var settings =
                JsonSerializer.Deserialize<AppearanceSettings>(json);

            if (settings?.ApplicationPreset is not string name)
                return;

            if (Enum.TryParse<ThemePreset>(
                    name, ignoreCase: true, out var preset)
                && Enum.IsDefined(preset))
            {
                Theme.SetPreset(preset);
            }
        }
        catch (Exception)
        {
            // Missing, corrupted, or inaccessible preferences
            // must never prevent the application from starting.
        }
    }

    public static bool TrySave(out string? error)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);

            var settings = new AppearanceSettings
            {
                ApplicationPreset = Theme.Preset.ToString()
            };

            var json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var temporaryPath = SettingsPath + ".tmp";

            File.WriteAllText(temporaryPath, json);
            File.Move(
                temporaryPath,
                SettingsPath,
                overwrite: true);

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
