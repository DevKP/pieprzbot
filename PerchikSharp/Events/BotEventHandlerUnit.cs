using System;
using Telegram.Bot.Types;

namespace PerchikSharp.Events
{
    public class BotEventHandlerUnit
    {
        public int userId;
        public long chatId;
        public object arg;
        public bool fromAnyUser;
        public event EventHandler<NextstepArgs> onCallback;

        public BotEventHandlerUnit(EventHandler<NextstepArgs> callback, Message message, bool fromAnyUser = false, object arg = null)
        {
            this.onCallback = callback;
            this.userId = message.From.Id;
            this.chatId = message.Chat.Id;
            this.arg = arg;
            this.fromAnyUser = fromAnyUser;
        }

        public void InvokeCallback(Message message) =>
            onCallback?.Invoke(this, new NextstepArgs(message, arg));
    }
}
