using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class RegExArgs : EventArgs
    {
        public RegExArgs(Message msg, Match m, string p)
        {
            this.Message = msg;
            this.Match = m;
            this.Pattern = p;
        }
        public Match Match { get; }
        public string Pattern { get; }
        public Message Message { get; }
    }
}
