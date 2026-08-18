using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using AIsle.DesktopApp.Bridge;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Infrastructure;
using Microsoft.Web.WebView2.Core;

namespace AIsle.DesktopApp
{
    public partial class MainWindow : Window
    {
        private const string VirtualHostName = "aisle.local";
        private BridgeMessageProcessor? _bridge;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (_, _) => _bridge?.Dispose();
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
            var projects = new ProjectApplicationService(new JsonProjectRepository(), new LayoutValidator());
            _bridge = new BridgeMessageProcessor(projects, defaultProjectPath);

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
