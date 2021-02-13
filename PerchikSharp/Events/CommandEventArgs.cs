using System;
using Telegram.Bot.Types;

namespace PerchikSharp.Events
{
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(Message m, string command, string text) =>
            (Message, Command, Text) = (m, command, text);
        public Message Message { get; }
        public string Command { get; }
        public string Text { get; }
    }
}
