using NatChatCore;

namespace NatChat.Services
{
    public class ChatService
    {
        public ChatProcessor Processor { get; set; }

        public ChatService()
        {
            this.Processor = new ChatProcessor();
        }
    }
}