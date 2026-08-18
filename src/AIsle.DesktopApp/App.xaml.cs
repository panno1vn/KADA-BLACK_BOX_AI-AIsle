using System;
using System.IO;
using System.Windows;
namespace AIsle.DesktopApp {
    public partial class App : System.Windows.Application {
        public App() {
            this.DispatcherUnhandledException += (s, e) => {
                File.WriteAllText("crash_log.txt", e.Exception.ToString());
                MessageBox.Show(e.Exception.ToString(), "Crash");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                File.WriteAllText("crash_log.txt", e.ExceptionObject.ToString());
            };
        }
    }
}
