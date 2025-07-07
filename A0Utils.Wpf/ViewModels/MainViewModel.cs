using A0Utils.Wpf.Helpers;
using A0Utils.Wpf.Models;
using A0Utils.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace A0Utils.Wpf.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly FileOperationsService _fileOperationsService;
        private readonly ILogger _logger;
        private readonly YandexService _yandexService;
        private readonly DialogService _dialogService;
        private readonly SettingsService _settingsService;
        private readonly string _a0InstallationPath;

        private static readonly string[] _assemblyInfo = AssemblyHelpers.GetAssemblyInfo();

        public MainViewModel(
            FileOperationsService fileOperationsService,
            ILogger<MainViewModel> logger,
            YandexService yandexService,
            DialogService dialogService,
            SettingsService settingsService)
        {
            _fileOperationsService = fileOperationsService;
            _logger = logger;
            _yandexService = yandexService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            var settings = _settingsService.GetSettings();

            _a0InstallationPath = settings.A0InstallationPath;
            FindAllLicenses();
            DownloadPath = settings.DownloadUpdatesPath;

            _yandexService.DownloadUpdatesProgressChanged += (s, progress) => DownloadProgress = progress;
        }


        public static string AssemblyVersion { get { return _assemblyInfo[0]; } }
        public static string AssemblyCopyright { get { return _assemblyInfo[1]; } }
        public static string AssemblyCompany { get { return _assemblyInfo[2]; } }

        public string SelectedLicense { get; set; }

        private string _downloadPath;
        public string DownloadPath
        {
            get => _downloadPath;
            set => SetProperty(ref _downloadPath, value);
        }

        private int _downloadProgress;
        public int DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
            }
        }

        private string _a0LicenseExp;
        public string A0LicenseExp
        {
            get => _a0LicenseExp;
            set => SetProperty(ref _a0LicenseExp, value);
        }

        private string _pirLicenseExp;
        public string PIRLicenseExp
        {
            get => _pirLicenseExp;
            set => SetProperty(ref _pirLicenseExp, value);
        }

        private string _subscriptionLicenseExp;
        public string SubscriptionLicenseExp
        {
            get => _subscriptionLicenseExp;
            set => SetProperty(ref _subscriptionLicenseExp, value);
        }

        private ObservableCollection<string> _licenses;
        public ObservableCollection<string> Licenses
        {
            get => _licenses;
            set
            {
                _licenses = value;
                OnPropertyChanged(nameof(Licenses));
            }
        }

        private ObservableCollection<UpdateModel> _updateModels;
        public ObservableCollection<UpdateModel> UpdateModels
        {
            get => _updateModels;
            set
            {
                _updateModels = value;
                OnPropertyChanged(nameof(UpdateModels));
            }
        }

        
        private ICommand _getLicenseInfoCommand;
        public ICommand GetLicenseInfoCommand
        {
            get
            {
                return _getLicenseInfoCommand ??= new AsyncRelayCommand(GetLicenseInfo);
            }
        }

        private async Task GetLicenseInfo()
        {
            try
            {
                UpdateModels?.Clear();

                if (string.IsNullOrEmpty(SelectedLicense))
                {
                    MessageDialogHelper.ShowError("Выберите лицензию из списка");
                    return;
                }

                var downloadLicenseResult = await DownloadAndCopyLicense(SelectedLicense);
                if (downloadLicenseResult.IsFailure)
                {
                    MessageDialogHelper.ShowError(downloadLicenseResult.Error);
                    return;
                }

                var licenseResult = await _yandexService.GetLicensesInfo(SelectedLicense);
                if (licenseResult.IsFailure)
                {
                    MessageDialogHelper.ShowError(licenseResult.Error);
                    return;
                }

                A0LicenseExp = licenseResult.Value.A0LicenseExpAt == default 
                    ? "Лицензия А0 отсутствует"
                    : $"Лицензия А0 действительна до: {licenseResult.Value.A0LicenseExpAt:dd.MM.yyyy}";

                PIRLicenseExp = licenseResult.Value.PIRLicenseExpAt == default 
                    ? "Лицензия ПИР отсутствует"
                    : $"Лицензия ПИР действительна до: {licenseResult.Value.PIRLicenseExpAt:dd.MM.yyyy}";

                SubscriptionLicenseExp = licenseResult.Value.SubscriptionLicenseExpAt == default
                    ? "Подписка на базы отсутствует"
                    : $"Подписка на базы: {licenseResult.Value.SubscriptionLicenseExpAt:dd.MM.yyyy}";

                var updatesResult = await _yandexService.GetUpdates();
                if (updatesResult.IsFailure)
                {
                    MessageDialogHelper.ShowError(updatesResult.Error);
                    return;
                }

                var filteredCollectionResult = updatesResult.Value.ApplyFilter(licenseResult.Value);
                if(filteredCollectionResult.IsFailure)
                {                    
                    MessageDialogHelper.ShowError(filteredCollectionResult.Error);
                    return;
                }

                UpdateModels = new ObservableCollection<UpdateModel>(filteredCollectionResult.Value);

                MessageDialogHelper.ShowInfo("Информация о лицензии получена!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                MessageDialogHelper.ShowError($"Ошибка: {ex.Message}");
            }
        }

        private ICommand _downloadSelectedCommand;
        public ICommand DownloadSelectedCommand
        {
            get
            {
                return _downloadSelectedCommand ??= new AsyncRelayCommand(DownloadSelected);
            }
        }

        private async Task DownloadSelected()
        {
            var selectedUpdates = UpdateModels.Where(item => item.IsSelected).ToList();
            if (selectedUpdates.Count == 0)
            {
                MessageDialogHelper.ShowError("Выберите обновления для загрузки");
                return;
            }

            var downloadResult = await _yandexService.DownloadUpdates(selectedUpdates, DownloadPath);
            if (downloadResult.IsFailure)
            {
                MessageDialogHelper.ShowError(downloadResult.Error);
                return;
            }

            MessageDialogHelper.ShowInfo("Обновления загружены!");
        }


        private ICommand _saveToCommand;
        public ICommand SaveToCommand
        {
            get
            {
                return _saveToCommand ??= new RelayCommand(SaveTo);
            }
        }

        private void SaveTo()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var path = Path.Combine(folderDialog.SelectedPath, "A0Updates");
                if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                DownloadPath = path;
                _settingsService.UpdateDownloadPath(DownloadPath);
            }

            MessageDialogHelper.ShowInfo("Путь сохранения обновлений изменен!");
        }

        private ICommand _openSettingsCommand;
        public ICommand OpenSettingsCommand
        {
            get
            {
                return _openSettingsCommand ??= new RelayCommand(_dialogService.ShowSettingsDialog);
            }
        }

        private ICommand _openLicenseCommand;
        public ICommand OpenLicenseCommand
        {
            get
            {
                return _openLicenseCommand ??= new RelayCommand(_dialogService.ShowLicenseDialog);
            }
        }

        private ICommand _refreshLicensesCommand;
        public ICommand RefreshLicensesCommand
        {
            get
            {
                return _refreshLicensesCommand ??= new RelayCommand(FindAllLicenses);
            }
        }

        private void FindAllLicenses()
        {
            try
            {
                if (_fileOperationsService.IsFolderExist(_a0InstallationPath))
                {

                    var foundLicense = _fileOperationsService.FindAllLicFiles(_a0InstallationPath);
                    if (!foundLicense.Any())
                    {
                        MessageDialogHelper.ShowError("Лицензионные файлы не найдены");
                    }
                    else
                    {
                        Licenses = new ObservableCollection<string>(foundLicense.Select(x => x.FileName).Distinct());
                    }
                }
                else
                {
                    _logger.LogError("Программа A0 не установлена или отсутствует доступ к папке {Path}", _a0InstallationPath);
                    MessageDialogHelper.ShowError($"Программа A0 не установлена или отсутствует доступ к папке {_a0InstallationPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                MessageDialogHelper.ShowError($"Ошибка: {ex.Message}");
            }
        }

        private async Task<Result> DownloadAndCopyLicense(string licenseName)
        {
            var licenseResult = await _yandexService.DownloadLicense(licenseName);
            if (licenseResult.IsFailure)
            {
                return Result.Failure(licenseResult.Error);
            }

            var copyResult = licenseResult.Value.CopyToAllFolders(_fileOperationsService, _a0InstallationPath);
            if (copyResult.IsFailure)
            {
                return Result.Failure(copyResult.Error);
            }

            return Result.Success();
        }
    }
}
