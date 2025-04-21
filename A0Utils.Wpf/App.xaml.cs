using A0Utils.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;

namespace A0Utils.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            _serviceProvider = ServiceExtensions.ConfigureServices();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("a0utils.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var mainViewModel = _serviceProvider.GetService<MainViewModel>();
            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Title = "Утилиты для А0";
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
