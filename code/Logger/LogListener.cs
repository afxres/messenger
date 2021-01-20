using System.Diagnostics;

namespace Mikodev.Logger
{
    internal class LogListener : TraceListener
    {
        public override void Write(string message) => Log._Trace(message);

        public override void WriteLine(string message) => Log._Trace(message);
    }
}
