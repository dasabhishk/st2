using System.Windows;
using CMMT.ViewModels;

namespace CMMT.UI
{
    /// <summary>
    /// Interaction logic for ValueMappingDialog.xaml
    /// </summary>
    public partial class ValueMappingDialog : Window
    {
        public ValueMappingDialog(ValueMappingDialogViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ValueMappingDialogViewModel;
            if (vm != null && vm.Validate())
            {
                DialogResult = true; // Close dialog with success
            }
            else
            {

                MessageBox.Show(vm?.ValidationMessage ?? "Validation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
