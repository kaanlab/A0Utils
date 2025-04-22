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
using System.Windows.Input;

namespace A0Utils.Wpf.ViewModels
{
    public sealed class LicenseViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly FileOperationsService _fileOperationsService;
        private readonly ILogger<LicenseViewModel> _logger;
        private readonly YandexService _yandexService;

        private readonly string _a0InstallationPath;

        public LicenseViewModel(
            SettingsService settingsService,
            FileOperationsService fileOperationsService,
            ILogger<LicenseViewModel> logger,
            YandexService yandexService)
        {
            _settingsService = settingsService;
            _fileOperationsService = fileOperationsService;
            _logger = logger;
            _yandexService = yandexService;

            _a0InstallationPath = _settingsService.GetSettings().A0InstallationPath;
            FindAllLicenses();

            _yandexService.DownloadLicenseProgressChanged += (s, progress) => DownloadProgress = progress;
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

        private string _infoMessage;
        public string InfoMessage
        {
            get => _infoMessage;
            set => SetProperty(ref _infoMessage, value);
        }

        private string _licenseName;
        public string LicenseName
        {
            get => _licenseName;
            set => SetProperty(ref _licenseName, value);
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

        private void FindAllLicenses()
        {
            try
            {
                if (_fileOperationsService.IsFolderExist(_a0InstallationPath))
                {

                    var foundLicense = _fileOperationsService.FindAllLicFiles(_a0InstallationPath);
                    if (!foundLicense.Any())
                    {
                        InfoMessage = "Лицензионные файлы не найдены. Введите номер лицензии, который указан на ключе или в программе А0 (в меню Справка -> О программе)";
                    }
                    else
                    {
                        Licenses = new ObservableCollection<string>(foundLicense.Select(x => x.FileName).Distinct());
                    }
                }
                else
                {
                    _logger.LogError("Программа A0 не установлена или отсутствует доступ к папке {Path}", _a0InstallationPath);
                    InfoMessage = $"Программа A0 не установлена или отсутствует доступ к папке {_a0InstallationPath}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                InfoMessage = $"Ошибка: {ex.Message}";
            }
        }

        private ICommand _updateLicensesCommand;
        public ICommand UpdateLicensesCommand
        {
            get
            {
                return _updateLicensesCommand ??= new AsyncRelayCommand(UpdateLicenses);
            }
        }

        private async Task UpdateLicenses()
        {
            try
            {
                if (Licenses.Count == 0)
                {
                    InfoMessage = $"Программа A0 не установлена или отсутствует доступ к папке {_a0InstallationPath}";
                    return;
                }

                foreach(var license in Licenses)
                {
                    await DownloadAndCopyLicense(license);
                }

                InfoMessage = "Лицензии обновлены!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                InfoMessage = $"Ошибка: {ex.Message}";
            }        
        }


        private ICommand _addLicenseCommand;
        public ICommand AddLicenseCommand
        {
            get
            {
                return _addLicenseCommand ??= new AsyncRelayCommand(AddLicense);
            }
        }

        private async Task AddLicense()
        {
            try
            {
                if (Licenses is null || Licenses.Count == 0) 
                {
                    InfoMessage = $"Программа A0 не установлена или отсутствует доступ к папке {_a0InstallationPath}";
                    return;
                }

                if (string.IsNullOrEmpty(LicenseName))
                {
                    InfoMessage = "Введите номер лицензии, который указан на ключе или в программе А0 (в меню Справка -> О программе)";
                    return;
                }

                if(LicenseName.Length < 8)
                {
                    LicenseName = LicenseName.PadLeft(8, '0');
                }

                var fileNameResult = await DownloadAndCopyLicense(LicenseName); 
                if(fileNameResult.IsFailure)
                {
                    InfoMessage = fileNameResult.Error;
                    return;
                }

                Licenses.Add(fileNameResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                InfoMessage = $"Ошибка: {ex.Message}";
            }
        }

        private async Task<Result<string>> DownloadAndCopyLicense(string licenseName)
        {
            var licenseResult = await _yandexService.DownloadLicense(licenseName);
            if (licenseResult.IsFailure)
            {               
                return licenseResult;
            }

            var licenses = _fileOperationsService.FindAllLicFiles(_a0InstallationPath);
            var destinationDirs = licenses.Select(x => x.DirectoryPath).Distinct();
            _fileOperationsService.CopyToAllFolders(licenseResult.Value, destinationDirs);

            return Path.GetFileName(licenseResult.Value);
        }
    }
}
