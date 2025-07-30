using System.Windows;
using Serilog;

namespace CMMT.Services
{
    public class LoggingService
    {
        public static void LogInfo(string message, bool showMsgBox = false)
        {
            Log.Information(message);
            ShowMessageBox(showMsgBox, message, "Information", MessageBoxImage.Information);
        }

        public static void LogWarning(string message, bool showMsgBox = false)
        {
            Log.Warning(message);
            ShowMessageBox(showMsgBox, message, "Warning", MessageBoxImage.Warning);
        }

        public static void LogError(string message, Exception? ex, bool showMsgBox = false)
        {
            string fullMessage;
            if (ex != null)
            {
                fullMessage = $"{message}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}";
            }
            else
            {
                fullMessage = $"{message}";
            }
            Log.Error(ex, fullMessage);
            ShowMessageBox(showMsgBox, message, "Error", MessageBoxImage.Error);
        }

        private static void ShowMessageBox(bool showMsgBox, string message, string title, MessageBoxImage type)
        {
            if (showMsgBox && !string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, type);
            }
        }

    }
}
