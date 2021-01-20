using System;
using System.Runtime.Serialization;

namespace Mikodev.Network
{
    [Serializable]
    public class LinkException : Exception
    {
        internal readonly LinkError _error = LinkError.None;

        public LinkException(LinkError error) : base(_GetMessage(error)) => _error = error;

        public LinkException(LinkError error, Exception inner) : base(_GetMessage(error), inner) => _error = error;

        protected LinkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            _error = (LinkError)info.GetValue(nameof(LinkError), typeof(LinkError));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(LinkError), _error);
            base.GetObjectData(info, context);
        }

        internal static string _GetMessage(LinkError error)
        {
            switch (error)
            {
                case LinkError.Success:
                    return "Operation successful without error!";

                case LinkError.Overflow:
                    return "Buffer length overflow!";

                case LinkError.ProtocolMismatch:
                    return "Protocol mismatch!";

                case LinkError.IdInvalid:
                    return "Invalid client id!";

                case LinkError.IdConflict:
                    return "Client id conflict with current users!";

                case LinkError.CountLimited:
                    return "Client count has been limited!";

                case LinkError.GroupLimited:
                    return "Group label is too many!";

                case LinkError.QueueLimited:
                    return "Message queue full!";

                default:
                    return "Unknown error!";
            }
        }
    }
}
