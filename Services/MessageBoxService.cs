using System.Windows;

namespace CMMT.Services
{
    public class MessageBoxService : IMessageService
    {
        public void ShowInfo(string msg, string title) => MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        public void ShowWarning(string msg, string title) => MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        public void ShowError(string msg, string title) => MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}