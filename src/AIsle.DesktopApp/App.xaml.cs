using System;
using System.IO;
using System.Windows;
namespace AIsle.DesktopApp {
    public partial class App : System.Windows.Application {
        public App() {
            var commandLine = Environment.GetCommandLineArgs();
            if (commandLine.Length > 1 && string.Equals(commandLine[1], "--qa-smoke", StringComparison.OrdinalIgnoreCase))
            {
                var outputDirectory = commandLine.Length > 2 ? Path.GetFullPath(commandLine[2]) : Path.Combine(Path.GetTempPath(), "aisle-release-smoke");
                Environment.Exit(AIsle.DesktopApp.Application.ReleaseSmokeRunner.Run(AppContext.BaseDirectory, outputDirectory));
            }

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
