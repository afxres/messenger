using Messenger.Models;
using Messenger.Modules;
using Mikodev.Network;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// ProfileFrame.xaml 的交互逻辑
    /// </summary>
    public partial class PageFrame : Page
    {
        private readonly PageProfile _profPage = new PageProfile();

        public PageFrame()
        {
            InitializeComponent();
            Loaded += _Loaded;
            Unloaded += _Unloaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            _profPage.uiLeftFrame.Content = new PageClient();
            uiFrame.Content = _profPage;

            var act = (Action)delegate
            {
                uiSwitchRadio.IsChecked = false;
                uiMainBorder.Visibility = Visibility.Collapsed;
            };
            uiMainBorder.MouseDown += (s, arg) => act.Invoke();
            uiMainBorder.TouchDown += (s, arg) => act.Invoke();
            HistoryModule.Receive += _HistoryReceiving;
        }

        private void _Unloaded(object sender, RoutedEventArgs e)
        {
            HistoryModule.Receive -= _HistoryReceiving;
        }

        /// <summary>
        /// 如果 Frame 不为用户列表 则消息提示应当存在
        /// </summary>
        private void _HistoryReceiving(object sender, LinkEventArgs<Packet> e)
        {
            if (uiFrame.Content == _profPage)
                return;
            e.Cancel = true;
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as RadioButton)?.Tag as string;
            if (tag == null)
                return;

            var cur = uiFrame;
            var ctx = default(Page);
            if (tag == "self")
                ctx = new Shower();
            else if (tag == "share")
                ctx = new PageShare();
            else if (tag == "setting")
                ctx = new PageOption();
            else if (tag != "switch" && cur.Content != _profPage)
                ctx = _profPage;

            if (ctx != null)
                cur.Content = ctx;

            // Context 属性会延迟生效, 因此只能与 ctx 比较
            if (ctx == _profPage)
            {
                var sco = ProfileModule.Inscope;
                if (sco != null)
                    sco.Hint = 0;
                _profPage.uiSearchBox.Text = null;
            }

            var lef = _profPage.uiLeftFrame;
            if (tag == "user")
                lef.Content = new PageClient();
            else if (tag == "group")
                lef.Content = new PageGroups();
            else if (tag == "recent")
                lef.Content = new PageRecent();

            if (uiNavigateGrid.Width > uiNavigateGrid.MinWidth)
                uiSwitchRadio.IsChecked = false;

            // 清空导航历史
            while (NavigationService.CanGoBack)
                _ = NavigationService.RemoveBackEntry();

            uiMainBorder.Visibility = uiSwitchRadio.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
