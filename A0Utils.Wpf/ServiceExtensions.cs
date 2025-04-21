using A0Utils.Wpf.Services;
using A0Utils.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Net.Http.Handlers;

namespace A0Utils.Wpf
{
    public static class ServiceExtensions
    {
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddTransient<ProgressMessageHandler>();

            services
                .AddHttpClient("yandexClient")
                .AddHttpMessageHandler<ProgressMessageHandler>();

            services.AddLogging(builder => builder.AddSerilog());

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();

            services.AddSingleton<DialogService>();  
            services.AddSingleton<FileOperationsService>();
            services.AddSingleton<YandexService>();
            services.AddSingleton<SettingsService>();

            return services.BuildServiceProvider();
        }
    }
}
