using A0Utils.Wpf.ViewModels;
using A0Utils.Wpf.Views;

namespace A0Utils.Wpf.Services
{
    public sealed class DialogService
    {
        private readonly SettingsViewModel _settingsViewModel;
        private readonly LicenseViewModel _licenseViewModel;

        public DialogService(SettingsViewModel settingsViewModel, 
            LicenseViewModel licenseViewModel)
        {
            _settingsViewModel = settingsViewModel;
            _licenseViewModel = licenseViewModel;
        }

        public void ShowSettingsDialog()
        {
            SettingsView dialog = new SettingsView
            {
                Title = "Утилиты для А0 :: Настройки",
                DataContext = _settingsViewModel                
            };

            dialog.ShowDialog();
        }

        public void ShowLicenseDialog()
        {
            LicenseView dialog = new LicenseView
            {
                Title = "Утилиты для А0 :: Лицензии",
                DataContext = _licenseViewModel
            };

            dialog.ShowDialog();
        }
    }
}
