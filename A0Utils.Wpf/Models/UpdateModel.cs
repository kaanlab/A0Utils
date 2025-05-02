using A0Utils.Wpf.Helpers;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Packaging;
using System.Linq;

namespace A0Utils.Wpf.Models
{
    public class UpdateModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Key_type { get; set; } = string.Empty;
        public string Category { get; set; }
        public IEnumerable<string> Urls { get; set; }
        public string Index { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

    public sealed record YandexUpdateModel
    {
        public string Name { get; private set; }
        public string Key_type { get; private set; }
        public string Category { get; private set; }
        public IEnumerable<string> Urls { get;  private set; }
        public string Index { get; private set; } 
        public string Date { get; private set; }

        public YandexUpdateModel(string name, string key_type, string category, IEnumerable<string> urls, string index, string date)
        {
            Name = name;
            Key_type = key_type;
            Category = category;
            Urls = urls;
            Index = index;
            Date = date;
        }
    }

    public static class UpdateModelExtensions
    {
        public static IEnumerable<UpdateModel> MapToUpdateModels(this IEnumerable<YandexUpdateModel> yandexUpdates)
        {
            return yandexUpdates.Select(y => new UpdateModel
            {
                Name = y.Name,
                Key_type = y.Key_type,
                Category = y.Category,
                Urls = y.Urls,
                Index = y.Index,
                Date = y.Date
            });
        }

        public static Result<IEnumerable<UpdateModel>> ApplyFilter(this IEnumerable<UpdateModel> models, LicenseInfoModel licenseInfo)
        {
            string[] data = licenseInfo.Content.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

            var nsiNames = models.Where(x => x.Category == "Базы НСИ").Select(x => x.Name).ToList();
            var nisFilter = new List<string>();
            foreach (var item in nsiNames)
            {
                var result = ParseHelpers.FindNsi(data, item);
                if (!string.IsNullOrEmpty(result))
                {
                    nisFilter.Add(item);
                }
            }

            var licenseTypeResult = ParseHelpers.FindLicenseType(data);
            if (licenseTypeResult.IsFailure)
            {
                return Result.Failure<IEnumerable<UpdateModel>>(licenseTypeResult.Error);
            }

            var pricesNames = models.Where(x => x.Category == "Справочники цен").Select(x => x.Index).ToList();
            var pricesFilter = new HashSet<string>();
            foreach (var item in pricesNames)
            {
                var result = ParseHelpers.FindPrices(data, item);
                if (!string.IsNullOrEmpty(result))
                {
                    pricesFilter.Add(item);
                }
            }

            var a0 = models.Where(x => x.Key_type == licenseTypeResult.Value && x.Category == "A0" ).ToList();
            var pir = models.Where(x => x.Key_type == licenseTypeResult.Value && x.Category == "ПИР").ToList();
            var nsi = models.Where(x => x.Category == "Базы НСИ" && nisFilter.Contains(x.Name)).ToList();
            var prices = models.Where(x => x.Category == "Справочники цен" && pricesFilter.Contains(x.Index)).ToList();
            var tables = models.Where(x => x.Category == "Таблицы").ToList();

            var filteredCollection = new List<UpdateModel>();
            filteredCollection.AddRange(a0.UpdateText(licenseInfo.A0LicenseExpAt));
            filteredCollection.AddRange(pir.UpdateText(licenseInfo.PIRLicenseExpAt));
            filteredCollection.AddRange(nsi);
            filteredCollection.AddRange(prices);
            filteredCollection.AddRange(tables);
            return filteredCollection;
        }

        private static List<UpdateModel> UpdateText(this List<UpdateModel> updates, DateTime licenseExpDate)
        {
            foreach (var update in updates)
            {
                if(DateTime.TryParse(update.Date, out DateTime updateDate))
                {
                    if (updateDate > licenseExpDate)
                    {
                        update.Name += " (Лицензия истекла)";
                    }
                }
            }

            return updates;
        }
    }
}
