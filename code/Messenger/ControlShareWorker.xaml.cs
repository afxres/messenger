using Messenger.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// ControlShareWorker.xaml 的交互逻辑
    /// </summary>
    public partial class ControlShareWorker : UserControl
    {
        public ControlShareWorker()
        {
            InitializeComponent();
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var btn = e.OriginalSource as Button;
            if (btn == null)
                return;
            var dat = btn.DataContext;
            var tag = btn.Tag as string;
            if (dat == null || tag == null)
                return;
            if (tag == "play" && dat is ShareReceiver rec)
                _ = rec.Start();
            else if (tag == "stop" && dat is IDisposable dis)
                dis.Dispose();
            return;
        }
    }
}
