using System;
using System.IO;
using System.Text.Json;

namespace TaskbarDock.Diagnostics
{
    public static class ErrorReporter
    {
        public static string GenerateDiagnosticSummary()
        {
            var summary = new
            {
                AppVersion = "1.0.0",
                OSVersion = Environment.OSVersion.VersionString,
                Is64BitOS = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                DotNetVersion = Environment.Version.ToString(),
                LogDirectory = Logger.LogDirectory,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
