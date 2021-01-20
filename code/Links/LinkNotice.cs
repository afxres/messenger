using System;
using System.Threading;

namespace Mikodev.Network
{
    public class LinkNotice
    {
        internal readonly LinkNoticeSource _source;

        internal readonly int _value;

        internal int _status;

        public bool IsAny => _status > 0;

        internal LinkNotice() => _status = 0;

        internal LinkNotice(LinkNoticeSource inspector, int version)
        {
            _source = inspector;
            _value = version;
            _status = 1;
        }

        public void Handled()
        {
            // 阻止多次调用
            if (Interlocked.CompareExchange(ref _status, 2, 1) != 1)
                throw new InvalidOperationException("Invalid operation!");
            _source._Handled(_value);
        }
    }
}
