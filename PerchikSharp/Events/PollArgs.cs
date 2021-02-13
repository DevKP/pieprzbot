using System;
using Telegram.Bot.Types;

namespace PerchikSharp.Events
{
    public class PollArgs : EventArgs
    {
        public PollArgs(Poll poll, PollAnswer pollAnswer) =>
            (this.Poll, this.PollAnswer) = (poll, pollAnswer);
        public Poll Poll { get; }
        public PollAnswer PollAnswer { get; }
    }
}
