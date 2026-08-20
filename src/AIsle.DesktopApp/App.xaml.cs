using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
namespace AIsle.DesktopApp {
    public partial class App : System.Windows.Application {
        public App() {
            NativeMethods.SetCurrentProcessExplicitAppUserModelID("AIsle.SimulationStudio");
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

    internal static class NativeMethods
    {
        internal const uint WmSetIcon = 0x0080;
        internal const int IconSmall = 0;
        internal const int IconBig = 1;
        internal const int GclpHIcon = -14;
        internal const int GclpHIconSmall = -34;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint ExtractIconEx(string fileName, int iconIndex, out IntPtr largeIcon, out IntPtr smallIcon, uint iconCount);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
        internal static extern IntPtr SetClassLongPtr(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr icon);
    }
}
