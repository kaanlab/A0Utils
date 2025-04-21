using A0Utils.Wpf.Helpers;
using A0Utils.Wpf.Models;
using A0Utils.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace A0Utils.Wpf.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly FileOperationsService _fileOperationsService;
        private readonly ILogger<MainViewModel> _logger;
        private readonly YandexService _yandexService;
        private readonly DialogService _dialogService;
        private readonly SettingsService _settingsService;

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

            _yandexService.ProgressChanged += (s, progress) => DownloadProgress = progress;

            DownloadPath = _settingsService.GetSettings().DownloadUpdatesPath;
        }


        public static string AssemblyVersion { get { return _assemblyInfo[0]; } }
        public static string AssemblyCopyright { get { return _assemblyInfo[1]; } }
        public static string AssemblyCompany { get { return _assemblyInfo[2]; } }


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

        private string _licenseName;
        public string LicenseName
        {
            get => _licenseName;
            set => SetProperty(ref _licenseName, value);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
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

        private ICommand _checkLicenseCommand;
        public ICommand CheckLicenseCommand
        {
            get
            {
                return _checkLicenseCommand ??= new RelayCommand(CheckLicense);
            }
        }

        private void CheckLicense()
        {
            try
            {
                string path = _settingsService.GetSettings().A0InstallationPath;

                if (_fileOperationsService.IsFolderExist(path))
                {
                    var foundLicense = _fileOperationsService.FindLicFile(path);
                    if (string.IsNullOrEmpty(foundLicense))
                    {
                        Message = "Лицензионный файл не найден. Введите номер лицензии, который указан на ключе или в программе А0 (в меню Справка -> О программе)";
                        LicenseName = string.Empty;
                    }
                    else
                    {
                        LicenseName = foundLicense;
                        Message = "Теперь можно обновить лицензию и получить список доступных обновлений";
                    }
                }
                else
                {
                    _logger.LogError("Программа A0 не установлена или отсутствует доступ к папке {Path}", path);
                    Message = $"Программа A0 не установлена или отсутствует доступ к папке {path}";
                    LicenseName = string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                Message = $"Ошибка: {ex.Message}";
                LicenseName = string.Empty;
            }
        }

        private ICommand _getLicenseCommand;
        public ICommand GetLicenseCommand
        {
            get
            {
                return _getLicenseCommand ??= new AsyncRelayCommand(GetLicense);
            }
        }

        private async Task GetLicense()
        {
            try
            {
                if (string.IsNullOrEmpty(LicenseName))
                {
                    Message = "Введите номер лицензии, который указан на ключе или в программе А0 (в меню Справка -> О программе)";
                    return;
                }

                var licenseResult = await _yandexService.GetLicenses(LicenseName);
                if (licenseResult.IsFailure)
                {
                    Message = licenseResult.Error;
                    return;
                }

                A0LicenseExp = $"Лицензия А0 действительна до: {licenseResult.Value.A0LicenseExpAt:dd.MM.yyyy}";
                PIRLicenseExp = $"Лицензия ПИР действительна до: {licenseResult.Value.PIRLicenseExpAt:dd.MM.yyyy}";
                SubscriptionLicenseExp = licenseResult.Value.SubscriptionLicenseExpAt == default
                    ? "Подписка на базы отсутствует"
                    : $"Подписка на базы: {licenseResult.Value.SubscriptionLicenseExpAt:dd.MM.yyyy}";

                var updatesResult = await _yandexService.GetUpdates();
                if (updatesResult.IsFailure)
                {
                    Message = updatesResult.Error;
                    return;
                }

                UpdateModels = new ObservableCollection<UpdateModel>(updatesResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                Message = $"Ошибка: {ex.Message}";
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
                Message = "Выберите обновления для загрузки";
                return;
            }

            var downloadResult = await _yandexService.DownloadUpdates(selectedUpdates, DownloadPath);
            if (downloadResult.IsFailure)
            {
                Message = downloadResult.Error;
                return;
            }

            Message = "Обновления загружены!";
        }


        private ICommand _saveToCommand;
        public ICommand SaveToCommand
        {
            get
            {
                return _saveToCommand ??= new AsyncRelayCommand(SaveTo);
            }
        }

        private async Task SaveTo()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                DownloadPath = folderDialog.SelectedPath;
                await _settingsService.UpdateDownloadPath(DownloadPath);
            }

            Message = "Путь сохранения обновлений изменен!";
        }

        private ICommand _openSettingsCommand;
        public ICommand OpenSettingsCommand
        {
            get
            {
                return _openSettingsCommand ??= new RelayCommand(_dialogService.ShowSettingsDialog);
            }
        }
    }
}
