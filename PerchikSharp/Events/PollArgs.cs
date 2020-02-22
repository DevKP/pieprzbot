using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class PollArgs : EventArgs
    {
        public PollArgs(Poll poll, PollAnswer pollAnswer) =>
            (this.poll, this.pollAnswer) = (poll, pollAnswer);
        public Poll poll { get; }
        public PollAnswer pollAnswer { get; }
    }
}
