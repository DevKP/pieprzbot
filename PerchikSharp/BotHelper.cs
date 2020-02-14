using System;
using System.Collections.Generic;
using Telegram.Bot.Args;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using PerchikSharp;
using System.Net.Http;

namespace PerchikSharp
{
    class BotHelper
    {
        public event EventHandler<MessageArgs> onTextMessage;
        public event EventHandler<MessageArgs> onStickerMessage;
        public event EventHandler<MessageArgs> onPhotoMessage;
        public event EventHandler<MessageArgs> onChatMembersAddedMessage;
        public event EventHandler<MessageArgs> onVideoMessage;
        public event EventHandler<MessageArgs> onDocumentMessage;
        public event EventHandler<MessageArgs> onVoiceMessage;
        public event EventHandler<MessageArgs> onVideoNoteMessage;
        public event EventHandler<MessageArgs> onTextEdited;
        public event EventHandler<PollAnswer> onPollAnswer;

        public Dictionary<string, EventHandler<CommandEventArgs>> commandHandlers;
        public Dictionary<InlineButton, EventHandler<CallbackQueryArgs>> queryHandlers; 
        public Dictionary<string, EventHandler<RegExArgs>> regexHandlers;
        public Dictionary<string, EventHandler<PollArgs>> pollHandlers;
        public List<BotEventHandlerUnit> nextstepHandlers;

        public List<PollAnswer> pollAnswersCache;

        public User Me { get; }
        private string bot_username;

