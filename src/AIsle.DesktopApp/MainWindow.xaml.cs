using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using AIsle.DesktopApp.Bridge;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Infrastructure;
using Microsoft.Web.WebView2.Core;
using System.Windows.Interop;

namespace AIsle.DesktopApp
{
    public partial class MainWindow : Window
    {
        private const string VirtualHostName = "aisle.local";
        private BridgeMessageProcessor? _bridge;
        private IntPtr _largeIcon;
        private IntPtr _smallIcon;

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            Closed += (_, _) =>
            {
                _bridge?.Dispose();
                if (_largeIcon != IntPtr.Zero) NativeMethods.DestroyIcon(_largeIcon);
                if (_smallIcon != IntPtr.Zero) NativeMethods.DestroyIcon(_smallIcon);
            };
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            SourceInitialized -= OnSourceInitialized;
            var executable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable) || NativeMethods.ExtractIconEx(executable, 0, out _largeIcon, out _smallIcon, 1) == 0) return;
            var handle = new WindowInteropHelper(this).Handle;
            if (_largeIcon != IntPtr.Zero)
            {
                NativeMethods.SendMessage(handle, NativeMethods.WmSetIcon, new IntPtr(NativeMethods.IconBig), _largeIcon);
                NativeMethods.SetClassLongPtr(handle, NativeMethods.GclpHIcon, _largeIcon);
            }
            if (_smallIcon != IntPtr.Zero)
            {
                NativeMethods.SendMessage(handle, NativeMethods.WmSetIcon, new IntPtr(NativeMethods.IconSmall), _smallIcon);
                NativeMethods.SetClassLongPtr(handle, NativeMethods.GclpHIconSmall, _smallIcon);
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try
            {
                await InitializeWebViewAsync();
            }
            catch (Exception exception)
            {
                ShowStartupError(exception);
            }
        }

        private async Task InitializeWebViewAsync()
        {
            var uiRoot = LocalUiAssets.ResolveRoot(AppContext.BaseDirectory);
            var bridgeScriptPath = Path.Combine(uiRoot, "desktop-bridge.js");
            LocalUiAssets.Verify(uiRoot, bridgeScriptPath);

            var defaultProjectPath = DefaultProjectLocation.Ensure(Path.Combine(uiRoot, "default-project.json"));
            var historySeedDirectory = Path.Combine(uiRoot, "history-seed");
            var projects = new ProjectApplicationService(new JsonProjectRepository(), new LayoutValidator());
            _bridge = new BridgeMessageProcessor(projects, defaultProjectPath, historySeedDirectory: historySeedDirectory);

            await StudioWebView.EnsureCoreWebView2Async();
            StudioWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VirtualHostName,
                uiRoot,
                CoreWebView2HostResourceAccessKind.DenyCors);
            StudioWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            var bridgeScript = await File.ReadAllTextAsync(bridgeScriptPath);
            await StudioWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);
            StudioWebView.CoreWebView2.Navigate($"https://{VirtualHostName}/index.html");
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (_bridge == null) return;
            var responseJson = await _bridge.ProcessAsync(e.WebMessageAsJson);
            StudioWebView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }

        private void ShowStartupError(Exception exception)
        {
            StudioWebView.Visibility = Visibility.Collapsed;
            StartupErrorText.Text = DesktopStartupErrors.Message(exception);
            StartupErrorPanel.Visibility = Visibility.Visible;
        }
    }
}
