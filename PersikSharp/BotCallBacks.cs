using System;
using System.Collections.Generic;
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
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(Message m, string command, string text)
        { Message = m; Command = command; Text = text; }
        public Message Message { get; }
        public string Command { get; }
        public string Text { get; }
    }
    public class CallbackQueryArgs : EventArgs
    {
        public CallbackQueryArgs(CallbackQuery m) { Callback = m; }
        public CallbackQueryArgs(Message m) {
            Callback = new CallbackQuery();
            Callback.Message = m;
        }
        public CallbackQuery Callback { get; }
    }

    class BotCallBacks
    {
        public event EventHandler<MessageArgs> onTextMessage;
        public event EventHandler<MessageArgs> onStickerMessage;
        public event EventHandler<MessageArgs> onPhotoMessage;
        public event EventHandler<MessageArgs> onChatMembersAddedMessage;
        public event EventHandler<MessageArgs> onVideoMessage;
        public event EventHandler<MessageArgs> onDocumentMessage;

        public event EventHandler<MessageArgs> onTextEdited;

        public Dictionary<string, EventHandler<CommandEventArgs>> commandsCallbacks =
            new Dictionary<string, EventHandler<CommandEventArgs>>();
        public Dictionary<string, EventHandler<CallbackQueryArgs>> queryCallbacks =
            new Dictionary<string, EventHandler<CallbackQueryArgs>>();
        public Dictionary<int, EventHandler<MessageArgs>> nextstepCallbacks =
            new Dictionary<int, EventHandler<MessageArgs>>();

        private string bot_username;

        public BotCallBacks() { }
        public BotCallBacks(TelegramBotClient bot)
        {
            bot.OnMessage += Bot_OnMessage;
            bot.OnMessageEdited += Bot_OnMessageEdited;
            bot.OnCallbackQuery += Bot_OnCallbackQuery;

            onTextMessage += onTextCommandsParsing;

            try
            {
                bot_username = bot.GetMeAsync().Result.Username;
            }
            catch (Exception exc)
            {
                Logger.Log(LogType.Fatal, $"Check your internet connection! Exception: {exc.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Registers a callback for chat command. (e.g. test )
        /// </summary>
        /// <param name="command">Command without slash.</param>
        /// <param name="c">Method to be called.</param>
        public void RegisterCommand(string command, EventHandler<CommandEventArgs> c)
        {
            commandsCallbacks.Add(command, c);
        }

        /// <summary>
        /// Registers a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        /// <param name="c">Method to be called.</param>
        public void RegisterCallbackQuery(string data, EventHandler<CallbackQueryArgs> c)
        {
            queryCallbacks.Add(data, c);
        }


        public void RegisterNextstepCallback(int userId, EventHandler<MessageArgs> c)
        {
            nextstepCallbacks.Add(userId, c);
        }

        private void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs a)
        {
            try { 
                queryCallbacks[a.CallbackQuery.Data].Invoke(this, new CallbackQueryArgs(a.CallbackQuery));
                Logger.Log(LogType.Info, $"<{this.GetType().Name}> InlineCallback \"{a.CallbackQuery.Data}\" from user ({a.CallbackQuery.From.FirstName}:{a.CallbackQuery.From.Id}).");
            }
            catch (KeyNotFoundException)
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

            if (nextstepCallbacks.ContainsKey(message.From.Id))
            {
                var event_method = nextstepCallbacks[message.From.Id];
                nextstepCallbacks.Remove(message.From.Id);
                event_method?.Invoke(this, new MessageArgs(message));
            }

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
                case MessageType.Document:
                    message_str = message.Document.FileId;
                    onDocumentMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Video:
                    message_str = message.Video.FileId;
                    onVideoMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Unknown:
                    break;
            }

            Logger.Log(LogType.Info, $"{message_type_str}: {message_str}");
        }

        private void onTextCommandsParsing(object sender, MessageArgs message_args)
        {
            var message = message_args.Message;
            var match = Regex.Match(message.Text, $"^\\/(?<command>\\w+)(?<botname>@{bot_username})?", RegexOptions.IgnoreCase);
            try
            {
                if (match.Success)
                {
                    string command = message.Text;
                    string text = "";
                    if(message.Text.IndexOf(' ') != -1)
                    {
                        command = message.Text.Substring(0, message.Text.IndexOf(' '));
                        text = message.Text.Substring(message.Text.IndexOf(' ') + 1);
                    }

                    CommandEventArgs cmdargs = new CommandEventArgs(message, command, text);
                    commandsCallbacks[match.Groups["command"].Value]?.Invoke(this, cmdargs);
                    Logger.Log(LogType.Info, $"<{this.GetType().Name}> User ({message.From.FirstName}:{message.From.Id}) called \"{match.Groups[0].Value}\" command.");
                }
            }catch(KeyNotFoundException)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> Command \"{match.Groups[0].Value}\" not found!!");
            }
        }
    }
}
