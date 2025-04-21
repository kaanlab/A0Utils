using A0Utils.Wpf.Models;
using A0Utils.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace A0Utils.Wpf.ViewModels
{
    public sealed class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;

            LoadSettings();
        }

        private string _a0InstallationPath;
        public string A0InstallationPath
        {
            get => _a0InstallationPath;
            set => SetProperty(ref _a0InstallationPath, value);
        }

        private string _yandexUrl;
        public string YandexUrl
        {
            get => _yandexUrl;
            set => SetProperty(ref _yandexUrl, value);
        }

        private string _licenseUrl;
        public string LicenseUrl
        {
            get => _licenseUrl;
            set => SetProperty(ref _licenseUrl, value);
        }

        private string _subscriptionUrl;
        public string SubscriptionUrl
        {
            get => _subscriptionUrl;
            set => SetProperty(ref _subscriptionUrl, value);
        }

        private string _updatesUrl;
        public string UpdatesUrl
        {
            get => _updatesUrl;
            set => SetProperty(ref _updatesUrl, value);
        }

        private ICommand _saveSettingsCommand;
        public ICommand SaveSettingsCommand
        {
            get
            {
                return _saveSettingsCommand ??= new AsyncRelayCommand(SaveSettings);
            }
        }

        private async Task SaveSettings()
        {
            var settings = new SettingsModel
            {
                A0InstallationPath = A0InstallationPath,
                YandexUrl = YandexUrl,
                LicenseUrl = LicenseUrl,
                SubscriptionUrl = SubscriptionUrl,
                UpdatesUrl = UpdatesUrl               
            };

            await _settingsService.SaveWithoutDownloadPath(settings);
        }

        private ICommand _saveA0PathCommand;
        public ICommand SaveA0PathCommand
        {
            get
            {
                return _saveA0PathCommand ??= new RelayCommand(SaveA0Path);
            }
        }

        private void SaveA0Path()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                A0InstallationPath = folderDialog.SelectedPath;
            }
        }


        private void LoadSettings()
        {
            var settings = _settingsService.GetSettings();

            A0InstallationPath = settings.A0InstallationPath;
            YandexUrl = settings.YandexUrl;
            LicenseUrl = settings.LicenseUrl;
            SubscriptionUrl = settings.SubscriptionUrl;
            UpdatesUrl = settings.UpdatesUrl;
        }
    }
}
