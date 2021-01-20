using Messenger.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Messenger.Tools
{
    /// <summary>
    /// 将大小转化为带单位的字符串
    /// </summary>
    internal class LengthUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Extension.ToUnitEx(System.Convert.ToInt64(value));

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
