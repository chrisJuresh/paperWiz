using System.IO;
using System.Text.Json;
using PaperWiz.Models;

namespace PaperWiz.Services;

/// <summary>Persists the last applied wallpaper configuration for the current user.</summary>
public static class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaperWiz");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static PaperWizSettings? Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return null;

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<PaperWizSettings>(json, JsonOptions);
        }
        catch
        {
            // A corrupt or inaccessible settings file must never stop the app opening.
            return null;
        }
    }

    public static void Save(PaperWizSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);

        // Atomic replacement avoids leaving truncated JSON if Windows shuts down mid-write.
        string temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
