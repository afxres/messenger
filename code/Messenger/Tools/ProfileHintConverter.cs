using System;
using System.Globalization;
using System.Windows.Data;

namespace Messenger.Tools
{
    /// <summary>
    /// 当未读消息过多时 标注 "+" 号
    /// </summary>
    internal class ProfileHintConverter : IValueConverter
    {
        public int MaxShowValue { get; set; } = 9;

        public string OverflowText { get; set; } = "9+";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case int val when val > MaxShowValue:
                    return OverflowText;

                case int val when val < 0 == false:
                    return val.ToString();

                default:
                    return 0.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
