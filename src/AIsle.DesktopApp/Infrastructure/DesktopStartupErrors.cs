using System;

namespace AIsle.DesktopApp.Infrastructure
{
    public static class DesktopStartupErrors
    {
        public static string Message(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            return "WebView2 hoặc gói giao diện local chưa sẵn sàng. "
                + "Hãy cài Microsoft Edge WebView2 Runtime và chạy lại ứng dụng.\n\n"
                + exception.Message;
        }
    }
}
