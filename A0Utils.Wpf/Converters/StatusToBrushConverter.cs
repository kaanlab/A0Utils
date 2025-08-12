using A0Utils.Wpf.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace A0Utils.Wpf.Converters
{
    public sealed class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LicenseStatus status)
            {
                return status switch
                {
                    LicenseStatus.None => Brushes.Transparent,
                    LicenseStatus.Warning => Brushes.LightCoral,
                    _ => Brushes.Transparent
                };
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
