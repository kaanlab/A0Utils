﻿using A0Utils.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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

        public static LoggingLevelSwitch LogLevel { get; } = new LoggingLevelSwitch(LevelAlias.Off);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LogLevel)
                .WriteTo.File("a0utils.log", rollOnFileSizeLimit: true, fileSizeLimitBytes: 1024 * 1024)
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
