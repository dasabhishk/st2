using System.IO;
using System.Windows;
using System.Windows.Controls;
using CMMT.dao;
using CMMT.Helpers;
using CMMT.Models;
using CMMT.Services;
using CMMT.ViewModels;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;

namespace CMMT.UI
{
    /// <summary>
    /// Interaction logic for ProcessCSV.xaml
    /// </summary>
    public partial class ProcessCSV : Page
    {
        private ProcessCSVService _processCSVService;
        private string _mappingconfigFilePath;
        private string _dbconfigFilePath;
        private string _schedulingconfigFilePath;
        private readonly string _csvDelimiter = AppConstants.CSV_DELIMITER;
        List<string>? _validCsvFiles;
        private string _csvType;
        private readonly ITransformationViewService _transformationService;
        private SchedulerConfig _schedulerConfig;
        private MappingConfig _mappingConfig;
        private DatabaseConfig _dbconfig;
        private int selectedArchiveTypeId;
        public ProcessCSV(ITransformationViewService transformationService, ProcessCSVViewModel processCSVViewModel)
        {
            InitializeComponent();
            _transformationService = transformationService;
            InitializeConfigurations();
            DataContext = processCSVViewModel;
        }

        private async void InitializeConfigurations()
        {
            try
            {
                _schedulingconfigFilePath = ConfigFileHelper.GetConfigFilePath("Configuration", "scheduler.json");
                _schedulerConfig = await ConfigFileHelper.LoadAsync<SchedulerConfig>(_schedulingconfigFilePath);

                _mappingconfigFilePath = ConfigFileHelper.GetConfigFilePath("Configuration", "mapping.json");
                _mappingConfig = await ConfigFileHelper.LoadAsync<MappingConfig>(_mappingconfigFilePath);

                _dbconfigFilePath = ConfigFileHelper.GetConfigFilePath("Configuration", "dbconfig.json");
                _dbconfig = await ConfigFileHelper.LoadAsync<DatabaseConfig>(_dbconfigFilePath);
            }
            catch (FileNotFoundException ex)
            {
                LoggingService.LogError("One of the config file needed for processing the csv files is missing, please check", ex, true);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Exceprion reading the config files", ex, true);
            }

            //Use the DataContext to access the ProcessCSVViewModel instance
            if (DataContext is ProcessCSVViewModel processCSVViewModel)
            {
                selectedArchiveTypeId = processCSVViewModel.SelectedArchiveTypeId;
                if (selectedArchiveTypeId <= 0)
                {
                    btnBrowseCsv.IsEnabled = false;
                }
            }

            btnLoadAndValidate.IsEnabled = false;
            _processCSVService = new ProcessCSVService();
        }
        private void CsvTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbCsvType.SelectedItem as ComboBoxItem;
            _csvType = selectedItem?.Content.ToString();

            ResetPage();

            LoggingService.LogInfo($"CSV Type chosen for import is {_csvType}");
        }

        private void BrowseCSVFiles_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.LogInfo("Browse CSV button clicked");
            ResetPage();

            string[] selectedCSVFiles = OpenFileDlg();

            if (selectedCSVFiles == null || selectedCSVFiles.Length == 0)
            {
                LoggingService.LogError("No CSV files selected for import.", null, true);
                return;
            }
            else
            {
                txtCsvFiles.Text = selectedCSVFiles.Aggregate((current, next) => current + ";" + next);
                _validCsvFiles = _processCSVService.CsvHeaderValidation(selectedCSVFiles, _csvType, _csvDelimiter, _mappingConfig);
                if(_validCsvFiles != null && _validCsvFiles.Count > 0)
                {
                    txtCsvFiles.Text = _validCsvFiles.Aggregate((current, next) => current + ";" + next);
                    btnLoadAndValidate.IsEnabled = true;
                } else
                {
                    txtCsvFiles.Text = string.Empty;
                }
            }

        }
        private string[] OpenFileDlg()
        {
            LoggingService.LogInfo("Open File Dialog to browse the csv file");
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "CSV files (*.csv)|*.csv";
                dlg.Title = "Select a CSV file";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() == true)
                {
                    return dlg.FileNames;
                }
                else
                {
                    return Array.Empty<string>();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error opening file dialog", ex,true);
                return Array.Empty<string>();
            }
        }
        private async void LoadAndValidateCsv_Click(object sender, RoutedEventArgs e)
        {
            btnBrowseCsv.IsEnabled = false;
            
            progressBar.Visibility = Visibility.Visible;
            progressText.Visibility = Visibility.Visible;
            btnLoadAndValidate.IsEnabled = false;

            if (_validCsvFiles.IsNullOrEmpty())
            {
                LoggingService.LogWarning("No valid patient or series csv files selected and hence no csv data to load and validate.", true);
                progressBar.Visibility = Visibility.Collapsed;
                progressText.Visibility = Visibility.Collapsed;
                return;
            }

            var progress = new Progress<(int percent, string message)>(tuple =>
            {
                progressBar.Value = tuple.percent;
                progressText.Text = tuple.message;
            });

            /* code to get the dbconfig.json and make the DB connection and check if staging DB exist */
            var plainConnStr = _dbconfig.StagingDatabase.EncryptedConnectionString;
            var decryptedConnStr = SecureStringHelper.Decrypt(plainConnStr);
            var dbLayer = new DBLayer(decryptedConnStr);

            if (!dbLayer.Connect(false))
            {
                LoggingService.LogError("Failed to connect to Staging DB please check the database configuration data", null, true);
                return;
            }

            try
            {
                var job = _schedulerConfig.Jobs.FirstOrDefault(j => j.Name == "ImportJob" && j.Enabled);
                int batchSize = job.BatchSize;

                if (DataContext is ProcessCSVViewModel processCSVViewModel)
                {
                    selectedArchiveTypeId = processCSVViewModel.SelectedArchiveTypeId;
                }

                await Task.Run(async () =>
                {
                    foreach (var map in _mappingConfig.Mappings)
                    {
                        if (_validCsvFiles != null && _validCsvFiles.Count > 0 && _csvType == map.CsvType)
                        {
                            await _processCSVService.ProcessCsvData(_validCsvFiles, map.TableName, _mappingConfig, _transformationService,
                                _csvDelimiter, _dbconfig, batchSize, _csvType, dbLayer, selectedArchiveTypeId, progress);
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error processing CSV files and hence not loaded into staging DB", ex, true);

            }
            finally
            {
                progressBar.Visibility = Visibility.Collapsed;
                progressText.Visibility = Visibility.Collapsed;
                btnBrowseCsv.IsEnabled = true;
                btnLoadAndValidate.IsEnabled = false;
            }
        }

        public void ResetPage()
        {
            LoggingService.LogInfo("Resetting the ProcessCSV page");
            if(txtCsvFiles != null)
            {
                txtCsvFiles.Text = "";
            }
            if(btnLoadAndValidate != null)
            {
                btnLoadAndValidate.IsEnabled = false;
            }
            _validCsvFiles?.Clear();
        }
    }
}
