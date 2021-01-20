using Messenger.Modules;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// PageRecent.xaml 的交互逻辑
    /// </summary>
    public partial class PageRecent : Page
    {
        public PageRecent()
        {
            InitializeComponent();
            Loaded += _Loaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            PageManager.SetProfilePage(this, uiListbox, ProfileModule.RecentList);
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == null)
                return;
            if (tag == "clear")
            {
                ProfileModule.RecentList.Clear();
                return;
            }
        }
    }
}
