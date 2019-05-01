using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;

namespace PersikSharp
{
    public class MessageArgs : EventArgs
    {
        public MessageArgs(Message m) { Message = m; }
        public Message Message { get; }
    }
    public class CallbackQueryArgs : EventArgs
    {
        public CallbackQueryArgs(CallbackQuery m) { Callback = m; }
        public CallbackQuery Callback { get; }
    }

    class BotCallBacks
    {
        public event EventHandler<MessageArgs> onTextMessage;
        public event EventHandler<MessageArgs> onStickerMessage;
        public event EventHandler<MessageArgs> onPhotoMessage;
        public event EventHandler<MessageArgs> onChatMembersAddedMessage;

        public event EventHandler<MessageArgs> onTextEdited;

        public Dictionary<string, EventHandler<MessageArgs>> commandsCallbacks =
            new Dictionary<string, EventHandler<MessageArgs>>();
        public Dictionary<string, EventHandler<CallbackQueryArgs>> queryCallbacks =
            new Dictionary<string, EventHandler<CallbackQueryArgs>>();

        private string bot_username;

        public BotCallBacks() { }
        public BotCallBacks(TelegramBotClient bot)
        {
            bot.OnMessage += Bot_OnMessage;
            bot.OnMessageEdited += Bot_OnMessageEdited;
            bot.OnCallbackQuery += Bot_OnCallbackQuery;

            onTextMessage += onTextCommandsParsing;

            bot_username = bot.GetMeAsync().Result.Username;
        }

        public void RegisterCommand(string command, EventHandler<MessageArgs> c)
        {
            commandsCallbacks.Add(command, c);
        }

        public void RegisterCallbackQuery(string data, EventHandler<CallbackQueryArgs> c)
        {
            queryCallbacks.Add(data, c);
        }

        private void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs a)
        {
            try { 
                queryCallbacks[a.CallbackQuery.Data].Invoke(this, new CallbackQueryArgs(a.CallbackQuery));
                Logger.Log(LogType.Info, $"<{this.GetType().Name}> InlineCallback \"{a.CallbackQuery.Data}\" from user ({a.CallbackQuery.From.FirstName}:{a.CallbackQuery.From.Id}).");
            }
            catch (KeyNotFoundException e)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> CallbackQuery with Data: \"{a.CallbackQuery.Data}\" isn't registered!!");
            }
        }

        private void Bot_OnMessageEdited(object sender, MessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case MessageType.Text:
                    onTextEdited?.Invoke(this, new MessageArgs(e.Message));
                    break;
            }
        }

        private void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            string message_type_str = $"[{message.Chat.Type.ToString()}:{e.Message.Type.ToString()}]({message.From.FirstName}:{message.From.Id})";
            string message_str = "";

            switch (e.Message.Type)
            {
                case MessageType.Text:
                    message_str = message.Text;
                    onTextMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Sticker:
                    message_str = message.Sticker?.FileId;
                    onStickerMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Photo:
                    message_str = message.Photo[0].FileId;
                    onPhotoMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.ChatMembersAdded:
                    message_str = message.NewChatMembers[0].Id.ToString();
                    onChatMembersAddedMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Unknown:
                    break;
            }

            Logger.Log(LogType.Info, $"{message_type_str}: {message_str}");
        }

        private void onTextCommandsParsing(object sender, MessageArgs message_args)
        {
            var message = message_args.Message;
            var match = Regex.Match(message.Text, $"^\\/(\\w*)((?=@{bot_username}))?", RegexOptions.IgnoreCase);
            try
            {
                if (match.Success)
                {
                    commandsCallbacks[match.Groups[0].Value].Invoke(this, new MessageArgs(message));
                    Logger.Log(LogType.Info, $"<{this.GetType().Name}> User ({message.From.FirstName}:{message.From.Id}) called \"{match.Groups[0].Value}\" command.");
                }
            }catch(KeyNotFoundException e)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> Command \"{match.Groups[0].Value}\" not found!!");
            }
        }
    }
}
