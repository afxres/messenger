using Messenger.Modules;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// PageClient.xaml 的交互逻辑
    /// </summary>
    public partial class PageClient : Page
    {
        public PageClient()
        {
            InitializeComponent();
            Loaded += _Loaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            PageManager.SetProfilePage(this, uiListbox, ProfileModule.ClientList);
        }
    }
}
