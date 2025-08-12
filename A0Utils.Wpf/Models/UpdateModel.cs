﻿using A0Utils.Wpf.Helpers;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public LicenseStatus Status { get; set; }

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

    public enum LicenseStatus
    {
        None = 0,
        Warning
    }

    public sealed record YandexUpdateModel
    {
        public string Name { get; private set; }
        public string Key_type { get; private set; }
        public string Category { get; private set; }
        public IEnumerable<string> Urls { get; private set; }
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

            var nsiNames = models.Where(x => x.Category == "Базы НСИ").ToList();
            var nsi = new List<UpdateModel>();
            foreach (var item in nsiNames)
            {
                if(ParseHelpers.FindNsi(data, item.Name))              
                {
                    nsi.Add(item);
                }
                else
                {
                    item.Status = LicenseStatus.Warning;
                    nsi.Add(item);
                }
            }

            var licenseTypeResult = ParseHelpers.FindLicenseType(data);
            if (licenseTypeResult.IsFailure)
            {
                return Result.Failure<IEnumerable<UpdateModel>>(licenseTypeResult.Error);
            }

            var prices = new List<UpdateModel>();
            var pResult = ParseHelpers.FindPrices(data);
            if (pResult.Count > 0)
            {
                foreach (var r in pResult)
                {
                    var priceUpdates = models.Where(x => x.Category == "Справочники цен" && x.Index == r.Name).ToList();
                    if (priceUpdates.Count > 0)
                    {
                        foreach (var priceUpdate in priceUpdates)
                        {
                            if (DateTime.TryParse(priceUpdate.Date, out DateTime date))
                            {
                                foreach (var d in r.Dates)
                                {
                                    if (d.Item1 <= date && date <= d.Item2)
                                    {
                                        prices.Add(priceUpdate);
                                    }
                                    else
                                    {
                                        priceUpdate.Status = LicenseStatus.Warning;
                                        prices.Add(priceUpdate);
                                    }
                                }

                            }
                        }
                    }
                }
            }

            var a0 = models.Where(x => x.Key_type == licenseTypeResult.Value && x.Category == "A0" && licenseInfo.A0LicenseExpAt != default).ToList();
            var pir = models.Where(x => x.Key_type == licenseTypeResult.Value && x.Category == "ПИР" && licenseInfo.PIRLicenseExpAt != default).ToList();
            var tables = models.Where(x => x.Category == "Таблицы").ToList();
            var indexes = models.Where(x => x.Category == "Индексы к ФЕР/ТЕР").ToList();

            var filteredCollection = new List<UpdateModel>();
            filteredCollection.AddRange(a0.UpdateText(licenseInfo.A0LicenseExpAt));
            filteredCollection.AddRange(pir.UpdateText(licenseInfo.PIRLicenseExpAt));
            filteredCollection.AddRange(nsi);
            filteredCollection.AddRange(prices);
            filteredCollection.AddRange(tables);
            filteredCollection.AddRange(indexes);
            return filteredCollection;
        }

        private static List<UpdateModel> UpdateText(this List<UpdateModel> updates, DateTime licenseExpDate)
        {
            foreach (var update in updates)
            {
                if (DateTime.TryParse(update.Date, out DateTime updateDate))
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
