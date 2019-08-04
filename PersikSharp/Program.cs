using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using Clarifai.API;
using Clarifai.DTOs.Inputs;
using System.IO;
using Clarifai.API.Requests.Models;
using Clarifai.DTOs.Predictions;
using Newtonsoft.Json;
using System.Net;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http;
using System.Diagnostics;
using System.Reflection;
using Telegram.Bot.Args;
using PersikSharp.Tables;
using System.Globalization;

namespace PersikSharp
{
    class Program
    {
        public static TelegramBotClient Bot;
        static Perchik perchik;
        static ClarifaiClient clarifai;
        static BotCallBacks botcallbacks;
        static StringManager strManager = new StringManager();
        static StringManager tokens = new StringManager();

        static PerschikDB database = new PerschikDB("database.db");

        static CancellationTokenSource exitTokenSource = new CancellationTokenSource();
        static CancellationToken exit_token = exitTokenSource.Token;

        const long offtopia_id = -1001125742098;
        static string ApplicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        static void Main(string[] args)
        {

            Logger.Log(LogType.Info, $"Bot version: {Perchik.BotVersion}");
            CloseAnotherInstance();

            CommandLine.Inst().onSubmitAction += PrintString;
            CommandLine.Inst().StartUpdating();

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();
            database.Create();
            Init();

            //Update Message to group and me
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "/u":
                            const int via_tcp_Id = 204678400;
                            string version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;

                            Bot.SendTextMessageAsync(via_tcp_Id,
                                $"*Updated to version: {version}*",
                                ParseMode.Markdown);
                            Bot.SendTextMessageAsync(offtopia_id,
                                $"*Updated to version: {version}*",
                                ParseMode.Markdown);
                            break;
                        case "/min":
                            ConsoleWindow.HideConsole();
                            break;
                    }
                }
            }


            Console.Title = botcallbacks.Me.FirstName;

            try
            {
                Bot.StartReceiving(Array.Empty<UpdateType>());
                Logger.Log(LogType.Info, $"Start listening for @{botcallbacks.Me.Username}");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"Exeption: {e.Message}");
                Console.ReadKey();
            }

            ConsoleWindow.StartTrayAsync();

            while (!exit_token.IsCancellationRequested)
            {
                StartDatabaseCheck(null);
                Thread.Sleep(5000);
            }

            Bot.StopReceiving();
            CommandLine.Inst().StopUpdating();
        }

        private static void LoadDictionary()
        {
            try
            {
                strManager.Open("./Configs/dict.json");
                tokens.Open(strManager["TOKENS_PATH"]);
            }
            catch (FileNotFoundException fe)
            {
                Logger.Log(LogType.Fatal, $"No dictionary file found! Exception: {fe.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
            catch (JsonReaderException jre)
            {
                Logger.Log(LogType.Fatal, $"Error parsing dictionary file! Exception: {jre.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"<{e.Source}> {e.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private static void CloseAnotherInstance()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception)
            {
                Logger.Log(LogType.Error, $"Unable to terminate another instance. Make it manualy.");
            }
        }

        private static void Init()
        {
            try
            {
                perchik = new Perchik();
                Bot = new TelegramBotClient(tokens["TELEGRAM"]);
                clarifai = new ClarifaiClient(tokens["CLARIFAI"]);
                if (clarifai.HttpClient.ApiKey == string.Empty)
                    throw new ArgumentException("CLARIFAI token isn't valid!");

                botcallbacks = new BotCallBacks(Bot);
            }
            catch (FileNotFoundException e)
            {
                Logger.Log(LogType.Fatal, $"No tokens file found! Exception: {e.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"<{e.Source}> {e.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }

            perchik.AddCommandRegEx(@"(?<ban>\b(за)?бань?\b)\s?(?<number>\d{1,9})?\s?(?<letter>[смчд](\w+)?)?\s?(?<comment>[\w\W\s]+)?", onPersikBanCommand);                                    //забань
            perchik.AddCommandRegEx(@"\bра[зс]бань?\b", onPersikUnbanCommand);                                 //разбань
            perchik.AddCommandRegEx(@"\bкик\b", onKickCommand);
            perchik.AddCommandRegEx(@"(?!\s)(?<first>[\W\w\s]+)\sили\s(?<second>[\W\w\s]+)(?>\s)?", onRandomChoice);                             //один ИЛИ два
            perchik.AddCommandRegEx(@"погода\s([\w\s]+)", onWeather);                                          //погода ГОРОД
            perchik.AddCommandRegEx(@"\b(дур[ао]к|пид[аоэ]?р|говно|д[еыи]бил|г[оа]ндон|лох|хуй|чмо|скотина)\b", onBotInsulting);//CENSORED
            perchik.AddCommandRegEx(@"\b(живой|красавчик|молодец|хороший|умный|умница)\b", onBotPraise);       //
            perchik.AddCommandRegEx(@"\bрулетк[уа]?\b", onRouletteCommand);                                    //рулетка
            perchik.AddCommandRegEx(@"инфо\s(?<name>[\w\W\s]+)", onStatisticsCommand);
            perchik.onNoneMatched += onNoneCommandMatched;


            botcallbacks.RegisterRegEx(strManager["BOT_REGX"], onPersikCommand);
            botcallbacks.RegisterRegEx(@".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?", onByWord);
            botcallbacks.RegisterRegEx("420|трав(к)?а|шишки|марихуана", (_, e) =>
            {
                Bot.SendStickerAsync(e.Message.Chat.Id,
                    "CAADAgAD0wMAApzW5wrXuBCHqOjyPQI",
                    replyToMessageId: e.Message.MessageId);
            });

            Bot.OnMessage += DatabaseUpdate;
            botcallbacks.onTextMessage += onTextMessage;
            botcallbacks.onTextMessage += onPerchikReplyTrigger;
            botcallbacks.onPhotoMessage += onPhotoMessage;
            botcallbacks.onStickerMessage += onStickerMessage;
            botcallbacks.onChatMembersAddedMessage += onChatMembersAddedMessage;
            botcallbacks.onDocumentMessage += onDocumentMessage;

            botcallbacks.RegisterCommand("start", onStartCommand);
            botcallbacks.RegisterCommand("info", onInfoCommand);
            botcallbacks.RegisterCommand("rate", onRateCommand);
            botcallbacks.RegisterCommand("me", onMeCommand);
            botcallbacks.RegisterCommand("upal_otjalsa", onUpalOtjalsaCommand);
            botcallbacks.RegisterCommand("version", onVersionCommand);
            botcallbacks.RegisterCommand("pickle", onPickleCommand);
            botcallbacks.RegisterCommand("stk", onStickerCommand);
            botcallbacks.RegisterCommand("topbans", onTopBansCommand);
            botcallbacks.RegisterCommand("top", onTopCommand);
            botcallbacks.RegisterCallbackQuery("update_rate", onRateUpdate);

            botcallbacks.RegisterCommand("button", onTestCommand);
        }

        static void StartDatabaseCheck(object s)
        {
            HandleDbRestrictions();
        }

        public static async void PrintString(object sender, CommandLineEventArgs e)
        {
            CommandLine.Text = string.Empty;
            string str = e.Text;

            var match = Regex.Match(str, @"ban:(.*):(.*):", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                try
                {
                    var until = DateTime.Now.AddSeconds(int.Parse(match.Groups[2].Value));
                    await Bot.RestrictChatMemberAsync(
                        chatId: offtopia_id,
                        userId: int.Parse(match.Groups[1].Value),
                        untilDate: until,
                        canSendMessages: false,
                        canSendMediaMessages: false,
                        canSendOtherMessages: false,
                        canAddWebPagePreviews: false);
                }
                catch (Exception exp)
                {
                    Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
                }

                Logger.Log(LogType.Info, $"User {match.Groups[1].Value} - RESTRICTED!");
                return;
            }
            if (str[0] == '!')
            {
                await Bot.SendTextMessageAsync(offtopia_id, str.Substring(1, str.Length - 1), ParseMode.Markdown);
                Logger.Log(LogType.Info, $"(ME) {str}");
                return;
            }
            if (str.Contains("exit"))
                exitTokenSource.Cancel();
            else
                Logger.Log(LogType.Info, $"{str}  <- Syntax Error!");
        }

        private static Task FullyRestrictUserAsync(ChatId chatId, int userId, int forSeconds = 40)
        {
            var until = DateTime.Now.AddSeconds(forSeconds);
            return Bot.RestrictChatMemberAsync(
                            chatId: chatId,
                            userId: userId,
                            untilDate: until,
                            canSendMessages: false,
                            canSendMediaMessages: false,
                            canSendOtherMessages: false,
                            canAddWebPagePreviews: false);
        }

        static async void HandleDbRestrictions()
        {
            try
            {
                var users = database.GetRowsByFilterAsync<DbUser>(u => u.RestrictionId != null).Result;
                if (users.Count != 0)
                {
                    foreach (DbUser user in users)
                    {
                        var restrictions = await database.GetRowsByFilterAsync<DbRestriction>(r => r.Id == user.RestrictionId);
                        if (restrictions.Count != 0)
                        {
                            DbRestriction restriction = restrictions[0];
                            DateTime to = DateTime.Parse(restriction.DateTimeTo);

                            if (DateTime.Now > to)
                            {
                                user.RestrictionId = null;
                                _ = database.InsertOrReplaceRowAsync(user);
                                _ = Bot.SendTextMessageAsync(
                                    chatId: restriction.ChatId,
                                    text: string.Format(strManager["UNBANNED"], $"[{user.FirstName}](tg://user?id={user.Id})"),
                                    parseMode: ParseMode.Markdown);
                            }
                        }
                        else
                        {
                            user.RestrictionId = null;
                            _ = database.InsertOrReplaceRowAsync(user);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        //=====Persik Commands======
        private static async void onPersikCommand(object s, RegExArgs e)
        {
            Message message = e.Message;
            if (message.ReplyToMessage?.Type == MessageType.Photo)
            {
                Logger.Log(LogType.Info,
                    $"[{message.Chat.Type.ToString()}:{message.Type.ToString()}]({message.From.FirstName}:{message.From.Id}) Predict IID: {message.ReplyToMessage.Photo[0].FileId}");

                var names = await PredictImage(message.ReplyToMessage.Photo[message.ReplyToMessage.Photo.Length - 1]);

                _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetSingle("PREDICTION"), message.From.FirstName, names[0], names[1], names[2]),
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: message.MessageId);

                Logger.Log(LogType.Info, $"Result: {names[0]}:{names[1]}:{names[2]}. IID: {message.ReplyToMessage.Photo[0].FileId}");

                return;
            }

            if (message.ReplyToMessage?.From.Id != botcallbacks.Me.Id)
                perchik.ParseMessage(message);
        }

        private static void onPerchikReplyTrigger(object sender, MessageArgs e)
        {
            if (e.Message.Chat.Type == ChatType.Private)
                return;
            if (e.Message.ReplyToMessage == null)
                return;

            if (e.Message.ReplyToMessage.From.Id == Bot.GetMeAsync().Result.Id)
                perchik.ParseMessage(e.Message);
        }

        private static void onWeather(object sender, RegExArgs a)//Переделать под другой АПИ
        {
            Message message = a.Message;
            Match weather_match = a.Match;

            string search_url = Uri.EscapeUriString(
                $"http://dataservice.accuweather.com/locations/v1/cities/search?apikey={tokens["ACCUWEATHER"]}&q={weather_match.Groups[1].Value}&language=ru");
            int location_code = 0;
            dynamic location_json;
            dynamic weather_json;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(search_url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream resStream = response.GetResponseStream();

                StreamReader reader = new StreamReader(resStream);
                string respone_str = reader.ReadToEnd();

                if (respone_str.Contains("The allowed number of requests has been exceeded."))
                {
                    _ = Bot.SendTextMessageAsync(
                         chatId: message.Chat.Id,
                         text: $"*Количество запросов превышено, лол!*",
                         parseMode: ParseMode.Markdown,
                         replyToMessageId: message.MessageId);
                    return;
                }

                location_json = JsonConvert.DeserializeObject(respone_str);

                location_code = location_json[0].Key;

                string current_url = $"http://dataservice.accuweather.com/currentconditions/v1/{location_code}?apikey={tokens["ACCUWEATHER"]}&language=ru";

                request = (HttpWebRequest)WebRequest.Create(current_url);
                response = (HttpWebResponse)request.GetResponse();
                resStream = response.GetResponseStream();

                reader = new StreamReader(resStream);
                respone_str = reader.ReadToEnd();

                weather_json = JsonConvert.DeserializeObject(respone_str);

                _ = Bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"*{location_json[0].LocalizedName}, {location_json[0].Country.LocalizedName}\n\n{weather_json[0].WeatherText}\nТемпература: {weather_json[0].Temperature.Metric.Value}°C*",
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");

                _ = Bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"*Нет такого .. {weather_match.Groups[1].Value.ToUpper()}!!😠*",
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (WebException w)
            {
                Stream resStream = w.Response.GetResponseStream();
                StreamReader reader = new StreamReader(resStream);
                if (reader.ReadToEnd().Contains("The allowed number of requests has been exceeded."))
                {
                    _ = Bot.SendTextMessageAsync(
                           chatId: message.Chat.Id,
                           text: $"*Количество запросов превышено, лол!*",//SMOKE WEED EVERYDAY
                           parseMode: ParseMode.Markdown,
                           replyToMessageId: message.MessageId);
                    return;
                }

                Logger.Log(LogType.Error, $"Exception: {w.Message}");
                _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: w.Message,
                            parseMode: ParseMode.Markdown,
                            replyToMessageId: message.MessageId);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }
        }

        private static void onNoneCommandMatched(object sender, RegExArgs e)
        {
            Logger.Log(LogType.Info, $"<Perchik>({e.Message.From.FirstName}:{e.Message.From.Id}) -> {"NONE"}");
            _ = Bot.SendTextMessageAsync(
                       chatId: e.Message.Chat.Id,
                       text: strManager.GetRandom("HELLO"),
                       parseMode: ParseMode.Markdown,
                       replyToMessageId: e.Message.MessageId);
        }

        private static async void onPersikBanCommand(object sender, RegExArgs e)//Переделать
        {
            Message message = e.Message;

            if (message.Chat.Type == ChatType.Private)
                return;

            const int default_second = 40;
            int seconds = default_second;
            int number = default_second;
            string word = "сек.";
            string comment = "...";

            if (e.Match.Success)
            {
                if (e.Match.Groups["number"].Value != string.Empty)
                {
                    number = int.Parse(e.Match.Groups["number"].Value);
                    seconds = number;
                }

                if (e.Match.Groups["letter"].Value != string.Empty)
                {
                    switch (e.Match.Groups["letter"].Value.First())
                    {
                        case 'с':
                            seconds = number;
                            word = "сек.";
                            break;
                        case 'м':
                            seconds *= 60;
                            word = "мин.";
                            break;
                        case 'ч':
                            word = "ч.";
                            seconds *= 3600;
                            break;
                        case 'д':
                            word = "д.";
                            seconds *= 86400;
                            break;
                    }
                }

                if (e.Match.Groups["comment"].Value != string.Empty)
                {
                    comment = e.Match.Groups["comment"].Value;
                }
            }

            try
            {
                if (message.ReplyToMessage != null)
                {
                    if (!Perchik.isUserAdmin(message.Chat.Id, message.From.Id))
                        return;

                    if (message.ReplyToMessage.From.Id == Bot.BotId)
                        return;

                    await FullyRestrictUserAsync(
                            chatId: message.Chat.Id,
                            userId: message.ReplyToMessage.From.Id,
                            forSeconds: seconds);

                    if (seconds >= 40)
                    {
                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(strManager.GetSingle("BANNED"), Perchik.MakeUserLink(message.ReplyToMessage.From), number, word, comment),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        seconds = int.MaxValue;
                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(strManager.GetSingle("SELF_PERMANENT"), Perchik.MakeUserLink(message.ReplyToMessage.From), number, word, comment),
                            parseMode: ParseMode.Markdown);
                    }

                    _ = database.AddRestrictionAsync(new DbUser()
                    {
                        Id = e.Message.ReplyToMessage.From.Id,
                        FirstName = e.Message.ReplyToMessage.From.FirstName,
                        LastName = e.Message.ReplyToMessage.From.LastName,
                        Username = e.Message.ReplyToMessage.From.Username,
                        LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }, e.Message.Chat.Id, seconds);

                    _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
                else
                {
                    if (seconds >= 40)
                    {
                        await FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                forSeconds: seconds);

                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), Perchik.MakeUserLink(message.From), number, word, comment),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        await FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id);

                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), Perchik.MakeUserLink(message.From), 40, word, comment),
                            parseMode: ParseMode.Markdown);
                    }

                    _ = database.AddRestrictionAsync(new DbUser()
                    {
                        Id = e.Message.From.Id,
                        FirstName = e.Message.From.FirstName,
                        LastName = e.Message.From.LastName,
                        Username = e.Message.From.Username,
                        LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    }, e.Message.Chat.Id, seconds);

                    _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static void onPersikUnbanCommand(object sender, RegExArgs e)
        {
            Message message = e.Message;

            if (message.Chat.Type == ChatType.Private)
                return;
            if (!Perchik.isUserAdmin(message.Chat.Id, message.From.Id))
                return;
            if (message.ReplyToMessage == null)
                return;

            try
            {
                var until = DateTime.Now.AddSeconds(1);
                _ = Bot.RestrictChatMemberAsync(
                    chatId: message.Chat.Id,
                    userId: message.ReplyToMessage.From.Id,
                    untilDate: until,
                    canSendMessages: true,
                    canSendMediaMessages: true,
                    canSendOtherMessages: true,
                    canAddWebPagePreviews: true);

                _ = database.InsertOrReplaceRowAsync(new DbUser()
                {
                    Id = e.Message.ReplyToMessage.From.Id,
                    FirstName = e.Message.ReplyToMessage.From.FirstName,
                    LastName = e.Message.ReplyToMessage.From.LastName,
                    Username = e.Message.ReplyToMessage.From.Username,
                    LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    RestrictionId = null
                });

                _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetRandom("UNBANNED"), Perchik.MakeUserLink(message.ReplyToMessage.From)),
                        parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static async void onKickCommand(object sender, RegExArgs e)
        {
            Message message = e.Message;

            if (message.Chat.Type == ChatType.Private)
                return;
            if (!Perchik.isUserAdmin(message.Chat.Id, message.From.Id))
                return;
            if (message.ReplyToMessage == null)
                return;

            try
            {
                await Bot.KickChatMemberAsync(
                    chatId: message.Chat.Id,
                    userId: message.ReplyToMessage.From.Id);

                _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetRandom("KICK"), Perchik.MakeUserLink(message.ReplyToMessage.From)),
                        parseMode: ParseMode.Markdown).Result;
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static async void onByWord(object sender, RegExArgs e)
        {
            Message message = e.Message;

            await Bot.SendStickerAsync(message.Chat.Id, "CAADAgADGwAD0JwyGF7MX7q4n6d_Ag");
            if (message.Chat.Type != ChatType.Private)
            {
                try
                {
                    await FullyRestrictUserAsync(
                            chatId: message.Chat.Id,
                            userId: message.From.Id,
                            forSeconds: 60 * 5);

                    _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetSingle("BYWORD_BAN"), message.From.FirstName),
                        parseMode: ParseMode.Markdown);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
                }
            }
        }

        private static void onBotPraise(object sender, RegExArgs e)
        {
            Message message = e.Message;
            Bot.SendStickerAsync(message.Chat.Id, "CAADAgADQQMAApFfCAABzoVI0eydHSgC");
        }

        private static async void onBotInsulting(object sender, RegExArgs e)
        {
            Message message = e.Message;
            try
            {
                await Bot.SendStickerAsync(message.Chat.Id, "CAADAgADJwMAApFfCAABfVrdPYRn8x4C");

                if (message.Chat.Type != ChatType.Private)
                {
                    await Task.Delay(2000);

                    await FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                forSeconds: 120);

                    await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetSingle("BANNED"), message.From.FirstName, 2, "мин."),
                        parseMode: ParseMode.Markdown);

                    _ = Bot.SendStickerAsync(message.Chat.Id, "CAADAgADPQMAApFfCAABt8Meib23A_QC");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }

        }

        private static void onRandomChoice(object sender, RegExArgs e)
        {
            Message message = e.Message;

            Regex regx = new Regex(strManager["BOT_REGX"], RegexOptions.IgnoreCase);
            string without_perchik = regx.Replace(message.Text, string.Empty, 1);

            var match = Regex.Match(without_perchik, e.Pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Random rand = new Random();
                string result;
                string first = match.Groups["first"].Value.Replace("?", "");
                string second = match.Groups["second"].Value.Replace("?", ""); ;

                if (rand.NextDouble() >= 0.5)
                {
                    result = first;
                }
                else
                {
                    result = second;
                }
                if (first.Equals(second))
                {
                    result = strManager.GetRandom("CHOICE_EQUAL");
                }

                _ = Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: String.Format(strManager.GetRandom("CHOICE"), result),
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: message.MessageId).Result;
            }
        }

        private static void onRouletteCommand(object sender, RegExArgs e)
        {
            Message message = e.Message;

            if (message.Chat.Type == ChatType.Private)
                return;

            try
            {
                Random rand = new Random(DateTime.Now.Millisecond);
                int random_number = rand.Next(0, 6);
                if (random_number == 3)
                {
                    var until = DateTime.Now.AddSeconds(10 * 60); //10 minutes
                    _ = Bot.RestrictChatMemberAsync(
                            chatId: message.Chat.Id,
                            userId: message.From.Id,
                            untilDate: until,
                            canSendMessages: false,
                            canSendMediaMessages: false,
                            canSendOtherMessages: false,
                            canAddWebPagePreviews: false);

                    _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetRandom("ROULETTEBAN"), Perchik.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown).Result;
                }
                else
                {
                    var msg = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetRandom("ROULETTEMISS"), Perchik.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown).Result;

                    Thread.Sleep(10 * 1000); //wait 10 seconds

                    Bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: msg.MessageId);
                    Bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onStatisticsCommand(object sender, RegExArgs e)
        {
            try
            {
                Message message = e.Message;
                string name = e.Match.Groups["name"].Value;
                string upper_name = name.ToUpper().Replace("@", "");

                var all_users = database.GetRows<DbUser>();
                var users = all_users.Where(u =>
                {
                    if (u.FirstName != null && u.FirstName.ToUpper().Contains(upper_name))
                        return true;
                    if (u.LastName != null && u.LastName.ToUpper().Contains(upper_name))
                        return true;
                    if (u.Username != null && u.Username.ToUpper().Contains(upper_name))
                        return true;

                    return false;
                });

                if (users.Count() == 0)
                {
                    _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"*Пользователя \"{name}\" нет в базе.*",
                            parseMode: ParseMode.Markdown);

                    return;
                }
                DbUser user = users.First();

                var messages = database.GetRowsByFilterAsync<DbMessage>(m => m.Text != null).Result;
                var msgs_from_user = database.GetRowsByFilterAsync<DbMessage>(m => m.UserId == user.Id && m.Text != null).Result;

                var date = DateTime.Now.ToString("yyyy-MM-dd");

                var messages_today = messages.Where(m => m.DateTime.Substring(0, 10) == date);
                var u_messages_today = msgs_from_user.Where(m => m.DateTime.Substring(0, 10) == date);

                int u_messages_lastday_count = msgs_from_user.Where(m => DateTime.Parse(m.DateTime).Day == DateTime.Now.Day - 1).Count(); //TODO: FIX
                int u_messages_today_count = u_messages_today.Count();
                int u_messages_count = msgs_from_user.Count();
                int restrictions_count = database.ExecuteScalarAsync<int>("SELECT count(*) FROM Restrictions WHERE UserId = ?", user.Id).Result;

                double user_activity = 0;
                if (u_messages_today_count != 0)
                {
                    int total_text_length = messages_today.Sum(m => m.Text.Length);
                    int user_text_length = u_messages_today.Sum(m => m.Text.Length);
                    user_activity = (double)user_text_length / total_text_length;
                }

                _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text:
                            $"*Имя: {user.FirstName} {user.LastName}\n" +
                            $"ID: {user.Id}\n" +
                            $"Ник: {user.Username}\n\n" +
                            string.Format("Активность: {0:F2}%\n", user_activity * 100) +
                            $"Сообщений сегодня: { u_messages_today_count }\n" +
                            $"Сообщений вчера: { u_messages_lastday_count }\n" +
                            $"Всего сообщений: { u_messages_count }\n\n" +
                            $"Банов: { restrictions_count }\n" +
                            $"Забанен: { user.RestrictionId != null }\n" +
                            $"*",
                            parseMode: ParseMode.Markdown).Result;
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static void onTopCommand(object sender, CommandEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Message message = e.Message;

            var users = database.GetRows<DbUser>();
            Dictionary<DbUser, double> users_activity = new Dictionary<DbUser, double>();

            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var messages_today = database.GetRowsByFilterAsync<DbMessage>(m => m.DateTime.Contains(date)).Result;

            IEnumerable<DbMessage> u_messages_today;
            int total_symbols = 0;

            foreach (var user in users)
            {
                u_messages_today = messages_today.Where(m => m.UserId == user.Id && m.DateTime.Substring(0, 10) == date);

                if (u_messages_today.Count() == 0)
                {
                    continue;
                }

                double user_activity = 0;
                int total_text_length = messages_today.Sum(m =>
                {
                    if (m.Text != null)
                        return m.Text.Length;
                    else
                        return 0;
                });
                total_symbols += total_text_length;

                int user_text_length = u_messages_today.Sum(m =>
                {
                    if (m.Text != null)
                        return m.Text.Length;
                    else
                        return 0;
                });

                user_activity = (double)user_text_length / total_text_length;
                users_activity.Add(user, user_activity);
            }

            var users_inorder = users_activity.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            string msg_string = "*Топ 10 по активности за сегодня:*\n";
            for (int i = 0; i < 10 && i < users_inorder.Count; i++)
            {
                int dict_index = users_inorder.Count - 1 - i;

                DbUser user = users_inorder.ElementAt(dict_index).Key;
                string first_name = user.FirstName?.Replace('[', '{').Replace(']', '}');
                string last_name = user.LastName?.Replace('[', '{').Replace(']', '}');
                string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);
                double activity = users_inorder.ElementAt(dict_index).Value;

                msg_string += string.Format("{0}. {1} -- {2:F2}%\n", i + 1, full_name, activity * 100);
            }

            stopwatch.Stop();

            _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"{msg_string}\n`Всего символов:{total_symbols}\n{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                            parseMode: ParseMode.Markdown).Result;
        }

        private static void onTopBansCommand(object sender, CommandEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Message message = e.Message;

            var users = database.GetRows<DbUser>();
            Dictionary<DbUser, int> users_bans = new Dictionary<DbUser, int>();
            foreach (var user in users)
            {
                int restrictions_count = database.ExecuteScalarAsync<int>("SELECT count(*) FROM Restrictions WHERE UserId = ?", user.Id).Result;
                users_bans.Add(user, restrictions_count);
            }
            var user_bans_ordered = users_bans.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            string msg_string = "*Топ 10 по банам:*\n";
            for (int i = 0; i < 10 && i < user_bans_ordered.Count; i++)
            {
                int dict_index = user_bans_ordered.Count - 1 - i;
                DbUser user = user_bans_ordered.ElementAt(dict_index).Key;
                string first_name = user.FirstName?.Replace('[', '{').Replace(']', '}');
                string last_name = user.LastName?.Replace('[', '{').Replace(']', '}');
                string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);
                int bans = user_bans_ordered.ElementAt(dict_index).Value;
                msg_string += $"{i + 1}. {full_name} -- {bans}\n";
            }

            stopwatch.Stop();

            _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"{msg_string}\n`{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                            parseMode: ParseMode.Markdown).Result;
        }


        private static async Task<List<string>> PredictImage(PhotoSize ps)
        {
            var file = await Bot.GetFileAsync(ps.FileId);
            MemoryStream photo = new MemoryStream();
            await Bot.DownloadFileAsync(file.FilePath, photo);


            ClarifaiFileImage file_image = new ClarifaiFileImage(photo.GetBuffer());
            PredictRequest<Concept> request =
                clarifai.PublicModels.GeneralModel.Predict(file_image, language: "ru");
            var result = await request.ExecuteAsync();

            List<string> predictions = new List<string>();
            for (int i = 0; predictions.Count < 3; i++)
            {
                if (result.Get().Data[i].Name != "нет человек")
                    predictions.Add(result.Get().Data[i].Name);
            }

            return predictions;
        }

        private static async void NSFWDetect(Message message)//Упростить
        {
            const bool ENABLE_FILTER = true;

            try
            {
                var file = await Bot.GetFileAsync(message.Photo[message.Photo.Length - 1].FileId);
                MemoryStream photo = new MemoryStream();
                await Bot.DownloadFileAsync(file.FilePath, photo);

                ClarifaiFileImage file_image = new ClarifaiFileImage(photo.GetBuffer());
                PredictRequest<Concept> request = clarifai.PublicModels.NsfwModel.Predict(file_image, language: "en");
                var result = await request.ExecuteAsync();
                var nsfw_val = result.Get().Data.Find(x => x.Name == "nsfw").Value;

                if ((float)nsfw_val > 0.7)
                {
                    await Perchik.SaveFileAsync(file.FileId, "nsfw");

                    if (ENABLE_FILTER)
                    {
                        _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                        if (message.Chat.Type != ChatType.Private)
                        {
                            var until = DateTime.Now.AddSeconds(120);
                            await Bot.RestrictChatMemberAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                untilDate: until,
                                canSendMessages: false,
                                canSendMediaMessages: false,
                                canSendOtherMessages: false,
                                canAddWebPagePreviews: false);

                            await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: String.Format(strManager.GetSingle("NSFW_TRIGGER"), message.From.FirstName, 2, 1 - nsfw_val),
                              parseMode: ParseMode.Markdown);
                        }
                    }
                }
                else
                {
                    await Perchik.SaveFileAsync(file.FileId, "photos");
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        //======Bot Updates=========

        private static async void onDocumentMessage(object sender, MessageArgs e)
        {
            Message message = e.Message;

            try
            {
                await Perchik.SaveFileAsync(message.Document.FileId, "documents", message.Document.FileName);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }


        private static void onTextMessage(object sender, MessageArgs message_args)
        {
            Message m = message_args.Message;

            //Message to superchat from privat Example: !Hello World
            if (m.Chat.Type == ChatType.Private && m.Text[0] == '!')
            {
                if (Perchik.isUserAdmin(-1001125742098, m.From.Id))
                {
                    string msg = m.Text.Substring(1, m.Text.Length - 1);
                    _ = Bot.SendTextMessageAsync(offtopia_id, $"*{msg}*", ParseMode.Markdown);

                    Logger.Log(LogType.Info, $"({m.From.FirstName}:{m.From.Id})(DM): {msg}");
                }
            }
        }

        private static void DatabaseUpdate(object s, MessageEventArgs e)
        {
            try
            {
                DateTime myDateTime = DateTime.Now;
                string sqlFormattedDate = myDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                database.InsertRowAsync(new DbMessage()
                {
                    Id = e.Message.MessageId,
                    UserId = e.Message.From.Id,
                    Text = e.Message.Text,
                    DateTime = sqlFormattedDate
                });
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onPhotoMessage(object sender, MessageArgs message_args)
        {
            Message message = message_args.Message;

            NSFWDetect(message);
        }

        private static void onStickerMessage(object sender, MessageArgs message_args)
        {
            Message message = message_args.Message;

        }

        private static void onChatMembersAddedMessage(object sender, MessageArgs message_args)
        {
            Message message = message_args.Message;

            string username = "Ноунейм";
            string firstName = "";
            string lastName = "";
            if (message.From.Username != null)
            {
                username = $"@{message.From.Username}";
            }
            else
            {
                if (message.From.FirstName != null)
                {
                    username = message.From.FirstName;
                    firstName = message.From.FirstName;
                }
                if (message.From.LastName != null)
                {
                    lastName = message.From.LastName;
                }
            }


            ///Spam Bot detection
            if (Regex.IsMatch(firstName, @"\b[bб6][оo][т7t]\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lastName, @"\b[bб6][оo][т7t]\b", RegexOptions.IgnoreCase))
            {
                _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                _ = Bot.SendTextMessageAsync(message.Chat.Id, String.Format(strManager.GetSingle("BOT_DETECTED"), username));

                var until = DateTime.Now.AddSeconds(1);
                _ = Bot.RestrictChatMemberAsync(
                            chatId: message.Chat.Id,
                            userId: message.From.Id,
                            untilDate: until,
                            canSendMessages: false,
                            canSendMediaMessages: false,
                            canSendOtherMessages: false,
                            canAddWebPagePreviews: false);
            }

            var user = database.GetRowsByFilterAsync<DbUser>(u => u.Id == message.From.Id).Result;
            if (user.Count > 0)
            {
                DateTime myDateTime = DateTime.Now;
                string sqlFormattedDate = myDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                database.InsertOrReplaceRowAsync(new DbUser()
                {
                    Id = message.From.Id,
                    FirstName = message.From.FirstName,
                    LastName = message.From.LastName,
                    Username = message.From.Username,
                    LastMessage = sqlFormattedDate,
                    RestrictionId = null
                });

                _ = Bot.RestrictChatMemberAsync(
                        chatId: message.Chat.Id,
                        userId: message.From.Id,
                        untilDate: DateTime.Now.AddYears(420),
                        canSendMessages: false,
                        canSendMediaMessages: false,
                        canSendOtherMessages: false,
                        canAddWebPagePreviews: false);
                
                var human_button = new InlineKeyboardButton();
                human_button.CallbackData = Path.GetRandomFileName();
                human_button.Text = strManager["CAPTCHA_HUMAN"];

                var bot_button = new InlineKeyboardButton();
                bot_button.CallbackData = Path.GetRandomFileName();
                bot_button.Text = strManager["CAPTCHA_BOT"];

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { human_button, bot_button }});

                Bot.SendTextMessageAsync(
                     chatId: message_args.Message.Chat.Id,
                     replyMarkup: inlineKeyboard,
                     text: string.Format(strManager["CAPTCHA"], Perchik.MakeUserLink(message.From)),
                     parseMode: ParseMode.Markdown);

                botcallbacks.RegisterCallbackQuery(human_button.CallbackData, message.From.Id, onBotCheckButtonNoBot);
                botcallbacks.RegisterCallbackQuery(bot_button.CallbackData, message.From.Id, onBotCheckButtonBot);
            }
            else
            {
                string msg_string = String.Format(strManager.GetRandom("NEW_MEMBERS"), username);
                _ = Bot.SendTextMessageAsync(message.Chat.Id, msg_string);
            }
        }
        private static void onBotCheckButtonNoBot(object sender, CallbackQueryArgs c)
        {
            //Bot.UnbanChatMemberAsync(
            //    chatId: c.Callback.Message.Chat.Id,
            //    userId: c.Callback.From.Id);

            _ = Bot.RestrictChatMemberAsync(
                        chatId: c.Callback.Message.Chat.Id,
                        userId: c.Callback.From.Id,
                        untilDate: DateTime.Now.AddSeconds(1),
                        canSendMessages: true,
                        canSendMediaMessages: true,
                        canSendOtherMessages: true,
                        canAddWebPagePreviews: true);

            Bot.DeleteMessageAsync(
                chatId: c.Callback.Message.Chat.Id,
                messageId: c.Callback.Message.MessageId);

            string username = "Ноунейм";
            string firstName = "";
            string lastName = "";
            if (c.Callback.Message.From.Username != null)
            {
                username = $"@{c.Callback.Message.From.Username}";
            }
            else
            {
                if (c.Callback.Message.From.FirstName != null)
                {
                    username = c.Callback.Message.From.FirstName;
                    firstName = c.Callback.Message.From.FirstName;
                }
                if (c.Callback.Message.From.LastName != null)
                {
                    lastName = c.Callback.Message.From.LastName;
                }
            }

            string msg_string = String.Format(strManager.GetRandom("NEW_MEMBERS"), username);
            _ = Bot.SendTextMessageAsync(c.Callback.Message.Chat.Id, msg_string);

            botcallbacks.RemoveCallbackQuery(c.Callback.Data);
        }

        private static void onBotCheckButtonBot(object sender, CallbackQueryArgs c)
        {
            Bot.DeleteMessageAsync(
                chatId: c.Callback.Message.Chat.Id,
                messageId: c.Callback.Message.MessageId);

            Bot.KickChatMemberAsync(
                chatId: c.Callback.Message.Chat.Id,
                userId: c.Callback.From.Id);

            Bot.UnbanChatMemberAsync(
                chatId: c.Callback.Message.Chat.Id,
                userId: c.Callback.From.Id);

            Bot.SendTextMessageAsync(
                  chatId: c.Callback.Message.Chat.Id,
                  text: $"{ Perchik.MakeUserLink(c.Callback.Message.From)} сказал что бот и ушел из чата!",
                  parseMode: ParseMode.Markdown);

            botcallbacks.RemoveCallbackQuery(c.Callback.Data);
        }

        //=======Bot commands========
        private static void onRateCommand(object sender, CommandEventArgs message_args)
        {
            CallbackQuery cq;
            try
            {
                var msg = Bot.SendTextMessageAsync(
                              chatId: message_args.Message.Chat.Id,
                              text: "*Обновление...*",
                              parseMode: ParseMode.Markdown).Result;

                cq = new CallbackQuery();
                cq.Message = msg;
                cq.InlineMessageId = msg.MessageId.ToString();
                cq.From = msg.From;

                onRateUpdate(sender, new CallbackQueryArgs(cq));
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }


        }

        private static void onRateUpdate(object sender, CallbackQueryArgs e)
        {
            try
            {
                string url = "https://min-api.cryptocompare.com/data/pricemultifull?fsyms=BTC,ETH,ETC,ZEC,LTC,BCH&tsyms=USD";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream resStream = response.GetResponseStream();

                StreamReader reader = new StreamReader(resStream);
                string respone_str = reader.ReadToEnd();

                var json_object = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>();
                json_object = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>>(respone_str);


                string template_str = "1 {0} = {1}$ ({2:f2}% / 24h){3}\n";
                string formated_str = "";

                foreach (var curr in json_object["RAW"])
                {
                    string CURRENCY_SYMBOL = curr.Key;
                    float CHANGEPCT24HOUR = json_object["RAW"][curr.Key]["USD"]["CHANGEPCT24HOUR"];
                    float PRICE = json_object["RAW"][curr.Key]["USD"]["PRICE"];


                    string symbol = "💹";
                    if (CHANGEPCT24HOUR < 0)
                        symbol = "🔻";

                    formated_str += String.Format(template_str, CURRENCY_SYMBOL, PRICE, CHANGEPCT24HOUR, symbol);
                }
                formated_str += $"\nОбновлено {DateTime.Now.ToShortTimeString()}";


                var button = new InlineKeyboardButton();
                button.CallbackData = "update_rate";
                button.Text = strManager.GetSingle("RATE_UPDATE_BTN");
                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

                _ = Bot.EditMessageTextAsync(
                     chatId: e.Callback.Message.Chat.Id,
                     messageId: e.Callback.Message.MessageId,
                     replyMarkup: inlineKeyboard,
                     text: formated_str).Result;
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static void onStartCommand(object sender, CommandEventArgs message_args)
        {
            try
            {
                if (message_args.Message.Chat.Type == ChatType.Private)
                    _ = Bot.SendTextMessageAsync(
                              chatId: message_args.Message.Chat.Id,
                              text: String.Format(strManager.GetSingle("START"), Perchik.MakeUserLink(message_args.Message.From)),
                              parseMode: ParseMode.Markdown).Result;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }
        }

        private static void onInfoCommand(object sender, CommandEventArgs message_args)
        {
            Message message = message_args.Message;

            _ = Bot.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: StringManager.StringFromFile(strManager["INFO_PATH"]),
                       parseMode: ParseMode.Markdown).Result;
        }
        private static void onMeCommand(object sender, CommandEventArgs message_args)
        {
            if (message_args.Text == "")
                return;

            Message message = message_args.Message;
            string msg_text = $"{Perchik.MakeUserLink(message.From)} *{message_args.Text}*";

            try
            {
                Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                if (message.ReplyToMessage != null)
                {
                    _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: msg_text,
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: message.ReplyToMessage.MessageId).Result;
                }
                else
                {
                    _ = Bot.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: msg_text,
                       parseMode: ParseMode.Markdown).Result;
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }
        }

        private static void onUpalOtjalsaCommand(object sender, CommandEventArgs e)
        {
            try
            {
                using (var stream = System.IO.File.OpenRead("kek.mp3"))
                {
                    _ = Bot.SendAudioAsync(
                      chatId: e.Message.Chat,
                      audio: stream,
                      performer: "Жизнь",
                      title: "Не слушать!"
                    ).Result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onPickleCommand(object sender, CommandEventArgs e)
        {
            try
            {
                using (var stream = System.IO.File.OpenRead("P_20190512_225535_BF.jpg"))
                {
                    _ = Bot.SendPhotoAsync(
                      chatId: e.Message.Chat,
                      photo: stream
                    ).Result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onVersionCommand(object sender, CommandEventArgs e)
        {
            try
            {
                _ = Bot.SendTextMessageAsync(
                       chatId: e.Message.Chat.Id,
                       text: $"*Version: {Perchik.BotVersion}*",
                       parseMode: ParseMode.Markdown).Result;
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onStickerCommand(object sender, CommandEventArgs e)
        {
            if (e.Message.Chat.Type != ChatType.Private)
                return;

            if (!Perchik.isUserAdmin(offtopia_id, e.Message.From.Id))
                return;

            try
            {
                _ = Bot.SendTextMessageAsync(
                         chatId: e.Message.Chat.Id,
                         text: strManager.GetRandom("STK"),
                         parseMode: ParseMode.Markdown,
                         replyMarkup: new ForceReplyMarkup()).Result;
                botcallbacks.RegisterNextstep(onStickerAnswer, e.Message);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onStickerAnswer(object sender, NextstepArgs e)
        {
            try
            {
                if (e.Message.Type == MessageType.Sticker)
                {

                    _ = Bot.SendTextMessageAsync(
                            chatId: e.Message.Chat.Id,
                            text: strManager.GetRandom("STK_OK"),
                            parseMode: ParseMode.Markdown).Result;
                    _ = Bot.SendStickerAsync(
                        chatId: offtopia_id,
                        sticker: e.Message.Sticker.FileId);

                    botcallbacks.RemoveNextstepCallback(e.Message);
                }
                else
                {
                    if (Perchik.FindTextCommand(e.Message.Text, "stop"))
                    {
                        _ = Bot.SendTextMessageAsync(
                           chatId: e.Message.Chat.Id,
                           text: strManager.GetRandom("STK_CANCEL"),
                           parseMode: ParseMode.Markdown).Result;

                        return;
                    }

                    _ = Bot.SendTextMessageAsync(
                            chatId: e.Message.Chat.Id,
                            text: strManager.GetRandom("STK_WRONG"),
                            parseMode: ParseMode.Markdown,
                            replyMarkup: new ForceReplyMarkup()).Result;
                    botcallbacks.RegisterNextstep(onStickerAnswer, e.Message);
                }

            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
        private static void onTestCommand(object sender, CommandEventArgs message_args)
        {
            var button = new InlineKeyboardButton();
            button.CallbackData = Path.GetRandomFileName();
            button.Text = "TEST BUTTON";
            var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

            _ = Bot.SendTextMessageAsync(
                 chatId: message_args.Message.Chat.Id,
                 replyMarkup: inlineKeyboard,
                 text: "TestMessage").Result;

            botcallbacks.RegisterCallbackQuery(button.CallbackData, message_args.Message.From.Id, onTestButtonPress);
        }
        private static void onTestButtonPress(object sender, CallbackQueryArgs c)
        {
            Bot.DeleteMessageAsync(
                chatId: c.Callback.Message.Chat.Id,
                messageId: c.Callback.Message.MessageId);

           Bot.SendTextMessageAsync(
                 chatId: c.Callback.Message.Chat.Id,
                 text: $"Message from right user. Button data: {c.Callback.Data}");

            botcallbacks.RemoveCallbackQuery(c.Callback.Data);
        }
        //==========================
    }
}