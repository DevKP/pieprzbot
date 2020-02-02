using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(Message m, string command, string text)
        { Message = m; Command = command; Text = text; }
        public Message Message { get; }
        public string Command { get; }
        public string Text { get; }
    }
}
