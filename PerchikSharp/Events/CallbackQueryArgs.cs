using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    public class CallbackQueryArgs : EventArgs
    {
        public CallbackQueryArgs(CallbackQuery m, int userid = 0, object obj = null) =>
            (Callback, UserId, this.obj) = (m, userid, obj);
        public CallbackQueryArgs(Message m, int userid = 0, object obj = null)
        {
            Callback = new CallbackQuery();
            Callback.Message = m;
            UserId = userid;
            this.obj = obj;
        }
        public CallbackQuery Callback { get; }
        public int UserId { get; }
        public object obj { get; }
    }
}
