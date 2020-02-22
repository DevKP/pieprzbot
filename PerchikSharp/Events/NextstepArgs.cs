using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class NextstepArgs : EventArgs
    {
        public NextstepArgs(Message m, object arg) =>
            (Message, Arg) = (m, arg);
        public Message Message { get; }
        public object Arg { get; }
    }
}
