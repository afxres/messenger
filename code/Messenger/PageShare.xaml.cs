using Messenger.Modules;
using Mikodev.Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for Transform.xaml
    /// </summary>
    public partial class PageShare : Page
    {
        public PageShare()
        {
            InitializeComponent();
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == null)
                return;

            if (tag == "clean")
            {
                _ = ShareModule.Remove();
            }
            else if (tag == "change")
            {
                var dfd = new System.Windows.Forms.FolderBrowserDialog();
                if (Directory.Exists(ShareModule.SavePath))
                    dfd.SelectedPath = ShareModule.SavePath;
                if (dfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    ShareModule.SavePath = dfd.SelectedPath;
            }
            else if (tag == "open")
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (Directory.Exists(ShareModule.SavePath) == false)
                            return;
                        using (Process.Start("explorer", "/e," + ShareModule.SavePath)) { }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                });
            }
            else if (tag == "stop")
            {
                ShareModule.Shutdown();
            }
        }
    }
}
