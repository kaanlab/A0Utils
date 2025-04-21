using A0Utils.Wpf.Converters;
using A0Utils.Wpf.Helpers;
using A0Utils.Wpf.Models;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace A0Utils.Wpf.Services
{
    public sealed class YandexService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;

        private string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private readonly SettingsModel _settings;

        public event EventHandler<int> ProgressChanged;

        public YandexService(
            IHttpClientFactory httpClientFactory, 
            ILogger<YandexService> logger, 
            SettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settingsService = settingsService;

            _settings = _settingsService.GetSettings();
        }

        public async Task<Result<IEnumerable<UpdateModel>>> GetUpdates()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("yandexClient");
                var response = await httpClient.GetAsync($"{_settings.YandexUrl}{_settings.UpdatesUrl}", HttpCompletionOption.ResponseHeadersRead);
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    var yandexItem = await JsonSerializer.DeserializeAsync<YandexItem>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var path = Path.Combine(_settings.DownloadUpdatesPath, yandexItem.Name);

                    using (var responseStream = await httpClient.GetStreamAsync(yandexItem.File))
                    {
                        using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                        {
                            await responseStream.CopyToAsync(fileStream);
                        }
                    }

                    string jsonContent = File.ReadAllText(path);
                    var updates = JsonSerializer.Deserialize<IEnumerable<UpdateModel>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Result.Success(updates);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при получении обновленй {Error}", ex);
                return Result.Failure<IEnumerable<UpdateModel>>($"Ошибка при получении обновленй");
            }
        }

        public async Task<Result> DownloadUpdates(IEnumerable<UpdateModel> updates, string downloadPath)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("yandexClient");
                foreach (var update in updates)
                {
                    foreach (var url in update.Urls)
                    {
                        var response = await httpClient.GetAsync($"{_settings.YandexUrl}{url}", HttpCompletionOption.ResponseHeadersRead);
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            var yandexItem = await JsonSerializer.DeserializeAsync<YandexItem>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            var path = Path.Combine(downloadPath, yandexItem.Name);

                            using (var responseStream = await httpClient.GetStreamAsync(yandexItem.File))
                            {
                                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: false))
                                {
                                    byte[] buffer = new byte[81920];
                                    long totalBytes = yandexItem.Size;
                                    long totalRead = 0;
                                    int bytesRead;

                                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        totalRead += bytesRead;

                                        if (totalBytes > 0)
                                        {
                                            ProgressChanged?.Invoke(this, (int)((totalRead * 100) / totalBytes));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return Result.Success();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при скачивании обновлений: {Error}", ex.Message);
                return Result.Failure("Ошибка при скачивании обновлений");
            }
        }

        public async Task<Result<LicensesExpModel>> GetLicenses(string licenseName)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("yandexClient");
                var yandexResourceResult = await GetLicenseResource(httpClient, 1000);
                if (yandexResourceResult.IsFailure)
                {
                    return Result.Failure<LicensesExpModel>(yandexResourceResult.Error);
                }

                var licenseResult = await DownloadLicenseFile(licenseName, yandexResourceResult.Value, httpClient);
                if (licenseResult.IsFailure)
                {
                    return Result.Failure<LicensesExpModel>(licenseResult.Error);
                }

                var descriptionResult = await DownloadLicenseDescriptionFile(licenseName, yandexResourceResult.Value, httpClient);
                if (descriptionResult.IsFailure)
                {
                    return Result.Failure<LicensesExpModel>(descriptionResult.Error);
                }

                var subscriptionResult = await GetSubscription(licenseName, httpClient);
                if (subscriptionResult.IsFailure)
                {
                    return Result.Failure<LicensesExpModel>(subscriptionResult.Error);
                }

                return new LicensesExpModel
                {
                    A0LicenseExpAt = descriptionResult.Value.A0LicenseExp,
                    PIRLicenseExpAt = descriptionResult.Value.PIRLicenseExp,
                    SubscriptionLicenseExpAt = subscriptionResult.Value
                };

            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при получении лицензии: {Error}", ex);
                return Result.Failure<LicensesExpModel>("Ошибка при получении лицензии");
            }
        }

        private async Task<Result<DateTime>> GetSubscription(string licenseName, HttpClient httpClient)
        {
            try
            {
                if (licenseName.EndsWith(".ISL"))
                {
                    licenseName = licenseName.Substring(0, licenseName.Length - 4);
                }

                var response = await httpClient.GetAsync($"{_settings.YandexUrl}{_settings.SubscriptionUrl}", HttpCompletionOption.ResponseHeadersRead);
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    var yandexItem = await JsonSerializer.DeserializeAsync<YandexItem>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var path = Path.Combine(AppPath, yandexItem.Name);
                    using (var responseStream = await httpClient.GetStreamAsync(yandexItem.File))
                    {
                        using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                        {
                            await responseStream.CopyToAsync(fileStream);
                        }
                    }

                    string jsonContent = File.ReadAllText(path);
                    var subscriptions = JsonSerializer.Deserialize<IEnumerable<SubscriptionModel>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonDateTimeConverter() } });
                    var subscription = subscriptions.FirstOrDefault(x => x.Number == licenseName);
                    if (subscription is null)
                    {
                        return default;
                    }

                    return subscription.Date;

                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при получении файла подписки {Error}", ex);
                return Result.Failure<DateTime>("Ошибка при получении файла подписки");
            }
        }

        private async Task<Result<YandexEmbedded>> GetLicenseResource(HttpClient httpClient, int limit)
        {
            try
            {
                var response = await httpClient.GetAsync($"{_settings.YandexUrl}{_settings.LicenseUrl}&limit={limit}", HttpCompletionOption.ResponseHeadersRead);
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    var yandexResource = await JsonSerializer.DeserializeAsync<YandexResource>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return yandexResource._Embedded;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при получении списка лицензий {Error}", ex);
                return Result.Failure<YandexEmbedded>($"Ошибка при получении списка лицензий");
            }
        }

        private async Task<Result> DownloadLicenseFile(string licenseName, YandexEmbedded yandexResource, HttpClient httpClient)
        {
            try
            {
                if (!licenseName.EndsWith(".ISL"))
                {
                    licenseName = licenseName + ".ISL";
                }

                if (licenseName.Split('.').Length > 2)
                {
                    return Result.Failure("Лицензия должна быть в формате .ISL");
                }

                var license = yandexResource.Items.FirstOrDefault(x => x.Name == licenseName);
                if (license == null)
                {
                    _logger.LogError($"Лицензия {licenseName} не найдена");
                    return Result.Failure($"Лицензия {licenseName} не найдена");
                }

                var path = Path.Combine(_settings.A0InstallationPath, license.Name);

                using (var responseStream = await httpClient.GetStreamAsync(license.File))
                {
                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        await responseStream.CopyToAsync(fileStream);
                    }
                }

                return Result.Success();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при скачивании лицензии: {Error}", ex.Message);
                return Result.Failure("Ошибка при скачивании лицензии");
            }

        }

        private async Task<Result<(DateTime A0LicenseExp, DateTime PIRLicenseExp)>> DownloadLicenseDescriptionFile(string licenseName, YandexEmbedded yandexResource, HttpClient httpClient)
        {
            try
            {
                if (licenseName.EndsWith(".ISL"))
                {
                    licenseName = licenseName.Substring(0, licenseName.Length - 4) + ".ild";
                }
                else
                {
                    licenseName = licenseName + ".ild";
                }

                if (licenseName.Split('.').Length > 2)
                {
                    return Result.Failure<(DateTime A0LicenseExp, DateTime PIRLicenseExp)>("Фаил должен быть в формате .ild");
                }

                var description = yandexResource.Items.FirstOrDefault(x => x.Name == licenseName);
                if (description == null)
                {
                    _logger.LogError($"Фаил с описанием лицензий {licenseName} не найден");
                    return Result.Failure<(DateTime A0LicenseExp, DateTime PIRLicenseExp)>($"Фаил с описанием лицензий {licenseName} не найден");
                }

                var path = Path.Combine(AppPath, description.Name);
                using (var responseStream = await httpClient.GetStreamAsync(description.File))
                {
                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true))
                    {
                        await responseStream.CopyToAsync(fileStream);
                    }
                }

                string content = File.ReadAllText(path, Encoding.GetEncoding("windows-1251"));

                var a0LicenseResult = ParseHelpers.FindA0LicenseExp(content);
                if (a0LicenseResult.IsFailure)
                {
                    return Result.Failure<(DateTime A0LicenseExp, DateTime PIRLicenseExp)>(a0LicenseResult.Error);
                }

                var pirLicenseResult = ParseHelpers.FindPIRLicenseExp(content);
                if (pirLicenseResult.IsFailure)
                {
                    return Result.Failure<(DateTime A0LicenseExp, DateTime PIRLicenseExp)>(pirLicenseResult.Error);
                }

                return (a0LicenseResult.Value, pirLicenseResult.Value);

            }
            catch (System.Exception ex)
            {
                _logger.LogError("Ошибка при скачивании фаила с описанием лицензий: {Error}", ex);
                return Result.Failure<(DateTime A0LicenseExp, DateTime PIRLicenseExp)>("Ошибка при скачивании фаила с описанием лицензий");
            }
        }
    }

    public class YandexResource
    {
        public string Name { get; set; }
        public YandexEmbedded _Embedded { get; set; }
    }

    public class YandexEmbedded
    {
        public YandexItem[] Items { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int Total { get; set; }
    }

    public class YandexItem
    {
        public string Name { get; set; }
        public string File { get; set; }
        public long Size { get; set; }
        public long Revision { get; set; }
    }
}
