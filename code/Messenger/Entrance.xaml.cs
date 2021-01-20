using Messenger.Modules;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static Messenger.Extensions.NativeMethods;
using static System.Windows.ResizeMode;
using static System.Windows.WindowState;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for Entrance.xaml
    /// </summary>
    public partial class Entrance : Window
    {
        public Entrance()
        {
            InitializeComponent();
            Closing += _Closing;
        }

        private void _Closing(object sender, CancelEventArgs e)
        {
            if (LinkModule.IsRunning == false)
                return;
            if (WindowState != Minimized)
                WindowState = Minimized;
            e.Cancel = true;
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == "confirm")
                uiMessagePanel.Visibility = Visibility.Collapsed;
            return;
        }

        /// <summary>
        /// 显示提示信息 (可以跨线程调用)
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">内容 (调用 <see cref="object.ToString"/> 方法)</param>
        public static void ShowError(string title, Exception content)
        {
            var app = Application.Current;
            var dis = app.Dispatcher;
            dis.Invoke(() =>
            {
                var win = app.MainWindow as Entrance;
                if (win == null)
                    return;
                win.uiHeadText.Text = title;

                win.uiContentText.Text = content?.ToString() ?? "未提供信息";
                win.uiMessagePanel.Visibility = Visibility.Visible;
            });
        }

        #region Flat window style

        private void _BorderLoaded(object sender, RoutedEventArgs e)
        {
            // 隐藏窗体控制按钮
            var han = new WindowInteropHelper(this).Handle;
            var now = GetWindowLong(han, GWL_STYLE);
            var res = SetWindowLong(han, GWL_STYLE, now & ~WS_SYSMENU);

            // 若为低版本的 Windows, 设置边框颜色为灰色
            var src = (Border)e.OriginalSource;
            var ver = Environment.OSVersion.Version;
            if (ver.Major < 6 || ver.Major == 6 && ver.Minor < 2)
                src.Background = Brushes.Gray;
            return;
        }

        private void _PanelClick(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == "min")
                WindowState = Minimized;
            else if (tag == "max")
                _Toggle();
            else if (tag == "exit")
                Close();
            return;
        }

        private void _Toggle()
        {
            if (ResizeMode != CanResize && ResizeMode != CanResizeWithGrip)
                return;
            var cur = WindowState;
            if (cur == Maximized && _IsTabletMode())
                return;
            WindowState = (cur == Maximized) ? Normal : Maximized;
        }

        private void _MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                _Toggle();
            else
                DragMove();
            return;
        }

        private const string _TabletModeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ImmersiveShell";

        private const string _TabletModeValue = "TabletMode";

        private bool _IsTabletMode()
        {
            var val = Registry.GetValue(_TabletModeKey, _TabletModeValue, -1);
            return val.Equals(1);
        }

        #endregion
    }
}
