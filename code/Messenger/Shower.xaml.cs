using Messenger.Modules;
using Mikodev.Logger;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for Shower.xaml
    /// </summary>
    public partial class Shower : Page
    {
        public Shower()
        {
            InitializeComponent();
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == null)
                return;
            if (tag == "apply")
            {
                ProfileModule.SetProfile(uiNameBox.Text, uiSignBox.Text);
            }
            else if (tag == "image")
            {
                var ofd = new System.Windows.Forms.OpenFileDialog() { Filter = "位图文件|*.bmp;*.png;*.jpg" };
                if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                try
                {
                    ProfileModule.SetImage(ofd.FileName);
                }
                catch (Exception ex)
                {
                    Entrance.ShowError("设置头像失败!", ex);
                    Log.Error(ex);
                }
            }
        }
    }
}
