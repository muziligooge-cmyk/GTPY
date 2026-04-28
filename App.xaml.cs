using System.Configuration;
using System.Data;
using System.Windows;

namespace BlueGlassMihomoClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 捕获所有未处理的异常
        AppDomain.CurrentDomain.UnhandledException += (s, ev) => 
            System.IO.File.WriteAllText("crash.log", ev.ExceptionObject.ToString());
    }
}

