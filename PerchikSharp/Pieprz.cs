using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PerchikSharp.Commands;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp
{
    internal class Pieprz : TelegramBotClient
    {
        public static string botVersion = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;

        public event EventHandler<RegExArgs> onNoneRegexMatched;
        public event EventHandler<RegExArgs> onNameRegexMatched;
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

        public Dictionary<INativeCommand, EventHandler<CommandEventArgs>> nativeCommands;
        public Dictionary<IRegExCommand, EventHandler<RegExArgs>> regExCommands;

        public Dictionary<string, EventHandler<CommandEventArgs>> commandHandlers;
        public Dictionary<InlineButton, EventHandler<CallbackQueryArgs>> queryHandlers; 
        public Dictionary<string, EventHandler<RegExArgs>> regexHandlers;
        public Dictionary<string, EventHandler<PollArgs>> pollHandlers;
        public List<BotEventHandlerUnit> nextstepHandlers;

        public List<PollAnswer> pollAnswersCache;

        public User Me { get; }
        public string RegexName { get; set; }
        private readonly string _botUsername;

        public Pieprz(string token) : base(token)
        {
            commandHandlers = new Dictionary<string, EventHandler<CommandEventArgs>>();
            queryHandlers = new Dictionary<InlineButton, EventHandler<CallbackQueryArgs>>();
            regexHandlers = new Dictionary<string, EventHandler<RegExArgs>>();
            pollHandlers = new Dictionary<string, EventHandler<PollArgs>>();
            nextstepHandlers = new List<BotEventHandlerUnit>();
            pollAnswersCache = new List<PollAnswer>();

            nativeCommands = new Dictionary<INativeCommand, EventHandler<CommandEventArgs>>();
            regExCommands = new Dictionary<IRegExCommand, EventHandler<RegExArgs>>();


            OnUpdate += PollRecieve;
            OnMessageEdited += Bot_OnMessageEdited;
            OnCallbackQuery += Bot_OnCallbackQuery;
            
            onTextMessage += RegexOnTextMessageAsync;
            onTextMessage += CommandsParseOnTextMessage;

            OnMessage += Bot_OnMessageAsync;

            onNameRegexMatched += MatchRegexCommands;


            try
            {
                Me = GetMeAsync().Result;
                _botUsername = Me.Username;
            }
            catch (Exception exc)
            {
                Logger.Log(LogType.Fatal, $"Check your internet connection! Exception: {exc.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private void PollRecieve(object sender, UpdateEventArgs e)
        {
            try
            {
                var update = e.Update;
                switch (e.Update.Type)
                {
                    case UpdateType.Poll:
                        
                        foreach (var (pollId, poll) in pollHandlers)
                        {
                            if (pollId == update.Poll.Id)
                            {
                                var pollAnswer = pollAnswersCache.LastOrDefault(p => p.PollId == update.Poll.Id);
                                poll?.Invoke(this, new PollArgs(update.Poll, pollAnswer));
                            }
                        }
                        break;
                    case UpdateType.PollAnswer:
                        if (pollHandlers.Any(h => h.Key == update.PollAnswer.PollId))
                        {
                            pollAnswersCache.Add(e.Update.PollAnswer);
                            onPollAnswer?.Invoke(sender, e.Update.PollAnswer);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        public void RegisterPoll(string pollId, EventHandler<PollArgs> p)
            => pollHandlers.Add(pollId, p);

        public void RemovePoll(string pollId) 
            => pollHandlers.Remove(pollId);

        /// <summary>
        /// Registers a callback for chat command. (e.g. test )
        /// </summary>
        /// <param name="command">Command without slash.</param>
        /// <param name="c">Method to be called.</param>
        public void NativeCommand(string command, EventHandler<CommandEventArgs> c)
        {
            commandHandlers.Add(command, c);
        }

        public void NativeCommand(INativeCommand command)
        {
            nativeCommands.Add(command, (s, x) => 
                Task.Run(() => command.OnExecution(s, x)));
        }

        /// <summary>
        /// Register patterns for searching in messages.
        /// </summary>
        /// <param name="pattern">Regular expression.</param>
        /// <param name="c">Method to be called.</param>
        public void AddRegEx(string pattern, EventHandler<RegExArgs> c)
        {
            regexHandlers.Add(pattern, c);
        }

        public void RegExCommand(IRegExCommand command)
        {
            regExCommands.Add(command, (s, r) => 
                Task.Run(() => command.OnExecution(s, r)));
        }

        /// <summary>
        /// Registers a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        /// <param name="c">Method to be called.</param>
        public void CallbackQuery(string data, EventHandler<CallbackQueryArgs> c)
        {
            queryHandlers.Add(new InlineButton(data), c);
        }

        /// <summary>
        /// Registers a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        /// <param name="arg">TODO</param>
        /// <param name="c">Method to be called.</param>
        /// <param name="userid">User Id</param>
        public void RegisterCallbackQuery(string data, int userid, object arg, EventHandler<CallbackQueryArgs> c)
        {
            queryHandlers.Add(new InlineButton(data, userid, arg), c);
        }

        /// <summary>
        /// Removes a сallback query for chat event. Pressing the button, etc.
        /// </summary>
        /// <param name="data">Callback data, see. Telegram API</param>
        public void RemoveCallbackQuery(string data)
        {
            var button = queryHandlers.FirstOrDefault(o => o.Key.Data == data).Key;
            queryHandlers.Remove(button);
        }


        public void RegisterNextstep(EventHandler<NextstepArgs> callback, Message message, bool fromAnyUser = false, object arg = null)
        {
            var isWaitingMessages = nextstepHandlers.Where(unit => 
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
                nextstepHandlers.Add(cbUnit);
            }
        }
		
        public void RemoveNextstepCallback(Message message)
        {
            var waitingMessages = nextstepHandlers.Where(unit =>
                                   (unit.userId, unit.chatId) == (message.From.Id, message.Chat.Id)).ToList();
            if (waitingMessages.Any())
            {
                var waitingMessage = waitingMessages.First();
                nextstepHandlers.Remove(waitingMessage);
            }
        }

        private void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs a)
        {
            try
            {
                Logger.Log(LogType.Info, $"<{GetType().Name}> InlineCallback \"{a.CallbackQuery.Data}\" from user ({a.CallbackQuery.From.FirstName}:{a.CallbackQuery.From.Id}).");

                var buttonEventPair = queryHandlers.First(o => o.Key.Data == a.CallbackQuery.Data);

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
                Logger.Log(LogType.Error, $"<{GetType().Name}> CallbackQuery with Data: \"{a.CallbackQuery.Data}\" isn't registered!!");
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

        private void Bot_OnMessageAsync(object sender, MessageEventArgs e)
        {
            Task.Run(() =>
            {
                var message = e.Message;

                var waitingMessages = nextstepHandlers.Where(unit =>
                {
                    if (unit.chatId != message.Chat.Id)
                        return false;

                    if (unit.fromAnyUser)
                        return true;

                    if (unit.userId == message.From.Id)
                        return true;

                    return false;
                }).ToList();

                if (waitingMessages.Any())
                {
                    var waitingMessage = waitingMessages.First();
                    nextstepHandlers.Remove(waitingMessage);

                    waitingMessage.InvokeCallback(message);
                }

                var messageTypeStr =
                    $"[{message.Chat.Type.ToString()}:{e.Message.Type.ToString()}]({message.From.FirstName}:{message.From.Id})";
                var messageStr = "";
                switch (e.Message.Type)
                {
                    case MessageType.Text:
                        messageStr = message.Text;
                        onTextMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.Sticker:
                        messageStr = message.Sticker?.FileId;
                        onStickerMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.Photo:
                        messageStr = message.Photo[0].FileId;
                        onPhotoMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.ChatMembersAdded:
                        messageStr = message.NewChatMembers[0].Id.ToString();
                        onChatMembersAddedMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.Document:
                        messageStr = message.Document.FileId;
                        onDocumentMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.Video:
                        messageStr = message.Video.FileId;
                        onVideoMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.Voice:
                        messageStr = message.Voice.FileId;
                        onVoiceMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.VideoNote:
                        messageStr = message.VideoNote.FileId;
                        onVideoNoteMessage?.Invoke(this, new MessageArgs(e.Message));
                        break;
                    case MessageType.Unknown:
                        break;
                }

                Logger.Log(LogType.Info, $"{messageTypeStr}: {messageStr}");
            });
        }

        private void RegexOnTextMessageAsync(object sender, MessageArgs e)
        {
            Task.Run(() => RegexOnTextMessage(sender, e));
        }
        private void RegexOnTextMessage(object sender, MessageArgs e)
        {
            var message = e.Message;
            try
            {
                foreach(var (key, value) in regexHandlers)
                {
                    string pattern = key;
                    Match match = Regex.Match(message.Text, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        Logger.Log(LogType.Info, $"<{GetType().Name}:RegEx>({message.From.FirstName}:{message.From.Id}) -> {pattern}");

                        RegExArgs rgxArgs = new RegExArgs(message, match, pattern);
                        //_ = Task.Run(() => regex.Value?.Invoke(this, rgxArgs)); 
                        value?.Invoke(this, rgxArgs);
                    }
                }

                if (RegexName != null)
                {
                    Match m = Regex.Match(message.Text, RegexName, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        message.Text = Regex.Replace(message.Text, RegexName, "", RegexOptions.IgnoreCase);
                        onNameRegexMatched?.Invoke(this, new RegExArgs(message, m, RegexName));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        private void MatchRegexCommands(object sender, RegExArgs arg)
        {
            var msg = arg.Message;

            var success = false;
            foreach (var (key, value) in regExCommands)
            {
                var pattern = key.RegEx;
                var match = Regex.Match(msg.Text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    success = true;
                    var rgxArgs = new RegExArgs(msg, match, pattern);
                    value.Invoke(this, rgxArgs);
                }
            }
            if (!success)
                onNoneRegexMatched?.Invoke(this, new RegExArgs(msg, null, null));
        }

        private void CommandsParseOnTextMessage(object sender, MessageArgs messageArgs)//TODO: Redo :D
        {
            var message = messageArgs.Message;
            var match = Regex.Match(message.Text, $"^\\/(?<command>\\w+)(?<botname>@{_botUsername})?", RegexOptions.IgnoreCase);
            try
            {
                if (!match.Success) return;


                Logger.Log(LogType.Info, $"<{GetType().Name}> User ({message.From.FirstName}:{message.From.Id}) called \"{match.Groups[0].Value}\" command.");

                var command = message.Text;
                var text = "";
                if(message.Text.IndexOf(' ') != -1)
                {
                    command = message.Text.Substring(0, message.Text.IndexOf(' '));
                    text = message.Text.Substring(message.Text.IndexOf(' ') + 1);
                }

                var commandEventArgs = new CommandEventArgs(message, command, text);

                nativeCommands
                    .FirstOrDefault(nc => nc.Key.Command == match.Groups["command"].Value)
                    .Value?
                    .Invoke(this, commandEventArgs);

                commandHandlers
                    .FirstOrDefault(x => x.Key == match.Groups["command"].Value)
                    .Value?
                    .Invoke(this, commandEventArgs);
            }catch(KeyNotFoundException)
            {
                Logger.Log(LogType.Error, $"<{GetType().Name}> Command \"{match.Groups[0].Value}\" not found!!");
            }
        }

        public static async Task<string> HttpRequestAsync(string url)
        {
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync()
                : null;
        }

        /// <summary>
        /// Checks if there is a specified command in the string.
        /// </summary>
        /// <returns>
        /// Boolean.
        /// </returns>
        /// <param name="text">Text.</param>
        /// <param name="command">Command to find.</param>
        public bool FindTextCommand(string text, string command)
        {
            var match = Regex.Match(text, $"^\\/(?<command>\\w+)(?<botname>@{_botUsername})?", RegexOptions.IgnoreCase);

            return match.Success && match.Groups["command"].Value.Equals(command);
        }

        /// <summary>
        /// Checks if the user has administrator rights in the chat.
        /// </summary>
        /// <returns>
        /// Boolean.
        /// </returns>
        /// <param name="chatId">Chat ID.</param>
        /// <param name="userId">User ID.</param>
        public bool IsUserAdmin(long chatId, int userId)
        {
            try
            {
                var chatMembers = GetChatAdministratorsAsync(chatId).Result;
                return Array.Find(chatMembers, e => e.User.Id == userId) != null;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates clickable text link to user profile.
        /// </summary>
        /// <param name="user">User object.</param>
        /// <returns>
        /// Formated text string.
        /// </returns>
        public string MakeUserLink(User user)
        {
            try
            {
                return $"[{user.FirstName.Replace('[', '<').Replace(']', '>')}](tg://user?id={user.Id})";
            }
            catch (NullReferenceException)//If FirstName is null using id as name
            {
                return $"[{user.Id}](tg://user?id={user.Id})";
            }

        }

        /// <summary>
        /// Resctrict user.
        /// </summary>
        /// <param name="chatid">Chat id.</param>
        /// <param name="userid">User id.</param>
        /// <param name="until">Rescrict until.</param>
        /// <param name="canWriteMessages">Can write messages or not.</param>
        public Task RestrictUserAsync(long chatid, int userid, DateTime until, bool canWriteMessages = false)
        {
            var permissions = new ChatPermissions();
            try
            {
                permissions.CanAddWebPagePreviews = canWriteMessages;
                permissions.CanChangeInfo = canWriteMessages;
                permissions.CanInviteUsers = canWriteMessages;
                permissions.CanPinMessages = canWriteMessages;
                permissions.CanSendMediaMessages = canWriteMessages;
                permissions.CanSendMessages = canWriteMessages;
                permissions.CanSendOtherMessages = canWriteMessages;
                permissions.CanSendPolls = canWriteMessages;

                return RestrictChatMemberAsync(
                    chatId: chatid,
                    userId: userid,
                    untilDate: until,
                    permissions: permissions);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
                return null;
            }
        }

        public Task SaveFileAsync(string fileId, string folder, string fileName = null)
        {
            return Task.Run(() => SaveFile(fileId, folder, fileName));
        }

        private async void SaveFile(string fileId, string folder, string fileName = null)
        {
            try
            {
                var file = GetFileAsync(fileId).Result;
                var stream = new MemoryStream();

                const int attempts = 5;
                for (var a = 0; a < attempts; a++)
                {
                    try
                    {
                        await DownloadFileAsync(file.FilePath, stream);
                        break;
                    }
                    catch (HttpRequestException)
                    {
                        Logger.Log(LogType.Info, $"<Downloader>: Bad Request, attempt #{a}");
                    }
                }


                var fileExt = file.FilePath.Split('.')[1];
                fileName ??= $"{fileId}.{fileExt}";

                if (!Directory.Exists($"./{folder}/"))
                    Directory.CreateDirectory($"./{folder}/");
                await using (var fileStream = new FileStream($"./{folder}/{fileName}", 
                    FileMode.Create, FileAccess.Write))
                {
                    stream.WriteTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
                Logger.Log(LogType.Info, $"<Downloader>: Filename: {fileName} saved.");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
        }
        public Task FullyRestrictUserAsync(ChatId chatId, int userId, int forSeconds = 40)
        {
            var until = DateTime.UtcNow.AddSeconds(forSeconds);
            return RestrictUserAsync(chatId.Identifier, userId, until);
        }
    }
}
