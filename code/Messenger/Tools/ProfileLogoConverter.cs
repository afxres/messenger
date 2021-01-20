using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Messenger.Tools
{
    /// <summary>
    /// 为没有头像的用户生成字符 Logo
    /// </summary>
    internal class ProfileLogoConverter : IValueConverter
    {
        private const int _limit = 3;

        private const int _short = 2;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var reg = new Regex(@"^[A-Za-z0-9]+$");
            var str = (value == null) ? string.Empty : value.ToString();
            if (str.Length > _limit && _limit > _short && _short > 0)
                str = str.Substring(0, _short);
            if (str.Length > 1 && reg.IsMatch(str) == false)
                str = str.Substring(0, 1);
            return str.ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
