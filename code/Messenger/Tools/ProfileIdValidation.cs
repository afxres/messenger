using Mikodev.Network;
using System.Globalization;
using System.Windows.Controls;

namespace Messenger.Tools
{
    internal class ProfileIdValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
                return new ValidationResult(false, "输入为空");
            if (int.TryParse(str, out var id))
                if (Links.Id < id && id < Links.DefaultId)
                    return new ValidationResult(true, string.Empty);
                else
                    return new ValidationResult(false, $"编号应大于 {Links.Id} 且小于 {Links.DefaultId}");
            return new ValidationResult(false, "输入无效");
        }
    }
}
