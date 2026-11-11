using System.Management;

namespace AsusKeyboardBacklight;

internal sealed class AsusBacklightController : IDisposable
{
    private const uint KeyboardBacklightDeviceId = 0x00050021;
    private ManagementObject? _device;

    public int GetBrightnessLevel()
    {
        ManagementObject device = GetDevice();
        using ManagementBaseObject input = device.GetMethodParameters("DSTS");
        input["Device_ID"] = KeyboardBacklightDeviceId;
        using ManagementBaseObject output = device.InvokeMethod("DSTS", input, null);
        uint status = Convert.ToUInt32(output["device_status"]);
        return (int)(status & 0x7F);
    }

    public void SetBrightnessLevel(int level)
    {
        if (level is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        ManagementObject device = GetDevice();
        using ManagementBaseObject input = device.GetMethodParameters("DEVS");
        input["Device_ID"] = KeyboardBacklightDeviceId;
        input["Control_status"] = (uint)(0x80 | level);
        using ManagementBaseObject output = device.InvokeMethod("DEVS", input, null);
        uint result = Convert.ToUInt32(output["result"]);
        if (result == 0)
        {
            throw new InvalidOperationException("ASUS firmware rejected the keyboard backlight command.");
        }
    }

    private ManagementObject GetDevice()
    {
        if (_device is not null)
        {
            return _device;
        }

        ManagementScope scope = new("\\\\.\\root\\wmi");
        scope.Connect();
        using ManagementClass managementClass = new(scope, new ManagementPath("AsusAtkWmi_WMNB"), null);
        foreach (ManagementObject instance in managementClass.GetInstances().Cast<ManagementObject>())
        {
            _device = instance;
            break;
        }

        return _device ?? throw new InvalidOperationException("ASUS WMI keyboard controller was not found.");
    }

    public void Dispose()
    {
        _device?.Dispose();
        _device = null;
    }
}
