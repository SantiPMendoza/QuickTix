using Microsoft.Maui.Devices;

namespace QuickTix.Mobile;

public static class ApiConfig
{
    public static string GetBaseUrl()
    {
        var platform = DeviceInfo.Platform;

        if (platform == DevicePlatform.Android)
            return "http://10.0.2.2:5137";   // Android Emulator → host PC

        if (platform == DevicePlatform.WinUI)
            return "http://localhost:5137";  // Windows

        if (platform == DevicePlatform.iOS)
            return "http://localhost:5137";  // iOS simulator (normalmente)

        return "http://localhost:5137";
    }
}
