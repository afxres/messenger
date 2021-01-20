using Messenger.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.Models
{
    public abstract class ShareBasic : IFinal, INotifyPropertyChanged
    {
        internal class Tick
        {
            public long Time = 0;

            public long Position = 0;

            public double Speed = 0;
        }

        /// <summary>
        /// 历史记录上限
        /// </summary>
        private const int _tickLimit = 16;

        private const int _delay = 200;

        private static Action s_action = null;

        private static readonly Stopwatch s_watch = new Stopwatch();

        private static readonly Task s_task = new Task(async () =>
        {
            while (true)
            {
                s_action?.Invoke();
                await Task.Delay(_delay);
            }
        });

        /// <summary>
        /// 注册以便实时计算传输进度 (当 <see cref="IsFinal"/> 为真时自动取消注册)
        /// </summary>
        protected void Register() => s_action += _Refresh;

        #region PropertyChange

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string str = null) =>
            Application.Current.Dispatcher.Invoke(() =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(str ?? string.Empty)));

        #endregion

        private readonly int _id;

        private int _status = (int)ShareStatus.等待;

        private double _speed = 0;

        private double _progress = 0;

        private TimeSpan _remain = TimeSpan.Zero;

        private readonly List<Tick> _ticks = new List<Tick>();

        protected ShareBasic(int id)
        {
            _id = id;
        }

        protected int Id => _id;

        public ShareStatus Status
        {
            get => (ShareStatus)Volatile.Read(ref _status);
            protected set => _SetStatus(value);
        }

        public bool IsFinal => (Status & ShareStatus.终止) != 0;

        public abstract long Length { get; }

        public abstract bool IsBatch { get; }

        public abstract string Name { get; }

        public abstract string Path { get; }

        public abstract long Position { get; }

        public Profile Profile => ProfileModule.Query(Id, true);

        public TimeSpan Remain => _remain;

        public double Speed => _speed;

        public double Progress => _progress;

        private void _SetStatus(ShareStatus status)
        {
            while (true)
            {
                var cur = Volatile.Read(ref _status);
                if ((cur & (int)ShareStatus.终止) != 0)
                    throw new InvalidOperationException();
                if (Interlocked.CompareExchange(ref _status, (int)status, cur) == cur)
                    break;
                continue;
            }
        }

        private void _Refresh()
        {
            var fin = IsFinal;

            var avg = _AverageSpeed();
            _speed = avg * 1000; // 毫秒 -> 秒
            _progress = (Length > 0)
                ? (100.0 * Position / Length)
                : (fin ? 100 : 0);

            if (IsBatch == false)
            {
                var spa = (avg > 0 && Position > 0)
                    ? TimeSpan.FromMilliseconds((Length - Position) / avg)
                    : TimeSpan.Zero;
                // 移除毫秒部分
                _remain = new TimeSpan(spa.Days, spa.Hours, spa.Minutes, spa.Seconds);
                OnPropertyChanged(nameof(Remain));
            }

            OnPropertyChanged(nameof(Speed));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Progress));

            // 确保 IsFinal 为真后再计算一次
            if (fin == true)
            {
                s_action -= _Refresh;
                OnPropertyChanged(nameof(IsFinal));
            }
        }

        private double _AverageSpeed()
        {
            var tic = s_watch.ElapsedMilliseconds;
            var cur = new Tick { Time = tic, Position = Position };
            if (_ticks.Count > 0)
            {
                var pre = _ticks[_ticks.Count - 1];
                var pos = cur.Position - pre.Position;
                var sub = cur.Time - pre.Time;
                cur.Speed = 1.0 * pos / sub;
            }
            _ticks.Add(cur);
            // 计算最近几条记录的平均速度
            if (_ticks.Count > _tickLimit)
                _ticks.RemoveRange(0, _ticks.Count - _tickLimit);
            return _ticks.Average(r => r.Speed);
        }

        [Loader(16, LoaderFlags.OnLoad)]
        public static void Load()
        {
            s_task.Start();
            s_watch.Start();
        }
    }
}
