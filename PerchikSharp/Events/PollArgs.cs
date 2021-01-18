using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class PollArgs : EventArgs
    {
        public PollArgs(Poll poll, PollAnswer pollAnswer) =>
            (this.Poll, this.PollAnswer) = (poll, pollAnswer);
        public Poll Poll { get; }
        public PollAnswer PollAnswer { get; }
    }
}
