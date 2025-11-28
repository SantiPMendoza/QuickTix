using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Mobile;

public static class ApiConfig
{
    public static string GetBaseUrl()
    {
        return DeviceInfo.Platform switch
        {
            //DevicePlatform.Android => "http://10.0.2.2:7137",
            //DevicePlatform.WinUI => "http://localhost:7137",
            _ => "http://10.0.2.2:7137" // iOS simulador también usa 10.0.2.2
        };
    }
}


