using Mikodev.Binary;

namespace Mikodev.Network
{
    public class LinkPacket
    {
        internal int _source = 0;

        internal int _target = 0;

        internal string _path = null;

        internal byte[] _buffer = null;

        internal Token _origin = null;

        internal Token _data = null;

        public int Source => _source;

        public int Target => _target;

        public string Path => _path;

        public byte[] Buffer => _buffer;

        public Token Origin => _origin;

        public Token Data => _data;
    }
}
