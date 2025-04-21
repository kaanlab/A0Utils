using A0Utils.Wpf.ViewModels;
using A0Utils.Wpf.Views;

namespace A0Utils.Wpf.Services
{
    public sealed class DialogService
    {
        private readonly SettingsViewModel _settingsViewModel;

        public DialogService(SettingsViewModel settingsViewModel)
        {
            _settingsViewModel = settingsViewModel;
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
    }
}
