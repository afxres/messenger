using Messenger.Models;
using Messenger.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    /// <summary>
    /// ProfilePage.xaml 的交互逻辑
    /// </summary>
    public partial class PageProfile : Page
    {
        public PageProfile()
        {
            InitializeComponent();
            Loaded += _Loaded;
            Unloaded += _Unloaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            ProfileModule.InscopeChanged += ModuleProfile_InscopeChanged;
            uiProfileList.SelectionChanged += PageManager._ListBoxSelectionChanged;
        }

        private void _Unloaded(object sender, RoutedEventArgs e)
        {
            ProfileModule.InscopeChanged -= ModuleProfile_InscopeChanged;
            uiProfileList.SelectionChanged -= PageManager._ListBoxSelectionChanged;
        }

        private void ModuleProfile_InscopeChanged(object sender, EventArgs e)
        {
            var pag = uiRightFrame.Content as Chatter;
            if (pag == null || pag.Profile.Id != ProfileModule.Inscope.Id)
                _ = uiRightFrame.Navigate(new Chatter());
        }

        /// <summary>
        /// 根据用户昵称和签名提供搜索功能
        /// </summary>
        private void _TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.OriginalSource != uiSearchBox)
                return;
            var lst = uiProfileList.ItemsSource as ICollection<Profile>;
            lst?.Clear();
            if (string.IsNullOrWhiteSpace(uiSearchBox.Text) == true)
            {
                uiProfileList.ItemsSource = null;
                uiPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                var txt = uiSearchBox.Text.ToLower();
                var val = (from i in ProfileModule.ClientList.Union(ProfileModule.GroupList).Union(ProfileModule.RecentList)
                           where i.Name?.ToLower().Contains(txt) == true || i.Text?.ToLower().Contains(txt) == true
                           select i).ToList();
                var idx = val.IndexOf(ProfileModule.Inscope);
                uiProfileList.ItemsSource = val;
                uiProfileList.SelectedIndex = idx;
                uiPanel.Visibility = Visibility.Visible;
            }
        }
    }
}
