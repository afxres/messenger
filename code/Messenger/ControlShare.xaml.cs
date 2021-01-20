using System;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// ControlShare.xaml 的交互逻辑
    /// </summary>
    public partial class ControlShare : UserControl
    {
        public ControlShare()
        {
            InitializeComponent();
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var btn = e.OriginalSource as Button;
            if (btn == null)
                return;
            var tag = btn.Tag as string;
            var dis = btn.DataContext as IDisposable;
            if (dis == null || tag == null)
                return;
            else if (tag == "stop")
                dis.Dispose();
            return;
        }
    }
}
