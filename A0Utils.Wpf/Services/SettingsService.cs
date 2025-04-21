using A0Utils.Wpf.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace A0Utils.Wpf.Services
{
    public sealed class SettingsService
    {
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
        }


        public async Task UpdateDownloadPath(string path)
        {
            try
            {
                var settings = GetSettings();
                settings.DownloadUpdatesPath = path;
                await Save(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пути загрузки: {message}", ex.Message);
            }
        }

        public async Task SaveWithoutDownloadPath(SettingsModel settings)
        {
            try
            {
                var currentSettings = GetSettings();
                settings.DownloadUpdatesPath = currentSettings.DownloadUpdatesPath;
                await Save(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении настроек: {message}", ex.Message);
            }
        }

        public SettingsModel GetSettings()
        {
            try
            {
                string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Фаил настроек не найден: {jsonPath}", filePath);
                    return new SettingsModel();
                }
                var content = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<SettingsModel>(content);
                if (string.IsNullOrEmpty(settings.DownloadUpdatesPath))
                {
                    settings.DownloadUpdatesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке настроек: {message}", ex.Message);
                return new SettingsModel();
            }
        }

        private async Task Save(SettingsModel settings)
        {
            try
            {
                string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");
                var content = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        await writer.WriteAsync(content);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении настроек: {message}", ex.Message);
            }
        }
    }
}
