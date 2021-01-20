using Messenger.Models;
using System.ComponentModel;

namespace Messenger.Modules
{
    /// <summary>
    /// 管理用户界面设置
    /// </summary>
    internal class SettingModule : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private const string _KeyCtrlEnter = "hotkey-control-enter";

        private SettingModule() { }

        private static readonly SettingModule s_ins = new SettingModule();

        public static SettingModule Instance => s_ins;

        private bool _ctrlenter = false;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool UseControlEnter
        {
            get => _ctrlenter;
            set
            {
                var changing = PropertyChanging;
                if (changing != null)
                {
                    changing.Invoke(this, new PropertyChangingEventArgs(nameof(UseEnter)));
                    changing.Invoke(this, new PropertyChangingEventArgs(nameof(UseControlEnter)));
                }

                if (_ctrlenter == value)
                    return;
                _ctrlenter = value;
                EnvironmentModule.Update(_KeyCtrlEnter, value.ToString());

                var changed = PropertyChanged;
                if (changed != null)
                {
                    changed.Invoke(this, new PropertyChangedEventArgs(nameof(UseEnter)));
                    changed.Invoke(this, new PropertyChangedEventArgs(nameof(UseControlEnter)));
                }
            }
        }

        public bool UseEnter
        {
            get => UseControlEnter == false;
            set => UseControlEnter = (value == false);
        }

        [Loader(8, LoaderFlags.OnLoad)]
        public static void Load()
        {
            var str = EnvironmentModule.Query(_KeyCtrlEnter, false.ToString());
            if (str != null && bool.TryParse(str, out var res))
                s_ins._ctrlenter = res;
            return;
        }
    }
}
