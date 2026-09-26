using System.Threading;
using BlackScreens;

const string MutexName = "Local\\BlackScreens.SingleInstance";

using var mutex = new Mutex(true, MutexName, out bool createdNew);
if (!createdNew)
{
    MessageBox.Show(
        "Black Screens is already running.\nCheck the system tray.",
        "Black Screens",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    return;
}

ApplicationConfiguration.Initialize();
Application.Run(new TrayApplicationContext());
