using System.Text.Json;

namespace AsusKeyboardBacklight;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public int IdleTimeoutSeconds { get; set; } = 60;
    public int BrightnessLevel { get; set; } = 2;
}

internal sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public AppSettingsStore()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsusKeyboardBacklight");
        _path = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
            settings.IdleTimeoutSeconds = Math.Clamp(settings.IdleTimeoutSeconds, 10, 3600);
            settings.BrightnessLevel = Math.Clamp(settings.BrightnessLevel, 1, 3);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
