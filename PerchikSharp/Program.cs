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
using System.Net.Http.Headers;
using System.Web;
using PerchikSharp.Db;
using Microsoft.EntityFrameworkCore;
using PersikSharp;
using PerchikSharp.Commands;

namespace PerchikSharp
{
    class Program
    {
        public static TelegramBotClient Bot;
        static ClarifaiClient clarifai;
        static BotHelper bothelper;
        public static StringManager strManager = new StringManager();
        static StringManager tokens = new StringManager();

        static RegExParser perchikregex;
        static RegExParser commands;

        static PerschikDB database;

        static CancellationTokenSource exitTokenSource = new CancellationTokenSource();
        static CancellationToken exit_token = exitTokenSource.Token;

        const long offtopia_id = -1001125742098;
        const int via_tcp_Id = 204678400;
        static string ApplicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);


        static List<long> votebanning_groups = new List<long>();

        static void Main(string[] args)
        {

            Logger.Log(LogType.Info, $"Bot version: {BotHelper.BotVersion}");

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            FileInfo file = new FileInfo("./Data/");
            file.Directory.Create();

            database = new PerschikDB("./Data/database.db");
            database.Create();
            PerchikDB.ConnectionString = tokens["MYSQL"];

            perchikregex = new RegExParser();
            commands = new RegExParser();

            Init();

            //Update Message to group and me
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "--update":
                            string version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
                            string changelog = string.Empty;
                            try
                            {
                                changelog = $"\n\n*Изменения:*\n{StringManager.FromFile("changelog.txt")}";
                                System.IO.File.Delete("changelog.txt");
                            }catch(FileNotFoundException)
                            {
                                
                            }

                            string text = $"*Перчик жив! 🌶*\nВерсия: {version}{changelog}";
                            _ = Bot.SendTextMessageAsync(via_tcp_Id,
                                                         text,
                                                         ParseMode.Markdown);
                            _ = Bot.SendTextMessageAsync(offtopia_id,
                                                         text,
                                                         ParseMode.Markdown);
                            break;
                        case "--close":
                            return;
                    }
                }
            }


            Console.Title = bothelper.Me.FirstName;

            try
            {
                Bot.StartReceiving(Array.Empty<UpdateType>());
                Logger.Log(LogType.Info, $"Start listening for @{bothelper.Me.Username}");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"Exeption: {e.Message}");
                Console.ReadKey();
            }

            //ConsoleWindow.StartTrayAsync();

            while (!exit_token.IsCancellationRequested)
            {
                StartDatabaseCheck(null);
                Thread.Sleep(5000);
            }

            Bot.StopReceiving();
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
                Console.Read();
                Environment.Exit(1);
            }
            catch (JsonReaderException jre)
            {
                Logger.Log(LogType.Fatal, $"Error parsing dictionary file! Exception: {jre.Message}");
                Console.Read();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"<{e.Source}> {e.Message}");
                Console.Read();
                Environment.Exit(1);
            }
        }

        private static void Init()
        {
            try
            {
                Bot = new TelegramBotClient(tokens["TELEGRAM"]);
                clarifai = new ClarifaiClient(tokens["CLARIFAI"]);
                if (clarifai.HttpClient.ApiKey == string.Empty)
                    throw new ArgumentException("CLARIFAI token isn't valid!");

                bothelper = new BotHelper(Bot);
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

            perchikregex.AddRegEx(strManager["BOT_REGX"], (_, o) => commands.CheckMessage(o.Message));
            commands.AddRegEx(@"(?<ban>\b(за)?бань?\b)\s?(?<number>\d{1,9})?\s?(?<letter>[смчд](\w+)?)?\s?(?<comment>[\w\W\s]+)?", onPersikBanCommand);                                    //забань
            commands.AddRegEx(@"\bра[зс]бань?\b", onPersikUnbanCommand);                                 //разбань
            commands.AddRegEx(@"\bкик\b", onKickCommand);
            commands.AddRegEx(@"(?!\s)(?<first>[\W\w\s]+)\sили\s(?<second>[\W\w\s]+)(?>\s)?", onRandomChoice);                             //один ИЛИ два
            commands.AddRegEx(@"погода\s([\w\s-]+)", onWeather);   //погода ГОРОД
            commands.AddRegEx(@"прогноз\s([\w\s-]+)", onWeatherForecast);
            commands.AddRegEx(@"\b(дур[ао]к|пид[аоэ]?р|говно|д[еыи]бил|г[оа]ндон|лох|хуй|чмо|скотина)\b", onBotInsulting);//CENSORED
            commands.AddRegEx(@"\b(живой|красавчик|молодец|хороший|умный|умница)\b", onBotPraise);       //
            commands.AddRegEx(@"\bрулетк[уа]?\b", onRouletteCommand);                                    //рулетка
            commands.AddRegEx(@"инфо\s?(?<name>[\w\W\s]+)?", onStatisticsCommand);
            commands.AddRegEx(@".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?", onByWord);
            commands.onNoneMatched += onNoneCommandMatched;


            commands.AddRegEx("\b(420|трав(к)?а|шишки|марихуана)\b", (_, e) =>
            {
                Bot.SendStickerAsync(e.Message.Chat.Id,
                    "CAADAgAD0wMAApzW5wrXuBCHqOjyPQI",
                    replyToMessageId: e.Message.MessageId);
            });

            bothelper.onTextMessage += onTextMessage;
            //botcallbacks.onTextMessage += ;
            bothelper.onPhotoMessage += onPhotoMessage;
            bothelper.onStickerMessage += onStickerMessage;
            bothelper.onChatMembersAddedMessage += onChatMembersAddedMessage;
            bothelper.onDocumentMessage += onDocumentMessage;
            Bot.OnMessage += Bot_OnMessage;

            bothelper.NativeCommand(new StartCommand());
            bothelper.NativeCommand(new InfoCommand());
            bothelper.NativeCommand(new RateCommand());
            bothelper.NativeCommand(new MeCommand());
            bothelper.NativeCommand(new VersionCommand());
            bothelper.NativeCommand(new PickleCommand());
            bothelper.NativeCommand(new StickerCommand());
            bothelper.NativeCommand(new TopBansCommand());
            bothelper.NativeCommand(new TopCommand());
            bothelper.NativeCommand(new VotebanCommand());
            bothelper.NativeCommand(new OfftopUnbanCommand());
            bothelper.NativeCommand(new EveryoneCommand());

            bothelper.NativeCommand(new TestCommand());

            bothelper.NativeCommand("fox", (_, e) => Bot.SendTextMessageAsync(e.Message.Chat.Id, "🦊"));

            bothelper.NativeCommand("migr", (_, e) =>
            {
                var users_old = database.GetRows<PersikSharp.Tables.DbUser>();
                var messages_old = database.GetRows<PersikSharp.Tables.DbMessage>();
                var restriction_old = database.GetRows<PersikSharp.Tables.DbRestriction>();

                using (var db = PerchikDB.Context)
                {
                    var existingChat = db.Chats.Where(x => x.Id == -1001125742098).FirstOrDefault();
                    var new_chat = new Db.Tables.Chat()
                    {
                        Id = -1001125742098,
                        Title = "OFFTOP",
                        Description = "DESCRIPTION"
                    };
                    if (existingChat == null)
                    {
                        db.Add(new_chat);
                        db.SaveChanges();
                    }
                }

                foreach (var user in users_old)
                {
                    try
                    {
                        using (var db = PerchikDB.Context)
                        {
                            var new_user = new Db.Tables.User()
                            {
                                Id = user.Id,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                UserName = user.Username,
                                Restricted = false
                            };
                            var existingUser = db.Users.Where(x => x.Id == user.Id).FirstOrDefault();
                            if (existingUser == null)
                            {
                                db.Users.Add(new_user);
                                db.SaveChanges();
                                db.ChatUsers.Add(new Db.Tables.ChatUser()
                                {
                                    ChatId = -1001125742098,
                                    UserId = user.Id
                                });
                                db.SaveChanges();
                                Logger.Log(LogType.Debug, $"User {user.FirstName}:{user.Id}");
                            }
                        }

                    }
                    catch (Exception)
                    {
                        Logger.Log(LogType.Debug, $"ERROR {user.FirstName}:{user.Id}");
                    }

                }

                int i = 0;
                foreach (var message in messages_old)
                {
                    try
                    {
                        using (var db = PerchikDB.Context)
                        {

                            //db.Database.AutoDetectChangesEnabled = false;
                            if (db.Users.AsNoTracking().Where(x => x.Id == message.UserId).FirstOrDefault() != null)
                            {
                                db.Messages.Add(new Db.Tables.Message()
                                {
                                    MessageId = message.Id,
                                    UserId = message.UserId,
                                    ChatId = -1001125742098,
                                    Text = message.Text,
                                    Date = DbConverter.ToEpochTime(DateTime.Parse(message.DateTime))
                                });
                                db.SaveChanges();
                                if(i++ % 10 == 0) 
                                    Logger.Log(LogType.Debug, $"Message #{i} ID {message.Id} : {message.Text}");

                            }
                            //db.AutoDetectChangesEnabled = false;
                        }
                    }
                    catch (Exception x)
                    {
                        Logger.Log(LogType.Debug, $"ERROR {message.Id} : {message.Text}");
                    }
                }

                foreach (var restriction in restriction_old)
                {
                    try
                    {

                        using (var db = PerchikDB.Context)
                        {
                            db.Restrictions.Add(new Db.Tables.Restriction()
                            {
                                ChatId = -1001125742098,
                                UserId = restriction.UserId,
                                Date = DateTime.Parse(restriction.DateTimeFrom),
                                Until = DateTime.Parse(restriction.DateTimeTo)
                            });
                            db.SaveChanges();
                            Logger.Log(LogType.Debug, $"Restriction ID {restriction.Id} : {restriction.DateTimeFrom}");
                        }
                    }
                    catch (Exception)
                    {
                        Logger.Log(LogType.Debug, $"Restriction ERROR");
                    }
                }

            });

        }

        private static void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            DatabaseUpdate(sender, e.Message);
        }

        static void StartDatabaseCheck(object s)
        {
            HandleDbRestrictions();
        }

        private static Task FullyRestrictUserAsync(ChatId chatId, int userId, int forSeconds = 40)
        {
            var until = DateTime.Now.AddSeconds(forSeconds);
            return BotHelper.RestrictUserAsync(chatId.Identifier, userId, until);
        }

        static async void HandleDbRestrictions()
        {
            try
            {
                using (var dbv2 = PerchikDB.Context)
                {
                    var users = dbv2.Users
                        .AsNoTracking()
                        .Include(x => x.Restrictions)
                        .Where(u => u.Restricted)
                        .ToList();

                    foreach (var user in users)
                    {
                        var restriction = user.Restrictions.LastOrDefault();
                        if (DateTime.Now > restriction.Until)
                        {
                            dbv2.Users
                                .Where(u => u.Id == user.Id)
                                .FirstOrDefault()
                                .Restricted = false;

                            dbv2.SaveChanges();

                            await Bot.SendTextMessageAsync(
                                    chatId: restriction.ChatId,
                                    text: string.Format(strManager["UNBANNED"], $"[{user.FirstName}](tg://user?id={user.Id})"),
                                    parseMode: ParseMode.Markdown);
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

                await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetSingle("PREDICTION"), message.From.FirstName, names[0], names[1], names[2]),
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: message.MessageId);

                Logger.Log(LogType.Info, $"Result: {names[0]}:{names[1]}:{names[2]}. IID: {message.ReplyToMessage.Photo[0].FileId}");

                return;
            }

            if (message.ReplyToMessage?.From.Id != bothelper.Me.Id)
                perchikregex.CheckMessage(message);
        }

        private static void onPerchikReplyTrigger(object sender, MessageArgs e)
        {
            if (e.Message.Chat.Type == ChatType.Private)
                return;
            if (e.Message.ReplyToMessage == null)
                return;

            if (e.Message.ReplyToMessage.From.Id == Bot.GetMeAsync().Result.Id)
                perchikregex.CheckMessage(e.Message);
        }

        private static void onWeather(object sender, RegExArgs a)//Переделать под другой АПИ
        {
            Message message = a.Message;
            Match weather_match = a.Match;

            string search_url = Uri.EscapeUriString(
                $"http://dataservice.accuweather.com/locations/v1/cities/autocomplete?apikey={tokens["ACCUWEATHER"]}&q={weather_match.Groups[1].Value}&language=ru-ru");
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
                         text: $"*Количество запросов превышено!*",
                         parseMode: ParseMode.Markdown,
                         replyToMessageId: message.MessageId);
                    return;
                }

                location_json = JsonConvert.DeserializeObject(respone_str);

                location_code = location_json[0].Key;

                string current_url = $"http://dataservice.accuweather.com/currentconditions/v1/{location_code}?apikey={tokens["ACCUWEATHER"]}&language=ru-ru&details=true";

                request = (HttpWebRequest)WebRequest.Create(current_url);
                response = (HttpWebResponse)request.GetResponse();
                resStream = response.GetResponseStream();

                reader = new StreamReader(resStream);
                respone_str = reader.ReadToEnd();

                weather_json = JsonConvert.DeserializeObject(respone_str);

                _ = Bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text:
                          string.Format(strManager["WEATHER_MESSAGE"],
                          location_json[0].LocalizedName, location_json[0].Country.LocalizedName, weather_json[0].WeatherText, weather_json[0].Temperature.Metric.Value,
                          weather_json[0].RealFeelTemperature.Metric.Value, weather_json[0].RelativeHumidity, weather_json[0].Wind.Direction.Localized, weather_json[0].Wind.Speed.Metric.Value),
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
                _ = Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: $"*{w.Message}*",
                              parseMode: ParseMode.Markdown,
                              replyToMessageId: message.MessageId);

                if (w.Response != null)
                {
                    Stream resStream = w.Response.GetResponseStream();
                    StreamReader reader = new StreamReader(resStream);
                    if (reader.ReadToEnd().Contains("The allowed number of requests has been exceeded."))
                    {
                        _ = Bot.SendTextMessageAsync(
                               chatId: message.Chat.Id,
                               text: $"*Количество запросов превышено!*",//SMOKE WEED EVERYDAY
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
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }
        }

        

        private static async void onWeatherForecast(object sender, RegExArgs a)//Переделать под другой АПИ
        {
            Message message = a.Message;
            Match weather_match = a.Match;

            string search_url = Uri.EscapeUriString(
                $"http://dataservice.accuweather.com/locations/v1/cities/autocomplete?apikey={tokens["ACCUWEATHER"]}&q={weather_match.Groups[1].Value}&language=ru-ru");
            int location_code = 0;
            dynamic location_json;
            dynamic weather_json;
            try
            {
                string respone_str = BotHelper.HttpRequestAsync(search_url).Result;
                if (respone_str.Contains("The allowed number of requests has been exceeded."))
                {
                    await Bot.SendTextMessageAsync(
                         chatId: message.Chat.Id,
                         text: $"*Количество запросов превышено!*",
                         parseMode: ParseMode.Markdown,
                         replyToMessageId: message.MessageId);
                    return;
                }

                location_json = JsonConvert.DeserializeObject(respone_str);
                location_code = location_json[0].Key;

                string current_url = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{location_code}?apikey={tokens["ACCUWEATHER"]}&language=ru-ru&metric=true&details=true";
                respone_str = BotHelper.HttpRequestAsync(current_url).Result;

                weather_json = JsonConvert.DeserializeObject(respone_str);

                await Bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text:
                          string.Format(strManager["WEATHER_FORECAST_MESSAGE"],
                          location_json[0].LocalizedName, location_json[0].Country.LocalizedName, weather_json.DailyForecasts[0].Day.LongPhrase,
                          weather_json.DailyForecasts[0].Temperature.Minimum.Value, weather_json.DailyForecasts[0].Temperature.Maximum.Value,
                          weather_json.DailyForecasts[0].Day.RainProbability, weather_json.DailyForecasts[0].Day.Wind.Speed.Value,
                          weather_json.DailyForecasts[0].Day.Wind.Direction.Localized),
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");

                await Bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"*Нет такого .. {weather_match.Groups[1].Value.ToUpper()}!!😠*",
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (WebException w)
            {
                await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: $"*{w.Message}*",
                              parseMode: ParseMode.Markdown,
                              replyToMessageId: message.MessageId);

                if (w.Response != null)
                {
                    Stream resStream = w.Response.GetResponseStream();
                    StreamReader reader = new StreamReader(resStream);
                    if (reader.ReadToEnd().Contains("The allowed number of requests has been exceeded."))
                    {
                        await Bot.SendTextMessageAsync(
                               chatId: message.Chat.Id,
                               text: $"*Количество запросов превышено!*",//SMOKE WEED EVERYDAY
                               parseMode: ParseMode.Markdown,
                               replyToMessageId: message.MessageId);
                        return;
                    }

                    Logger.Log(LogType.Error, $"Exception: {w.Message}");
                    await Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: w.Message,
                                parseMode: ParseMode.Markdown,
                                replyToMessageId: message.MessageId);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }
        }

        private static async void onNoneCommandMatched(object sender, RegExArgs e)
        {
            Logger.Log(LogType.Info, $"<Perchik>({e.Message.From.FirstName}:{e.Message.From.Id}) -> {"NONE"}");
            await Bot.SendTextMessageAsync(
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
                    if (!BotHelper.isUserAdmin(message.Chat.Id, message.From.Id))
                        return;

                    if (message.ReplyToMessage.From.Id == Bot.BotId)
                        return;

                    await FullyRestrictUserAsync(
                            chatId: message.Chat.Id,
                            userId: message.ReplyToMessage.From.Id,
                            forSeconds: seconds);

                    if (seconds >= default_second)
                    {
                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(strManager.GetSingle("BANNED"), BotHelper.MakeUserLink(message.ReplyToMessage.From), number, word, comment, BotHelper.MakeUserLink(message.From)),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        seconds = int.MaxValue;
                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(strManager.GetSingle("SELF_PERMANENT"), BotHelper.MakeUserLink(message.ReplyToMessage.From), number, word, comment),
                            parseMode: ParseMode.Markdown);
                    }

                    using (var db = PerchikDB.Context)
                    {
                        var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.Now.AddSeconds(seconds));
                        db.AddRestriction(restriction);
                    }

                    await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
                else
                {
                    if (seconds >= default_second)
                    {
                        await FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                forSeconds: seconds);

                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), BotHelper.MakeUserLink(message.From), number, word, comment),
                            parseMode: ParseMode.Markdown);

                        using (var db = PerchikDB.Context)
                        {
                            var restriction = DbConverter.GenRestriction(message, DateTime.Now.AddSeconds(seconds));
                            db.AddRestriction(restriction);
                        }
                    }
                    else
                    {
                        await FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id);

                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), BotHelper.MakeUserLink(message.From), 40, word, comment),
                            parseMode: ParseMode.Markdown);

                        using (var db = PerchikDB.Context)
                        {
                            var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.Now.AddSeconds(40));
                            db.AddRestriction(restriction);
                        }
                    }

                    await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
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
            if (!BotHelper.isUserAdmin(message.Chat.Id, message.From.Id))
                return;
            if (message.ReplyToMessage == null)
                return;

            try
            {
                var until = DateTime.Now.AddSeconds(1);
                BotHelper.RestrictUserAsync(message.Chat.Id, message.ReplyToMessage.From.Id, until, true);

                using (var db = PerchikDB.Context)
                {


                    var existingUser = db.Users
                        .Where(x => x.Id == message.ReplyToMessage.From.Id)
                        .FirstOrDefault();

                    if (existingUser != null)
                    {
                        existingUser.Restricted = false;
                        db.SaveChanges();
                    }

                }

                _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetRandom("UNBANNED"), BotHelper.MakeUserLink(message.ReplyToMessage.From)),
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
            if (!BotHelper.isUserAdmin(message.Chat.Id, message.From.Id))
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
                        text: string.Format(strManager.GetRandom("KICK"), BotHelper.MakeUserLink(message.ReplyToMessage.From)),
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
                            forSeconds: 60);

                    await Bot.SendTextMessageAsync(
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


                result = rand.NextDouble() >= 0.5 ? first : second;

                if (first.Equals(second))
                    result = strManager.GetRandom("CHOICE_EQUAL");

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
                if (rand.Next(0, 6) == 3)
                {
                    var until = DateTime.Now.AddSeconds(10 * 60); //10 minutes
                    BotHelper.RestrictUserAsync(message.Chat.Id, message.From.Id, until);


                    _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetRandom("ROULETTEBAN"), BotHelper.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown).Result;
                }
                else
                {
                    var msg = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetRandom("ROULETTEMISS"), BotHelper.MakeUserLink(message.From)),
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

        private static async void onStatisticsCommand(object sender, RegExArgs e)
        {
            try
            {
                Message message = e.Message;
                string name = e.Match.Groups["name"]?.Value;
                if (name == null || name.Length == 0)
                {
                    if (message.ReplyToMessage == null)
                    {
                        name = message.From.Username ?? name;//Can be null

                        name = message.From.FirstName ?? name;//But FirstName can't
                    }else
                    {
                        name = message.ReplyToMessage.From.Username ?? name;//Can be null

                        name = message.ReplyToMessage.From.FirstName ?? name;//But FirstName can't
                    }

                }                                         // Last name isn't required, this will be unreachable code

                var update_button = new InlineKeyboardButton();
                update_button.CallbackData = "stats-" + Guid.NewGuid().ToString("n").Substring(0, 8);
                update_button.Text = strManager["RATE_UPDATE_BTN"];

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { update_button } });

                string text = string.Empty;
                try
                {
                    text = getStatisticsText(name);
                }catch(Exception ex)
                {
                    inlineKeyboard = null;
                    text = ex.Message;
                }

                Logger.Log(LogType.Info, $"User {message.From.FirstName}:{message.From.Id} created info message with Data: {update_button.CallbackData}");

                Message msg = await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: text,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.Markdown);

                bothelper.RegisterCallbackQuery(update_button.CallbackData, 0, name, async (_, o) => 
                {
                    string new_text = string.Empty;
                    try
                    {
                        new_text = getStatisticsText(o.obj as string);
                        
                    }
                    catch (Exception ex)
                    {
                        new_text = ex.Message;
                    }

                    try
                    {
                        await Bot.EditMessageTextAsync(
                            chatId: msg.Chat.Id,
                            messageId: o.Callback.Message.MessageId,
                            replyMarkup: inlineKeyboard,
                            text: new_text,
                            parseMode: ParseMode.Markdown);
                    }
                    catch (Exception)
                    {

                    }
                });
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static string getStatisticsText(string search)
        {
            using (var db = PerchikDB.Context)
            {
                var sw = new Stopwatch();
                sw.Start();

                string name = search.Replace("@", "");

                long today = DbConverter.ToEpochTime(DateTime.Now.Date);
                long lastday = DbConverter.ToEpochTime(DateTime.Now.AddDays(-1).Date);

                var user = db.Users
                    .AsNoTracking()
                    .Where(u =>
                        (u.FirstName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                        (u.LastName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                        (u.UserName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    .Select(x => new
                    {
                        x.Id,
                        x.Restricted,
                        x.FirstName,
                        x.LastName,
                        x.UserName,
                        x.Restrictions.OrderByDescending(x => x.Until).FirstOrDefault().Until,
                        msgLastday = x.Messages.Where(m => m.Date > lastday && m.Date < today).Count(),
                        msgToday = x.Messages.Where(m => m.Date > today).Count(),
                        msgTotal = x.Messages.Count,
                        RestrictionCount = x.Restrictions.Count,
                        activity = x.Messages.Where(m => m.Date > today).Sum(m => m.Text.Length) /
                                   (double)db.Messages.Where(m => m.Date > today).Sum(m => m.Text.Length)
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    throw new Exception($"*Пользователя \"{search}\" нет в базе.*");
                }

                TimeSpan remaining = new TimeSpan(0);
                if (user.Restricted)
                {
                    remaining = user.Until - DateTime.Now;
                }

                sw.Stop();

                return $"*Имя: {user.FirstName} {user.LastName}\n" +
                            $"ID: {user.Id}\n" +
                            $"Ник: {user.UserName}\n\n" +
                            string.Format("Активность: {0:F2}%\n", user.activity * 100) +
                            $"Сообщений сегодня: { user.msgToday }\n" +
                            $"Сообщений вчера: { user.msgLastday }\n" +
                            $"Всего сообщений: { user.msgTotal }\n" +
                            $"Банов: { user.RestrictionCount }\n\n*" +
                            (remaining.Ticks != 0 ? $"💢`Сейчас забанен, осталось: { $"{remaining:hh\\:mm\\:ss}`\n" }" : "") + 
                            $"`{sw.ElapsedMilliseconds/1000.0}сек`";
            }
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
                    await BotHelper.SaveFileAsync(file.FileId, "./Data/nsfw");

                    if (ENABLE_FILTER)
                    {
                        _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                        if (message.Chat.Type != ChatType.Private)
                        {
                            var until = DateTime.Now.AddSeconds(120);
                            _ = BotHelper.RestrictUserAsync(message.Chat.Id, message.From.Id, until);

                            await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: String.Format(strManager.GetSingle("NSFW_TRIGGER"), message.From.FirstName, 2, 1 - nsfw_val),
                              parseMode: ParseMode.Markdown);
                        }
                    }
                }
                else
                {
                    await BotHelper.SaveFileAsync(file.FileId, "./Data/photos");
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
                await BotHelper.SaveFileAsync(message.Document.FileId, "./Data/documents", message.Document.FileName);
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
                if (BotHelper.isUserAdmin(offtopia_id, m.From.Id))
                {
                    string msg = m.Text.Substring(1, m.Text.Length - 1);
                    _ = Bot.SendTextMessageAsync(offtopia_id, $"*{msg}*", ParseMode.Markdown);

                    Logger.Log(LogType.Info, $"({m.From.FirstName}:{m.From.Id})(DM): {msg}");
                }
            }


            onPerchikReplyTrigger(sender, message_args);
        }

        private static void DatabaseUpdate(object s, Message msg)
        {
            try
            {
                using (var db = PerchikDB.Context)
                {

                    db.UpsertChat(DbConverter.GenChat(msg.Chat));

                    db.UpsertUser(DbConverter.GenUser(msg.From), msg.Chat.Id);

                    db.AddMessage(DbConverter.GenMessage(msg));
                }
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

        private static async void onChatMembersAddedMessage(object sender, MessageArgs message_args)
        {
            try
            {
                Chat telegram_chat = await Bot.GetChatAsync(message_args.Message.Chat.Id);
                using (var db = PerchikDB.Context)
                {
                    db.UpsertChat(DbConverter.GenChat(telegram_chat));
                }

                if (message_args.Message.From.IsBot)
                    return;

                Message message = message_args.Message;

                string username = string.Empty;
                if (message.From.Username != null)
                {
                    username = $"@{message.From.Username}";
                }
                else { username = BotHelper.MakeUserLink(message.From); }

                string msg_string = String.Format(strManager["NEW_MEMBERS"], username);
                _ = Bot.SendTextMessageAsync(message.Chat.Id, msg_string, ParseMode.Markdown);


                if(message.From.Id == via_tcp_Id)
                {
                    Thread.Sleep(2000);
                    _ = Bot.PromoteChatMemberAsync(message.Chat.Id, via_tcp_Id, true, false, false, true, true, true, true, true);
                }


            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}