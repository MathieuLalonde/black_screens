using System.Text.Json;

namespace BlackScreens;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public bool IsActive { get; set; }

    /// <summary>
    /// Device names (e.g. \\.\DISPLAY1) that should be blacked out when active.
    /// </summary>
    public HashSet<string> SelectedDeviceNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    private static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BlackScreens");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettingsDto>(json);
            if (loaded is null)
            {
                return new AppSettings();
            }

            return new AppSettings
            {
                IsActive = loaded.IsActive,
                SelectedDeviceNames = new HashSet<string>(
                    loaded.SelectedDeviceNames ?? [],
                    StringComparer.OrdinalIgnoreCase),
            };
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var dto = new AppSettingsDto
        {
            IsActive = IsActive,
            SelectedDeviceNames = SelectedDeviceNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(),
        };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private sealed class AppSettingsDto
    {
        public bool IsActive { get; set; }
        public List<string>? SelectedDeviceNames { get; set; }
    }
}
