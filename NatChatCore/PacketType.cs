namespace NatChatCore
{
    public enum PacketType
    {
        Message = 0,
        Keepalive = 1,
        KeepaliveReply = 2,
        Discover = 3
    }
}