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

    class BotCallBackManager
    {
        public event EventHandler<MessageArgs> onTextMessage;
        public event EventHandler<MessageArgs> onStickerMessage;
        public event EventHandler<MessageArgs> onPhotoMessage;
        public event EventHandler<MessageArgs> onChatMembersAddedMessage;

        public event EventHandler<MessageArgs> onTextEdited;

        public Dictionary<string, EventHandler<MessageArgs>> commandsCallbacks = new Dictionary<string, EventHandler<MessageArgs>>();

        private string botusername;

        public BotCallBackManager() { }
        public BotCallBackManager(TelegramBotClient bot)
        {
            bot.OnMessage += Bot_OnMessage;
            bot.OnMessageEdited += Bot_OnMessageEdited;
            onTextMessage += onText;
            botusername = bot.GetMeAsync().Result.Username;
        }

        public void RegisterCommand(string command, EventHandler<MessageArgs> c)
        {
            commandsCallbacks.Add(command, c);
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
            switch (e.Message.Type)
            {
                case MessageType.Text:
                    onTextMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Sticker:
                    onStickerMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Photo:
                    onPhotoMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.ChatMembersAdded:
                    onChatMembersAddedMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Unknown:
                    break;
            }
        }

        private void onText(object sender, MessageArgs message_args)
        {
            var message = message_args.Message;
            var match = Regex.Match(message.Text, $"^\\/(\\w*)((?=@{botusername}))?", RegexOptions.IgnoreCase);
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
