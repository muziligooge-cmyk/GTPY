using System.Net.NetworkInformation;
using System.Linq;

namespace BlueGlassMihomoClient.Services;

public static class PortService
{
    public static bool IsPortOccupied(int port)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
        return tcpListeners.Any(l => l.Port == port);
    }
}
