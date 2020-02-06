using System;
using Telegram.Bot.Args;
using Telegram.Bot;
using Telegram.Bot.Types;
using PerchikSharp;

namespace PerchikSharp
{
    public class BotEventHandlerUnit
    {
        public int userId;
        public long chatId;
        public object arg;
        public bool fromAnyUser;
        public event EventHandler<NextstepArgs> callback;

        public BotEventHandlerUnit(EventHandler<NextstepArgs> callback, Message message, bool fromAnyUser = false, object arg = null)
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
