using Messenger.Extensions;
using Messenger.Models;
using Messenger.Modules;
using Microsoft.Win32;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for Chatter.xaml
    /// </summary>
    public partial class Chatter : Page
    {
        private Profile _profile = null;

        private BindingList<Packet> _messages = null;

        public Profile Profile => _profile;

        public Chatter()
        {
            InitializeComponent();
            Loaded += _Loaded;
            Unloaded += _Unloaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            HistoryModule.Receive += _HistoryReceiving;
            (Application.Current as App).TextBoxKeyDown += _TextBoxKeyDown;

            _profile = ProfileModule.Inscope;
            _messages = _profile.GetMessages();
            uiProfileGrid.DataContext = _profile;
            uiMessageBox.ItemsSource = _messages;
            _messages.ListChanged += _ListChanged;
            uiMessageBox.ScrollIntoLastEx();
        }

        private void _Unloaded(object sender, RoutedEventArgs e)
        {
            uiMessageBox.ItemsSource = null;
            HistoryModule.Receive -= _HistoryReceiving;
            (Application.Current as App).TextBoxKeyDown -= _TextBoxKeyDown;
            _messages.ListChanged -= _ListChanged;
        }

        private void _ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemAdded)
                return;
            uiMessageBox.ScrollIntoLastEx();
        }

        /// <summary>
        /// 拦截消息通知
        /// </summary>
        private void _HistoryReceiving(object sender, LinkEventArgs<Packet> e)
        {
            if (e.Object.Index != _profile.Id)
                return;
            e.Finish = true;
        }

        private void _TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (sender != uiInputBox || e.Key != Key.Enter)
                return;
            var mod = e.KeyboardDevice.Modifiers;
            var ins = SettingModule.Instance;
            if (ins.UseControlEnter && mod == ModifierKeys.Control || ins.UseEnter && mod == ModifierKeys.None)
                _SendText();
            else
                uiInputBox.InsertEx(Environment.NewLine);
            e.Handled = true;
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == null)
                return;
            if (tag == "text")
                _SendText();
            else if (tag == "image")
                _PushImage();
            else if (tag == "clean")
                HistoryModule.Clear(_profile.Id);
            _ = uiInputBox.Focus();
        }

        private void _SendText()
        {
            var str = uiInputBox.Text.TrimEnd(new char[] { '\0', '\r', '\n', '\t', ' ' });
            if (str.Length < 1)
                return;
            uiInputBox.Text = string.Empty;
            PostModule.Text(_profile.Id, str);
            ProfileModule.SetRecent(_profile);
        }

        private void _PushImage()
        {
            var ofd = new OpenFileDialog() { Filter = "位图文件|*.bmp;*.png;*.jpg" };
            if (ofd.ShowDialog() != true)
                return;
            try
            {
                var buf = CacheModule.ImageZoom(ofd.FileName);
                PostModule.Image(_profile.Id, buf);
                ProfileModule.SetRecent(_profile);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Entrance.ShowError("发送图片失败", ex);
            }
        }

        private void _Share(string path)
        {
            if (File.Exists(path))
                PostModule.File(_profile.Id, path);
            else if (Directory.Exists(path))
                PostModule.Directory(_profile.Id, path);
            return;
        }

        private void _TextBoxPreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
                e.Effects = DragDropEffects.None;
            else
                e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void _TextBoxPreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
                return;
            var arr = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (arr == null || arr.Length < 1)
                return;
            var val = arr[0];
            _Share(val);
        }
    }
}
