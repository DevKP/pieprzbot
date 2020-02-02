using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class MessageArgs : EventArgs
    {
        public MessageArgs(Message m) { Message = m; }
        public Message Message { get; }
    }
}
