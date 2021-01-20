using Messenger.Models;
using Messenger.Modules;
using Mikodev.Logger;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for Connection.xaml
    /// </summary>
    public partial class Connection : Page
    {
        private class _Temp
        {
            public string I { get; set; } = string.Empty;

            public string P { get; set; } = string.Empty;
        }

        private readonly BindingList<Host> _hosts = new BindingList<Host>();

        public Connection()
        {
            InitializeComponent();
            uiTableGrid.DataContext = new _Temp();
            Loaded += _Loaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            if (HostModule.Name != null)
            {
                uiHostBox.Text = HostModule.Name;
                uiPortBox.Text = HostModule.Port.ToString();
            }
            uiIdBox.Text = ProfileModule.Id.ToString();
            uiServerList.ItemsSource = _hosts;
            uiServerList.SelectionChanged += _SelectionChanged;
        }

        private void _SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1)
                return;
            var itm = e.AddedItems[0] as Host;
            if (itm == null)
                return;
            uiHostBox.Text = itm.Address?.ToString();
            uiPortBox.Text = itm.Port.ToString();
            uiServerList.SelectedIndex = -1;
        }

        private async void _Click(object sender, RoutedEventArgs e)
        {
            async void _Refresh()
            {
                uiRefreshButton.IsEnabled = false;
                var lst = await Task.Run(HostModule.Refresh);
                foreach (var inf in lst)
                {
                    var idx = _hosts.IndexOf(inf);
                    if (idx < 0)
                        _hosts.Add(inf);
                    else _hosts[idx] = inf;
                }
                uiRefreshButton.IsEnabled = true;
            }

            var src = (Button)e.OriginalSource;
            if (src == uiBrowserButton)
            {
                uiBrowserButton.Visibility = Visibility.Collapsed;
                uiRefreshButton.Visibility =
                uiListGrid.Visibility = Visibility.Visible;
                _Refresh();
                return;
            }
            else if (src == uiRefreshButton)
            {
                _hosts.Clear();
                _Refresh();
                return;
            }
            else if (src == uiConnectButton)
            {
                uiConnectButton.IsEnabled = false;
                try
                {
                    var uid = int.Parse(uiIdBox.Text);
                    var pot = int.Parse(uiPortBox.Text);
                    var hos = uiHostBox.Text;

                    var add = IPAddress.TryParse(hos, out var hst);
                    if (add == false)
                        hst = Dns.GetHostEntry(hos).AddressList.First(r => r.AddressFamily == AddressFamily.InterNetwork);
                    var iep = new IPEndPoint(hst, pot);

                    // 放弃等待该方法返回的任务
                    _ = await LinkModule.Start(uid, iep);
                    HostModule.Name = hos;
                    HostModule.Port = pot;

                    _ = NavigationService.Navigate(new PageFrame());
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    Entrance.ShowError("连接失败", ex);
                }
                uiConnectButton.IsEnabled = true;
            }
        }
    }
}
