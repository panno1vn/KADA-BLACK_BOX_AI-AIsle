using System;
using System.IO;

namespace AIsle.DesktopApp.Infrastructure
{
    public static class LocalUiAssets
    {
        public static string ResolveRoot(string applicationBaseDirectory)
        {
            if (string.IsNullOrWhiteSpace(applicationBaseDirectory))
            {
                throw new ArgumentException("Application base directory is required.", nameof(applicationBaseDirectory));
            }

            return Path.GetFullPath(Path.Combine(applicationBaseDirectory, "UI"));
        }

        public static void Verify(string uiRoot, string bridgeScriptPath)
        {
            var requiredFiles = new[]
            {
                Path.Combine(uiRoot, "index.html"),
                Path.Combine(uiRoot, "styles.css"),
                Path.Combine(uiRoot, "app.js"),
                Path.Combine(uiRoot, "default-project.json"),
                bridgeScriptPath
            };

            foreach (var requiredFile in requiredFiles)
            {
                if (!File.Exists(requiredFile))
                {
                    throw new FileNotFoundException($"Required local UI asset was not packaged: {requiredFile}", requiredFile);
                }
            }
        }
    }
}
