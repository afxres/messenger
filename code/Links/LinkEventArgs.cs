using System;

namespace Mikodev.Network
{
    public class LinkEventArgs<T> : EventArgs
    {
        internal T _obj;

        internal bool _cancel = false;

        internal bool _finish = false;

        public LinkEventArgs(T value) => _obj = value;

        public T Object => _obj;

        public bool Cancel { get => _cancel; set => _cancel = value; }

        public bool Finish { get => _finish; set => _finish = value; }
    }
}
