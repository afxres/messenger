using Messenger.Modules;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// PageOption.xaml 的交互逻辑
    /// </summary>
    public partial class PageOption : Page
    {
        public PageOption()
        {
            InitializeComponent();
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == null)
                return;
            if (tag == "exit")
            {
                LinkModule.Shutdown();
                Application.Current.MainWindow.Close();
            }
            else if (tag == "out")
            {
                var mai = Application.Current.MainWindow as Entrance;
                if (mai == null)
                    return;
                LinkModule.Shutdown();
                ProfileModule.Shutdown();
                _ = mai.frame.Navigate(new Connection());
            }
        }
    }
}