        public BotHelper() 
        {
            this.commandHandlers = new Dictionary<string, EventHandler<CommandEventArgs>>();
            this.queryHandlers = new Dictionary<InlineButton, EventHandler<CallbackQueryArgs>>();
            this.regexHandlers = new Dictionary<string, EventHandler<RegExArgs>>();
            this.pollHandlers = new Dictionary<string, EventHandler<PollArgs>>();
            this.nextstepHandlers = new List<BotEventHandlerUnit>();
            this.pollAnswersCache = new List<PollAnswer>();

        }
        public BotHelper(TelegramBotClient bot) : this()
        {
            bot.OnUpdate += this.Bot_OnUpdate;
            bot.OnMessage += this.Bot_OnMessageAsync;
            bot.OnMessageEdited += this.Bot_OnMessageEdited;
            bot.OnCallbackQuery += this.Bot_OnCallbackQuery;
           

            try
            {
                this.Me = bot.GetMeAsync().Result;
                this.bot_username = this.Me.Username;
            }
            catch (Exception exc)
            {
                Logger.Log(LogType.Fatal, $"Check your internet connection! Exception: {exc.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private void Bot_OnUpdate(object sender, UpdateEventArgs e)
        {
            try
            {
                Update update = e.Update;
                switch (e.Update.Type)
                {
                    case UpdateType.Poll:
                        
                        foreach (var poll in this.pollHandlers)
                        {
                            if (poll.Key == update.Poll.Id)
                            {
                                PollAnswer pollanswer = this.pollAnswersCache.Where((p) => p.PollId == update.Poll.Id).LastOrDefault();
                                poll.Value?.Invoke(this, new PollArgs(update.Poll, pollanswer));
                            }
                        }
                        break;
                    case UpdateType.PollAnswer:
                        if (this.pollHandlers.Any(h => h.Key == update.PollAnswer.PollId))
                        {
                            this.pollAnswersCache.Add(e.Update.PollAnswer);
                            this.onPollAnswer?.Invoke(sender, e.Update.PollAnswer);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }


        public void RegisterPoll(string pollId, EventHandler<PollArgs> p)
        {
            this.pollHandlers.Add(pollId, p);
        }

        public void RemovePoll(string pollId)
        {
            this.pollHandlers.Remove(pollId);
        }

        /// <summary>
        /// Registers a callback for chat command. (e.g. test )
        /// </summary>
        /// <param name="command">Command without slash.</param>
        /// <param name="c">Method to be called.</param>
        public void NativeCommand(string command, EventHandler<CommandEventArgs> c)
        {
            this.commandHandlers.Add(command, c);
        }

        /// <summary>
        /// Register patterns for searching in messages.
        /// </summary>
        /// <param name="pattern">Regular expression.</param>
        /// <param name="c">Method to be called.</param>
        public void AddRegEx(string pattern, EventHandler<RegExArgs> c)
        {
            this.regexHandlers.Add(pattern, c);
        }

        /// <summary>
        /// Registers a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        /// <param name="c">Method to be called.</param>
        public void CallbackQuery(string data, EventHandler<CallbackQueryArgs> c)
        {
            this.queryHandlers.Add(new InlineButton(data), c);
        }

        /// <summary>
        /// Registers a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        /// <param name="c">Method to be called.</param>
        public void RegisterCallbackQuery(string data, int userid, object arg, EventHandler<CallbackQueryArgs> c)
        {
            this.queryHandlers.Add(new InlineButton(data, userid, arg), c);
        }

        /// <summary>
        /// Removes a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        public void RemoveCallbackQuery(string data)
        {
            var button = queryHandlers.FirstOrDefault(o => o.Key.Data == data).Key;
            this.queryHandlers.Remove(button);
        }


        public void RegisterNextstep(EventHandler<NextstepArgs> callback, Message message, bool fromAnyUser = false, object arg = null)
        {
            var isWaitingMessages = this.nextstepHandlers.Where(unit => 
            {
                if (unit.chatId == message.Chat.Id)
                {
                    if (fromAnyUser)
                        return true;

                    if (unit.userId == message.From.Id)
                        return true;
                }
                return false;
            }).Any();

            if (!isWaitingMessages)
            {
                var cbUnit = new BotEventHandlerUnit(callback, message, fromAnyUser, arg);
                this.nextstepHandlers.Add(cbUnit);
            }
        }
		
        public void RemoveNextstepCallback(Message message)
        {
            var waitingMessages = this.nextstepHandlers.Where(unit => unit.userId == message.From.Id
                && unit.chatId == message.Chat.Id);
            if (waitingMessages.Any())
            {
                var waitingMessage = waitingMessages.First();
                this.nextstepHandlers.Remove(waitingMessage);
            }
        }

        private void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs a)
        {
            try
            {
                Logger.Log(LogType.Info, $"<{this.GetType().Name}> InlineCallback \"{a.CallbackQuery.Data}\" from user ({a.CallbackQuery.From.FirstName}:{a.CallbackQuery.From.Id}).");

                var buttonEventPair = this.queryHandlers.First(o => o.Key.Data == a.CallbackQuery.Data);

                if (buttonEventPair.Key.UserId != 0)
                {
                    if (buttonEventPair.Key.UserId == a.CallbackQuery.From.Id)
                        buttonEventPair.Value.Invoke(this, new CallbackQueryArgs(a.CallbackQuery, obj: buttonEventPair.Key.Arg));
                }
                else
                {
                    buttonEventPair.Value.Invoke(this, new CallbackQueryArgs(a.CallbackQuery, obj: buttonEventPair.Key.Arg));
                }

            }
            catch (Exception)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> CallbackQuery with Data: \"{a.CallbackQuery.Data}\" isn't registered!!");
            }
        }

        private void Bot_OnMessageEdited(object sender, MessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case MessageType.Text:
                    this.onTextEdited?.Invoke(this, new MessageArgs(e.Message));
                    break;
            }
        }

        private void Bot_OnMessageAsync(object sender, MessageEventArgs e)
        {
            Thread thread = new Thread(() => Bot_OnMessage(sender, e));
            thread.Start();
        }

        private void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            var waitingMessages = this.nextstepHandlers.Where(unit =>
            {
                if (unit.chatId == message.Chat.Id)
                {
                    if (unit.fromAnyUser)
                        return true;

                    if (unit.userId == message.From.Id)
                        return true;
                }
                return false;
            });

            if (waitingMessages.Any())
            {
                var waitingMessage = waitingMessages.First();
                this.nextstepHandlers.Remove(waitingMessage);

                waitingMessage.InvokeCallback(message);
            }

            string message_type_str = $"[{message.Chat.Type.ToString()}:{e.Message.Type.ToString()}]({message.From.FirstName}:{message.From.Id})";
            string message_str = "";
            switch (e.Message.Type)
            {
                case MessageType.Text:
                    MessageArgs message_args = new MessageArgs(message);
                    this.onTextCommandsParsing(this, message_args);
                    this.RegEx_OnMessageAsync(this, message_args);

                    message_str = message.Text;
                    this.onTextMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Sticker:
                    message_str = message.Sticker?.FileId;
                    this.onStickerMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Photo:
                    message_str = message.Photo[0].FileId;
                    this.onPhotoMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.ChatMembersAdded:
                    message_str = message.NewChatMembers[0].Id.ToString();
                    this.onChatMembersAddedMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Document:
                    message_str = message.Document.FileId;
                    this.onDocumentMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Video:
                    message_str = message.Video.FileId;
                    this.onVideoMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Voice:
                    message_str = message.Voice.FileId;
                    this.onVoiceMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.VideoNote:
                    message_str = message.VideoNote.FileId;
                    this.onVideoNoteMessage?.Invoke(this, new MessageArgs(e.Message));
                    break;
                case MessageType.Unknown:
                    break;
            }

            Logger.Log(LogType.Info, $"{message_type_str}: {message_str}");
        }

        private void RegEx_OnMessageAsync(object sender, MessageArgs e)
        {
            Thread thread = new Thread(() => RegEx_OnMessage(sender, e));
            thread.Start();
        }
        private void RegEx_OnMessage(object sender, MessageArgs e)
        {
            Message message = e.Message;
            try
            {
                foreach(var regex in this.regexHandlers)
                {
                    string pattern = regex.Key;
                    Match match = Regex.Match(message.Text, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        Logger.Log(LogType.Info, $"<{this.GetType().Name}:RegEx>({message.From.FirstName}:{message.From.Id}) -> {pattern}");

                        RegExArgs rgxArgs = new RegExArgs(message, match, pattern);
                        //_ = Task.Run(() => regex.Value?.Invoke(this, rgxArgs)); 
                        regex.Value?.Invoke(this, rgxArgs);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        private void onTextCommandsParsing(object sender, MessageArgs message_args)//TODO: Redo :D
        {
            var message = message_args.Message;
            var match = Regex.Match(message.Text, $"^\\/(?<command>\\w+)(?<botname>@{bot_username})?", RegexOptions.IgnoreCase);
            try
            {
                if (match.Success)
                {
                    Logger.Log(LogType.Info, $"<{this.GetType().Name}> User ({message.From.FirstName}:{message.From.Id}) called \"{match.Groups[0].Value}\" command.");

                    string command = message.Text;
                    string text = "";
                    if(message.Text.IndexOf(' ') != -1)
                    {
                        command = message.Text.Substring(0, message.Text.IndexOf(' '));
                        text = message.Text.Substring(message.Text.IndexOf(' ') + 1);
                    }

                    CommandEventArgs cmdargs = new CommandEventArgs(message, command, text);
                    this.commandHandlers[match.Groups["command"].Value]?.Invoke(this, cmdargs);
                }
            }catch(KeyNotFoundException)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> Command \"{match.Groups[0].Value}\" not found!!");
            }
        }

        public static async Task<string> HttpRequestAsync(string url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string strResult = await response.Content.ReadAsStringAsync();

                    return strResult;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
