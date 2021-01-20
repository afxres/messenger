using System;
using System.Globalization;
using System.Windows.Data;

namespace Messenger.Tools
{
    internal class StringEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => string.IsNullOrEmpty(value?.ToString());

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
