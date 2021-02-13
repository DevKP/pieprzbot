using System;
using Telegram.Bot.Types;

namespace PerchikSharp.Events
{
    public class NextstepArgs : EventArgs
    {
        public NextstepArgs(Message m, object arg) =>
            (Message, Arg) = (m, arg);
        public Message Message { get; }
        public object Arg { get; }
    }
}
