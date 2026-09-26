namespace BlackScreens;

public sealed record MonitorInfo(
    string DeviceName,
    string DisplayLabel,
    Rectangle Bounds,
    bool IsPrimary);

public static class MonitorCatalog
{
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Screen.AllScreens
            .Select(screen =>
            {
                string size = $"{screen.Bounds.Width}\u00d7{screen.Bounds.Height}";
                string shortName = ShortDeviceName(screen.DeviceName);
                string label = screen.Primary
                    ? $"{shortName} ({size}, primary)"
                    : $"{shortName} ({size})";

                return new MonitorInfo(
                    screen.DeviceName,
                    label,
                    screen.Bounds,
                    screen.Primary);
            })
            .OrderBy(m => ShortDeviceName(m.DeviceName), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string ShortDeviceName(string deviceName)
    {
        const string prefix = @"\\.\";
        return deviceName.StartsWith(prefix, StringComparison.Ordinal)
            ? deviceName[prefix.Length..]
            : deviceName;
    }
}
