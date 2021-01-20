using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.Models
{
    public sealed class Share : IFinal, IDisposable, INotifyPropertyChanged
    {
        internal static Func<int, Guid, Socket, Task> _backlog;

        internal static void _Register(Share share)
        {
            _backlog += share._Accept;
        }

        /// <summary>
        /// 通知发送者并返回关联任务
        /// </summary>
        public static async Task Notify(int id, Guid key, Socket socket)
        {
            var lst = _backlog?.GetInvocationList();
            if (lst == null)
                return;
            foreach (var i in lst)
            {
                var fun = (Func<int, Guid, Socket, Task>)i;
                var res = fun.Invoke(id, key, socket);
                if (res == null)
                    continue;
                await res;
            }
        }

        internal readonly Guid _key = Guid.NewGuid();

        internal readonly string _name;

        internal readonly string _path;

        internal readonly object _info;

        internal readonly long _length;

        internal readonly BindingList<ShareWorker> _list = new BindingList<ShareWorker>();

        internal int _closed = 0;

        #region PropertyChange

        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged(string str = null) =>
            Application.Current.Dispatcher.Invoke(() =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(str ?? string.Empty)));

        #endregion

        /// <summary>
        /// 是否为批量操作 (目录: 真, 文件: 假)
        /// </summary>
        public bool IsBatch => _info is DirectoryInfo;

        /// <summary>
        /// 文件名或目录名
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 完整路径
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// 文件长度
        /// </summary>
        public long Length => _length;

        public BindingList<ShareWorker> WorkerList => _list;

        public bool IsFinal => Volatile.Read(ref _closed) != 0;

        internal Share(FileSystemInfo info)
        {
            _info = info;
            _name = info.Name;
            _path = info.FullName;
        }

        public Share(FileInfo info) : this((FileSystemInfo)info)
        {
            _length = info.Length;
            _Register(this);
        }

        public Share(DirectoryInfo info) : this((FileSystemInfo)info)
        {
            _Register(this);
        }

        internal Task _Accept(int id, Guid key, Socket socket)
        {
            if (Volatile.Read(ref _closed) != 0 || key != _key)
                return null;
            var obj = new ShareWorker(this, id, socket);
            Application.Current.Dispatcher.Invoke(() => _list.Add(obj));
            return obj.Start();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _closed, 1, 0) != 0)
                return;
            _backlog -= _Accept;

            OnPropertyChanged(nameof(IsFinal));
            var lst = default(List<ShareWorker>);
            _ = Application.Current.Dispatcher.Invoke(() => lst = _list.ToList());
            lst.ForEach(r => r.Dispose());
        }
    }
}
