namespace Mikodev.Network
{
    public enum LinkError : int
    {
        None,

        Success,

        Overflow,

        ProtocolMismatch,

        IdInvalid,

        IdConflict,

        CountLimited,

        GroupLimited,

        QueueLimited,
    }
}
