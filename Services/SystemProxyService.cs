using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace BlueGlassMihomoClient.Services;

public static class SystemProxyService
{
    [DllImport("wininet.dll")]
    public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    public const int INTERNET_OPTION_REFRESH = 37;

    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    public static void SetProxy(string server)
    {
        try
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true)!;
            registry.SetValue("ProxyEnable", 1);
            registry.SetValue("ProxyServer", server);
            Refresh();
            LogService.LogApp($"系统代理已开启: {server}");
        }
        catch (Exception ex)
        {
            LogService.LogApp($"设置系统代理失败: {ex.Message}");
        }
    }

    public static void UnsetProxy()
    {
        try
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true)!;
            registry.SetValue("ProxyEnable", 0);
            Refresh();
            LogService.LogApp("系统代理已关闭");
        }
        catch (Exception ex)
        {
            LogService.LogApp($"关闭系统代理失败: {ex.Message}");
        }
    }

    private static void Refresh()
    {
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
    }
}
