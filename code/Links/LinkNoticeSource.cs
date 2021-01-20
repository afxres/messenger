using System;
using System.Threading;

namespace Mikodev.Network
{
    public class LinkNoticeSource
    {
        internal readonly TimeSpan _interval;

        internal DateTime _timestamp = DateTime.MinValue;

        internal int _version = 0;

        internal int _handled = 0;

        public LinkNoticeSource(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval));
            _interval = interval;
        }

        public void Update()
        {
            _ = Interlocked.Increment(ref _version);
        }

        public LinkNotice Notice() => GetNotice(false);

        public LinkNotice GetNotice(bool force)
        {
            var ver = Volatile.Read(ref _version);
            var cur = _handled;
            if (cur == ver)
                goto nothing;

            if (force)
                return new LinkNotice(this, _version);

            var old = _timestamp;
            var now = DateTime.Now;
            var sub = now - old;
            if (sub > TimeSpan.Zero && sub < _interval)
                goto nothing;
            return new LinkNotice(this, ver);

        nothing:
            return new LinkNotice();
        }

        internal void _Handled(int version)
        {
            var now = DateTime.Now;
            _timestamp = now;
            Volatile.Write(ref _handled, version);
        }
    }
}
