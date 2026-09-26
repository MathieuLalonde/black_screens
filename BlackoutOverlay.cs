using System.Runtime.InteropServices;

namespace BlackScreens;

/// <summary>
/// Borderless topmost black window covering one monitor.
/// Uses WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE so it stays out of Alt+Tab and does not steal focus.
/// Hides the cursor after 5 seconds of no movement while the pointer is over the overlay.
/// </summary>
public sealed class BlackoutOverlay : Form
{
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int CursorHideDelayMs = 5000;

    private static Cursor? _blankCursor;
    private static Icon? _blankIcon;

    private readonly System.Windows.Forms.Timer _cursorHideTimer;

    public string DeviceName { get; }

    public BlackoutOverlay(MonitorInfo monitor)
    {
        DeviceName = monitor.DeviceName;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        Bounds = monitor.Bounds;
        TopMost = true;
        Cursor = Cursors.Default;

        _cursorHideTimer = new System.Windows.Forms.Timer
        {
            Interval = CursorHideDelayMs,
        };
        _cursorHideTimer.Tick += OnCursorHideTimerTick;

        MouseMove += OnOverlayMouseMove;
        MouseEnter += OnOverlayMouseEnter;
        MouseLeave += OnOverlayMouseLeave;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WsExToolWindow | WsExNoActivate;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Keep above other windows without activating.
        SetWindowPos(
            Handle,
            HWND_TOPMOST,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    public void ApplyBounds(Rectangle bounds)
    {
        Bounds = bounds;
        if (IsHandleCreated)
        {
            SetWindowPos(
                Handle,
                HWND_TOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
    }

    private void OnOverlayMouseEnter(object? sender, EventArgs e)
    {
        ShowCursorAndRestartTimer();
    }

    private void OnOverlayMouseMove(object? sender, MouseEventArgs e)
    {
        ShowCursorAndRestartTimer();
    }

    private void OnOverlayMouseLeave(object? sender, EventArgs e)
    {
        _cursorHideTimer.Stop();
        Cursor = Cursors.Default;
    }

    private void OnCursorHideTimerTick(object? sender, EventArgs e)
    {
        _cursorHideTimer.Stop();
        Cursor = GetBlankCursor();
    }

    private void ShowCursorAndRestartTimer()
    {
        if (!ReferenceEquals(Cursor, Cursors.Default))
        {
            Cursor = Cursors.Default;
        }

        _cursorHideTimer.Stop();
        _cursorHideTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cursorHideTimer.Stop();
            _cursorHideTimer.Tick -= OnCursorHideTimerTick;
            _cursorHideTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Cursor GetBlankCursor()
    {
        if (_blankCursor is not null)
        {
            return _blankCursor;
        }

        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
        }

        // Keep Icon alive for process lifetime; Cursor(IntPtr) does not own the handle.
        _blankIcon = Icon.FromHandle(bitmap.GetHicon());
        _blankCursor = new Cursor(_blankIcon.Handle);
        bitmap.Dispose();
        return _blankCursor;
    }

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
