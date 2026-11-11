
# ASUS Keyboard Backlight

A lightweight background application for the ASUS Vivobook X1504ZA. It automatically turns on the keyboard backlight when user activity is detected and turns it off after a configurable period of inactivity.

## Controls

The application runs in the system tray. Right-click the tray icon to access the following options:

- `Automation enabled` — enable or pause automatic backlight control.
- `Turn off after` — configure the inactivity timeout.
- `Brightness` — adjust the backlight brightness.
- `Turn on now` — enable automation and turn on the backlight immediately.
- `Pause and turn off` — pause automation and turn off the backlight.
- `Exit` — close the application until the next Windows sign-in.

Settings are stored in:

`%LOCALAPPDATA%\AsusKeyboardBacklight\settings.json`

**Available inactivity timeouts:** 10, 20, 30 seconds, 1, 2, 5, and 10 minutes.

## Installation

1. Download the latest release archive.
2. Extract the archive.
3. Run `install.ps1`.
4. Approve the UAC prompt when requested.

The installation script copies the executable to:

`%LOCALAPPDATA%\Programs\AsusKeyboardBacklight`

It also creates a scheduled task named `ASUS Keyboard Backlight` that automatically launches the application with elevated privileges when you sign in to Windows.

## Uninstallation

Run `uninstall.ps1` as administrator.

**Note:** Your user settings will be preserved after uninstallation.

## Compatibility

The application uses the ASUS WMI device `0x00050021`, which has been tested and verified on the supported laptop.

User inactivity is detected using the native Windows Win32 API `GetLastInputInfo`.

### Tested Configuration

- **Laptop:** ASUS Vivobook X1504ZA
- **Driver:** ASUS System Control Interface v3
- **Status:** Confirmed working

### Disclaimer

> [!WARNING]
> The same WMI interface may not be available on other ASUS models.
> Compatibility with other devices is not guaranteed.
> **Use on unsupported models at your own risk.**
