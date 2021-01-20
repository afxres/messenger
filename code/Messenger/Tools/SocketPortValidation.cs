using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace Messenger.Tools
{
    internal class SocketPortValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
                return new ValidationResult(false, "输入为空");
            if (int.TryParse(str, out var val))
                if (val >= IPEndPoint.MinPort && val <= IPEndPoint.MaxPort)
                    return new ValidationResult(true, string.Empty);
                else
                    return new ValidationResult(false, $"端口号应当介于 {IPEndPoint.MinPort} 和 {IPEndPoint.MaxPort} 之间");
            return new ValidationResult(false, "输入无效");
        }
    }
}
