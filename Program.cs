using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AsusKeyboardBacklight;

internal static class Program
{
    private const string MutexName = "Local\\AsusKeyboardBacklight.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using Mutex mutex = new(true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new BacklightApplicationContext());
    }
}

internal sealed class BacklightApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly AsusBacklightController _controller = new();
    private readonly AppSettingsStore _settingsStore = new();
    private AppSettings _settings;
    private bool? _lastDesiredState;
    private DateTime _lastHardwareCheck = DateTime.MinValue;
    private DateTime _lastErrorNotice = DateTime.MinValue;
    private int? _lastReportedLevel;

    public BacklightApplicationContext()
    {
        _settings = _settingsStore.Load();
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "ASUS Keyboard Backlight",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleEnabled();

        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => UpdateBacklight();
        _timer.Start();
        UpdateBacklight(forceHardwareCheck: true);
    }

    private ContextMenuStrip BuildMenu()
    {
        ContextMenuStrip menu = new();

        ToolStripMenuItem enabled = new("Automation enabled")
        {
            Checked = _settings.Enabled,
            CheckOnClick = true,
            Name = "Enabled",
        };
        enabled.CheckedChanged += (_, _) =>
        {
            _settings.Enabled = enabled.Checked;
            SaveSettings();
            _lastDesiredState = null;
            UpdateBacklight(forceHardwareCheck: true);
        };
        menu.Items.Add(enabled);

        ToolStripMenuItem timeout = new("Turn off after");
        AddTimeoutItem(timeout, "10 seconds", 10);
        AddTimeoutItem(timeout, "20 seconds", 20);
        AddTimeoutItem(timeout, "30 seconds", 30);
        AddTimeoutItem(timeout, "1 minute", 60);
        AddTimeoutItem(timeout, "2 minutes", 120);
        AddTimeoutItem(timeout, "5 minutes", 300);
        AddTimeoutItem(timeout, "10 minutes", 600);
        menu.Items.Add(timeout);

        ToolStripMenuItem brightness = new("Brightness");
        AddBrightnessItem(brightness, "Low", 1);
        AddBrightnessItem(brightness, "Medium", 2);
        AddBrightnessItem(brightness, "High", 3);
        menu.Items.Add(brightness);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Turn on now", null, (_, _) =>
        {
            _settings.Enabled = true;
            SetEnabledMenuCheck(true);
            SaveSettings();
            SetBacklight(true);
        });
        menu.Items.Add("Pause and turn off", null, (_, _) =>
        {
            _settings.Enabled = false;
            SetEnabledMenuCheck(false);
            SaveSettings();
            SetBacklight(false);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return menu;
    }

    private void AddTimeoutItem(ToolStripMenuItem parent, string label, int seconds)
    {
        ToolStripMenuItem item = new(label) { Checked = _settings.IdleTimeoutSeconds == seconds, Tag = seconds };
        item.Click += (_, _) =>
        {
            _settings.IdleTimeoutSeconds = seconds;
            foreach (ToolStripMenuItem sibling in parent.DropDownItems.OfType<ToolStripMenuItem>())
            {
                sibling.Checked = Equals(sibling.Tag, seconds);
            }
            SaveSettings();
            _lastDesiredState = null;
        };
        parent.DropDownItems.Add(item);
    }

    private void AddBrightnessItem(ToolStripMenuItem parent, string label, int level)
    {
        ToolStripMenuItem item = new(label) { Checked = _settings.BrightnessLevel == level, Tag = level };
        item.Click += (_, _) =>
        {
            _settings.BrightnessLevel = level;
            foreach (ToolStripMenuItem sibling in parent.DropDownItems.OfType<ToolStripMenuItem>())
            {
                sibling.Checked = Equals(sibling.Tag, level);
            }
            SaveSettings();
            if (_settings.Enabled)
            {
                SetBacklight(true);
            }
        };
        parent.DropDownItems.Add(item);
    }

    private void ToggleEnabled()
    {
        _settings.Enabled = !_settings.Enabled;
        SetEnabledMenuCheck(_settings.Enabled);
        SaveSettings();
        _lastDesiredState = null;
        UpdateBacklight(forceHardwareCheck: true);
    }

    private void SetEnabledMenuCheck(bool value)
    {
        if (_notifyIcon.ContextMenuStrip?.Items["Enabled"] is ToolStripMenuItem enabled)
        {
            enabled.Checked = value;
        }
    }

    private void UpdateBacklight(bool forceHardwareCheck = false)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        TimeSpan idleTime = NativeInput.GetIdleTime();
        bool shouldBeOn = idleTime < TimeSpan.FromSeconds(_settings.IdleTimeoutSeconds);
        bool desiredChanged = _lastDesiredState != shouldBeOn;
        bool hardwareCheckDue = forceHardwareCheck || DateTime.UtcNow - _lastHardwareCheck >= TimeSpan.FromSeconds(2);

        if (!desiredChanged && !hardwareCheckDue)
        {
            return;
        }

        try
        {
            int currentLevel = _controller.GetBrightnessLevel();
            _lastHardwareCheck = DateTime.UtcNow;
            if ((shouldBeOn && currentLevel == 0) || (!shouldBeOn && currentLevel != 0) || (desiredChanged && shouldBeOn && currentLevel != _settings.BrightnessLevel))
            {
                _controller.SetBrightnessLevel(shouldBeOn ? _settings.BrightnessLevel : 0);
                currentLevel = shouldBeOn ? _settings.BrightnessLevel : 0;
            }
            _lastDesiredState = shouldBeOn;
            _notifyIcon.Text = shouldBeOn ? "ASUS Keyboard Backlight — on" : "ASUS Keyboard Backlight — idle";
            WriteStatus(currentLevel, idleTime);
        }
        catch (Exception error)
        {
            LogError(error);
            if (DateTime.UtcNow - _lastErrorNotice >= TimeSpan.FromMinutes(5))
            {
                _lastErrorNotice = DateTime.UtcNow;
                _notifyIcon.ShowBalloonTip(5000, "ASUS Keyboard Backlight", "Unable to control the keyboard light. The app will keep retrying.", ToolTipIcon.Warning);
            }
        }
    }

    private void SetBacklight(bool enabled)
    {
        try
        {
            _controller.SetBrightnessLevel(enabled ? _settings.BrightnessLevel : 0);
            _lastDesiredState = enabled;
            _lastHardwareCheck = DateTime.UtcNow;
        }
        catch (Exception error)
        {
            LogError(error);
        }
    }

    private void SaveSettings() => _settingsStore.Save(_settings);

    private void WriteStatus(int hardwareLevel, TimeSpan idleTime)
    {
        if (_lastReportedLevel == hardwareLevel && _lastDesiredState == (hardwareLevel > 0))
        {
            return;
        }

        _lastReportedLevel = hardwareLevel;
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsusKeyboardBacklight");
            Directory.CreateDirectory(directory);
            string json = System.Text.Json.JsonSerializer.Serialize(new
            {
                automationEnabled = _settings.Enabled,
                hardwareLevel,
                idleSeconds = Math.Round(idleTime.TotalSeconds, 1),
                updatedAt = DateTimeOffset.Now,
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(directory, "status.json"), json);
        }
        catch
        {
            // Status is diagnostic only and must not interrupt the controller.
        }
    }

    private static void LogError(Exception error)
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsusKeyboardBacklight");
        Directory.CreateDirectory(directory);
        File.AppendAllText(Path.Combine(directory, "errors.log"), $"{DateTimeOffset.Now:O} {error}\r\n");
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _controller.Dispose();
        base.ExitThreadCore();
    }
}

internal static class NativeInput
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    public static TimeSpan GetIdleTime()
    {
        LastInputInfo info = new() { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        uint elapsedMilliseconds = unchecked((uint)Environment.TickCount - info.Time);
        return TimeSpan.FromMilliseconds(elapsedMilliseconds);
    }
}
