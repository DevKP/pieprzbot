using System;
using Telegram.Bot.Args;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PersikSharp
{
    public class BotCallBackUnit
    {
        public int userId;
        public long chatId;
        public object arg;
        public bool fromAnyUser;
        public event EventHandler<NextstepArgs> callback;

        public BotCallBackUnit(EventHandler<NextstepArgs> callback, Message message, bool fromAnyUser = false, object arg = null)
        {
            this.callback = callback;
            this.userId = message.From.Id;
            this.chatId = message.Chat.Id;
            this.arg = arg;
            this.fromAnyUser = fromAnyUser;
        }

        public void InvokeCallback(Message message)
        {
            callback?.Invoke(this, new NextstepArgs(message, arg));
        }
    }
}
