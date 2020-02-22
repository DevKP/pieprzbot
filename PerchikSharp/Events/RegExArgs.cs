using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class RegExArgs : EventArgs
    {
        public RegExArgs(Message msg, Match m, string p) =>
            (Match, Pattern, Message) = (m, p, msg);
        public Match Match { get; }
        public string Pattern { get; }
        public Message Message { get; }
    }
}
