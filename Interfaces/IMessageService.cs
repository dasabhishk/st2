using System.Windows;

namespace CMMT.Services
{
    public interface IMessageService
    {
        void ShowInfo(string message, string title);
        void ShowWarning(string message, string title);
        void ShowError(string message, string title);
    }
}