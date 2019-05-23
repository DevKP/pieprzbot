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
        public event EventHandler<NextstepArgs> callback;

        public BotCallBackUnit(EventHandler<NextstepArgs> callback, Message message, object arg)
        {
            this.callback = callback;
            this.userId = message.From.Id;
            this.chatId = message.Chat.Id;
            this.arg = arg;
        }

        public void InvokeCallback(Message message)
        {
            callback?.Invoke(this, new NextstepArgs(message, arg));
        }
    }
}
