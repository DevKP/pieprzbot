using System;
using Telegram.Bot.Types;

namespace PerchikSharp.Events
{
    public class MessageArgs : EventArgs
    {
        public MessageArgs(Message m) => Message = m;
        public Message Message { get; }
    }
}
