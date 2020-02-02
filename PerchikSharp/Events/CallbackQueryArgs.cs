using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class CallbackQueryArgs : EventArgs
    {
        public CallbackQueryArgs(CallbackQuery m, int userid = 0)
        { Callback = m; UserId = userid; }
        public CallbackQueryArgs(Message m, int userid = 0)
        {
            Callback = new CallbackQuery();
            Callback.Message = m;
            UserId = userid;
        }
        public CallbackQuery Callback { get; }
        public int UserId { get; }
    }
}
