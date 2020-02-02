using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class PollArgs : EventArgs
    {
        public PollArgs(Poll poll)
        { this.poll = poll; }
        public Poll poll { get; }
    }
}
