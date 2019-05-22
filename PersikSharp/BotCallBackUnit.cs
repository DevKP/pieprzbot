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
        public event EventHandler<MessageArgs> callback;

        public BotCallBackUnit(Message message, EventHandler<MessageArgs> callback)
        {
            this.userId   = message.From.Id;
            this.chatId   = message.Chat.Id;
            this.callback = callback;
        }

        public void InvokeCallback(Message message)
        {
            callback?.Invoke(this, new MessageArgs(message));
        }
    }
}
