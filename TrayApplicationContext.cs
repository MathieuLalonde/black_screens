using System.Drawing.Imaging;
using Microsoft.Win32;

namespace BlackScreens;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, BlackoutOverlay> _overlays = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ToolStripMenuItem> _monitorItems = new(StringComparer.OrdinalIgnoreCase);
    private ToolStripMenuItem? _activateItem;
    private ContextMenuStrip? _menu;
    private Icon? _customIcon;
    private bool _suppressActivateEvent;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _customIcon = CreateTrayIcon();

        _trayIcon = new NotifyIcon
        {
            Icon = _customIcon,
            Text = "Black Screens",
            Visible = true,
        };
        _trayIcon.MouseClick += OnTrayMouseClick;

        RebuildMenu();
        SyncOverlays();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Application.ApplicationExit += OnApplicationExit;
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();
        _monitorItems.Clear();

        IReadOnlyList<MonitorInfo> monitors = MonitorCatalog.GetMonitors();
        var knownDevices = new HashSet<string>(monitors.Select(m => m.DeviceName), StringComparer.OrdinalIgnoreCase);

        // Drop selections for monitors that no longer exist.
        _settings.SelectedDeviceNames.RemoveWhere(name => !knownDevices.Contains(name));

        foreach (MonitorInfo monitor in monitors)
        {
            bool selected = _settings.SelectedDeviceNames.Contains(monitor.DeviceName);
            var item = new ToolStripMenuItem(monitor.DisplayLabel)
            {
                Checked = selected,
                CheckOnClick = true,
                Tag = monitor.DeviceName,
            };
            item.CheckedChanged += OnMonitorCheckedChanged;
            _monitorItems[monitor.DeviceName] = item;
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());

        _activateItem = new ToolStripMenuItem("Activate blackout")
        {
            CheckOnClick = true,
        };
        _suppressActivateEvent = true;
        _activateItem.Checked = _settings.IsActive;
        _suppressActivateEvent = false;
        _activateItem.CheckedChanged += OnActivateCheckedChanged;
        menu.Items.Add(_activateItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();
        menu.Items.Add(exitItem);

        ContextMenuStrip? oldMenu = _menu;
        _menu = menu;
        _trayIcon.ContextMenuStrip = menu;
        oldMenu?.Dispose();

        UpdateTrayTooltip(monitors);
    }

    private void OnTrayMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ToggleActive();
        }
    }

    private void OnMonitorCheckedChanged(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item || item.Tag is not string deviceName)
        {
            return;
        }

        if (item.Checked)
        {
            _settings.SelectedDeviceNames.Add(deviceName);
        }
        else
        {
            _settings.SelectedDeviceNames.Remove(deviceName);
        }

        _settings.Save();
        SyncOverlays();
        UpdateTrayTooltip();
    }

    private void OnActivateCheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressActivateEvent || _activateItem is null)
        {
            return;
        }

        SetActive(_activateItem.Checked);
    }

    private void ToggleActive() => SetActive(!_settings.IsActive);

    private void SetActive(bool active)
    {
        if (_settings.IsActive != active)
        {
            _settings.IsActive = active;
            _settings.Save();
        }

        if (_activateItem is not null && _activateItem.Checked != active)
        {
            _suppressActivateEvent = true;
            _activateItem.Checked = active;
            _suppressActivateEvent = false;
        }

        SyncOverlays();
        UpdateTrayTooltip();
    }

    private void SyncOverlays()
    {
        IReadOnlyList<MonitorInfo> monitors = MonitorCatalog.GetMonitors();
        var byDevice = monitors.ToDictionary(m => m.DeviceName, StringComparer.OrdinalIgnoreCase);

        // Close overlays for monitors that are gone or no longer selected / inactive.
        foreach (string deviceName in _overlays.Keys.ToList())
        {
            bool shouldShow = _settings.IsActive
                && _settings.SelectedDeviceNames.Contains(deviceName)
                && byDevice.ContainsKey(deviceName);

            if (!shouldShow)
            {
                CloseOverlay(deviceName);
            }
        }

        if (!_settings.IsActive)
        {
            return;
        }

        foreach (string deviceName in _settings.SelectedDeviceNames)
        {
            if (!byDevice.TryGetValue(deviceName, out MonitorInfo? monitor))
            {
                continue;
            }

            if (_overlays.TryGetValue(deviceName, out BlackoutOverlay? existing))
            {
                existing.ApplyBounds(monitor.Bounds);
                if (!existing.Visible)
                {
                    existing.Show();
                }
            }
            else
            {
                var overlay = new BlackoutOverlay(monitor);
                overlay.MouseDown += OnOverlayMouseDown;
                _overlays[deviceName] = overlay;
                overlay.Show();
            }
        }
    }

    private void OnOverlayMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            SetActive(false);
            return;
        }

        if (e.Button == MouseButtons.Right && _menu is not null)
        {
            _menu.Show(Cursor.Position);
        }
    }

    private void CloseOverlay(string deviceName)
    {
        if (!_overlays.Remove(deviceName, out BlackoutOverlay? overlay))
        {
            return;
        }

        overlay.MouseDown -= OnOverlayMouseDown;
        overlay.Close();
        overlay.Dispose();
    }

    private void CloseAllOverlays()
    {
        foreach (string deviceName in _overlays.Keys.ToList())
        {
            CloseOverlay(deviceName);
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // DisplaySettingsChanged can fire off the UI thread.
        if (_menu is { IsHandleCreated: true } menu && menu.InvokeRequired)
        {
            menu.BeginInvoke(HandleDisplayChange);
        }
        else
        {
            HandleDisplayChange();
        }
    }

    private void HandleDisplayChange()
    {
        RebuildMenu();
        SyncOverlays();
    }

    private void UpdateTrayTooltip(IReadOnlyList<MonitorInfo>? monitors = null)
    {
        monitors ??= MonitorCatalog.GetMonitors();
        int selectedCount = monitors.Count(m => _settings.SelectedDeviceNames.Contains(m.DeviceName));

        string status = _settings.IsActive
            ? $"Active — {selectedCount} monitor(s) blacked out"
            : "Inactive";

        // NotifyIcon.Text max length is 63 characters.
        string tip = $"Black Screens — {status}";
        _trayIcon.Text = tip.Length <= 63 ? tip : tip[..63];
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        Cleanup();
    }

    protected override void ExitThreadCore()
    {
        Cleanup();
        base.ExitThreadCore();
    }

    private void Cleanup()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        Application.ApplicationExit -= OnApplicationExit;
        CloseAllOverlays();

        if (_trayIcon.Visible)
        {
            _trayIcon.Visible = false;
        }

        _trayIcon.Dispose();
        _menu?.Dispose();
        _customIcon?.Dispose();
        _customIcon = null;
    }

    private static Icon CreateTrayIcon()
    {
        // Simple black square with a light border — readable in the tray.
        var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using var fill = new SolidBrush(Color.Black);
            using var border = new Pen(Color.FromArgb(200, 200, 200), 1);
            g.FillRectangle(fill, 1, 1, 14, 14);
            g.DrawRectangle(border, 1, 1, 13, 13);
        }

        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = Icon.FromHandle(hIcon);
        // Clone so we own a managed copy; DestroyIcon the temp handle afterward.
        var clone = (Icon)icon.Clone();
        DestroyIcon(hIcon);
        bitmap.Dispose();
        return clone;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
