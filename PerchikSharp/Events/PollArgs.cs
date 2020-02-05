using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class PollArgs : EventArgs
    {
        public PollArgs(Poll poll, PollAnswer pollAnswer)
        {
            this.poll = poll;
            this.pollAnswer = pollAnswer;
        }
        public Poll poll { get; }
        public PollAnswer pollAnswer { get; }
    }
}
