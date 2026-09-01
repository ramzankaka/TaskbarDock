using System;
using System.IO;
using System.Text;

namespace TaskbarDock.Diagnostics
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Recovery
    }

    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _logDir;
        private static string _currentLogFile;

        static Logger()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDir = Path.Combine(localAppData, "TaskbarDock", "Logs");
                Directory.CreateDirectory(_logDir);
                _currentLogFile = Path.Combine(_logDir, $"dock_{DateTime.UtcNow:yyyyMMdd}.log");
                CleanOldLogs();
            }
            catch
            {
                _logDir = AppDomain.CurrentDomain.BaseDirectory;
                _currentLogFile = Path.Combine(_logDir, "dock.log");
            }
        }

        public static string LogDirectory => _logDir;
        public static string CurrentLogFile => _currentLogFile;

        public static void Log(LogLevel level, string message, Exception? ex = null)
        {
            try
            {
                lock (_lock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var sb = new StringBuilder();
                    sb.Append($"[{timestamp}] [{level.ToString().ToUpperInvariant().PadRight(8)}] {message}");
                    if (ex != null)
                    {
                        sb.AppendLine();
                        sb.Append($"Exception: {ex.GetType().FullName}: {ex.Message}");
                        sb.AppendLine();
                        sb.Append(ex.StackTrace);
                    }
                    sb.AppendLine();

                    File.AppendAllText(_currentLogFile, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Fallback: Avoid crashing the app if logging fails
            }
        }

        public static void Debug(string message) => Log(LogLevel.Debug, message);
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warn(string message, Exception? ex = null) => Log(LogLevel.Warning, message, ex);
        public static void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
        public static void Recovery(string message) => Log(LogLevel.Recovery, message);

        private static void CleanOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(_logDir, "dock_*.log");
                var cutoff = DateTime.UtcNow.AddDays(-7);
                foreach (var f in files)
                {
                    if (File.GetCreationTimeUtc(f) < cutoff)
                    {
                        File.Delete(f);
                    }
                }
            }
            catch { }
        }
    }
}
