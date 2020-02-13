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

namespace PerchikSharp
{
    class Program
    {
        public static TelegramBotClient Bot;
        static Perchik perchik;
        static ClarifaiClient clarifai;
        static BotHelper bothelper;
        static StringManager strManager = new StringManager();
        static StringManager tokens = new StringManager();

        static PerschikDB database;

        static CancellationTokenSource exitTokenSource = new CancellationTokenSource();
        static CancellationToken exit_token = exitTokenSource.Token;

        const long offtopia_id = -1001125742098;
        const int via_tcp_Id = 204678400;
        static string ApplicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);


        static List<long> votebanning_groups = new List<long>();

        static void Main(string[] args)
        {

            Logger.Log(LogType.Info, $"Bot version: {Perchik.BotVersion}");

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            FileInfo file = new FileInfo("./Data/");
            file.Directory.Create();

            database = new PerschikDB("./Data/database_old.db");
            database.Create();

            PerchikDB.ConnectionString = tokens["MYSQL"];

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

        private static void Init()
        {
            try
            {
                perchik = new Perchik();
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

            perchik.AddCommandRegEx(@"(?<ban>\b(за)?бань?\b)\s?(?<number>\d{1,9})?\s?(?<letter>[смчд](\w+)?)?\s?(?<comment>[\w\W\s]+)?", onPersikBanCommand);                                    //забань
            perchik.AddCommandRegEx(@"\bра[зс]бань?\b", onPersikUnbanCommand);                                 //разбань
            perchik.AddCommandRegEx(@"\bкик\b", onKickCommand);
            perchik.AddCommandRegEx(@"(?!\s)(?<first>[\W\w\s]+)\sили\s(?<second>[\W\w\s]+)(?>\s)?", onRandomChoice);                             //один ИЛИ два
            perchik.AddCommandRegEx(@"погода\s([\w\s-]+)", onWeather);   //погода ГОРОД
            perchik.AddCommandRegEx(@"прогноз\s([\w\s-]+)", onWeatherForecast);
            perchik.AddCommandRegEx(@"\b(дур[ао]к|пид[аоэ]?р|говно|д[еыи]бил|г[оа]ндон|лох|хуй|чмо|скотина)\b", onBotInsulting);//CENSORED
            perchik.AddCommandRegEx(@"\b(живой|красавчик|молодец|хороший|умный|умница)\b", onBotPraise);       //
            perchik.AddCommandRegEx(@"\bрулетк[уа]?\b", onRouletteCommand);                                    //рулетка
            perchik.AddCommandRegEx(@"инфо\s?(?<name>[\w\W\s]+)?", onStatisticsCommand);
            perchik.onNoneMatched += onNoneCommandMatched;


            bothelper.AddRegEx(strManager["BOT_REGX"], onPersikCommand);
            bothelper.AddRegEx(@".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?", onByWord);
            bothelper.AddRegEx(@"#оффтоп", onEveryoneCommand);

            bothelper.AddRegEx("\b(420|трав(к)?а|шишки|марихуана)\b", (_, e) =>
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

            bothelper.NativeCommand("start", onStartCommand);
            bothelper.NativeCommand("info", onInfoCommand);
            bothelper.NativeCommand("rate", onRateCommand);
            bothelper.NativeCommand("me", onMeCommand);
            bothelper.NativeCommand("upal_otjalsa", onUpalOtjalsaCommand);
            bothelper.NativeCommand("version", onVersionCommand);
            bothelper.NativeCommand("pickle", onPickleCommand);
            bothelper.NativeCommand("stk", onStickerCommand);
            bothelper.NativeCommand("topbans", onTopBansCommand);
            bothelper.NativeCommand("top", onTopCommand);
            bothelper.NativeCommand("voteban", onVoteban);
            bothelper.NativeCommand("offtopunban", onOfftopUnban);
            bothelper.CallbackQuery("update_rate", onRateUpdate);

            bothelper.NativeCommand("promote", onPromoteCommand);

            bothelper.NativeCommand("fox", (_, e) => Bot.SendTextMessageAsync(e.Message.Chat.Id, "🦊"));

            bothelper.NativeCommand("migr", (_, e) =>
            {
                var users_old = database.GetRows<PersikSharp.Tables.DbUser>();
                var messages_old = database.GetRows<PersikSharp.Tables.DbMessage>();
                var restriction_old = database.GetRows<PersikSharp.Tables.DbRestriction>();

                //using (var db = PerchikDBv2.Context)
                //{
                //    var existingChat = db.Chats.Where(x => x.Id == -1001125742098).FirstOrDefault();
                //    var new_chat = new Db.Tables.Chatv2()
                //    {
                //        Id = -1001125742098,
                //        Title = "OFFTOP",
                //        Description = "DESCRIPTION"
                //    };
                //    if (existingChat == null)
                //    {
                //        db.Add(new_chat);
                //        db.SaveChanges();
                //    }
                //}

                //foreach (var user in users_old)
                //{
                //    try
                //    {
                //        using (var db = PerchikDBv2.Context)
                //        {
                //            var new_user = new Db.Tables.Userv2()
                //            {
                //                Id = user.Id,
                //                FirstName = user.FirstName,
                //                LastName = user.LastName,
                //                UserName = user.Username,
                //                Restricted = false
                //            };
                //            var existingUser = db.Users.Where(x => x.Id == user.Id).FirstOrDefault();
                //            if (existingUser == null)
                //            {
                //                db.Users.Add(new_user);
                //                db.SaveChanges();
                //                db.ChatUsers.Add(new Db.Tables.ChatUserv2()
                //                {
                //                    ChatId = -1001125742098,
                //                    UserId = user.Id
                //                });
                //                db.SaveChanges();
                //                Logger.Log(LogType.Debug, $"User {user.FirstName}:{user.Id}");
                //            }
                //        }

                //    }
                //    catch (Exception x)
                //    {
                //        Logger.Log(LogType.Debug, $"ERROR {user.FirstName}:{user.Id}");
                //    }

                //}
                
                //foreach (var message in messages_old)
                //{
                //    try
                //    {
                //        using (var db = PerchikDBv2.Context)
                //        {
                            
                //            //db.Database.AutoDetectChangesEnabled = false;
                //            if (db.Users.AsNoTracking().Where(x => x.Id == message.UserId).FirstOrDefault() != null)
                //            {
                //                db.Messages.Add(new Db.Tables.Messagev2()
                //                {
                //                    MessageId = message.Id,
                //                    UserId = message.UserId,
                //                    ChatId = -1001125742098,
                //                    Text = message.Text,
                //                    Date = PerchikDBv2.ToEpochTime(DateTime.Parse(message.DateTime))
                //                });
                //                db.SaveChanges();
                //                //Logger.Log(LogType.Debug, $"Message ID {message.Id} : {message.Text}");
                //            }
                //            //db.AutoDetectChangesEnabled = false;
                //        }
                //    }
                //    catch (Exception x)
                //    {
                //        Logger.Log(LogType.Debug, $"ERROR {message.Id} : {message.Text}");
                //    }
                //}

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
            return Perchik.RestrictUserAsync(chatId.Identifier, userId, until);
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

                _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetSingle("PREDICTION"), message.From.FirstName, names[0], names[1], names[2]),
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: message.MessageId);

                Logger.Log(LogType.Info, $"Result: {names[0]}:{names[1]}:{names[2]}. IID: {message.ReplyToMessage.Photo[0].FileId}");

                return;
            }

            if (message.ReplyToMessage?.From.Id != bothelper.Me.Id)
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

        

        private static void onWeatherForecast(object sender, RegExArgs a)//Переделать под другой АПИ
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
                    _ = Bot.SendTextMessageAsync(
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

                _ = Bot.SendTextMessageAsync(
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

                    if (seconds >= default_second)
                    {
                        await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(strManager.GetSingle("BANNED"), Perchik.MakeUserLink(message.ReplyToMessage.From), number, word, comment, Perchik.MakeUserLink(message.From)),
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

                    using (var db = PerchikDB.Context)
                    {
                        var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.Now.AddSeconds(seconds));
                        db.AddRestriction(restriction);
                    }

                    _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
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
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), Perchik.MakeUserLink(message.From), number, word, comment),
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
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), Perchik.MakeUserLink(message.From), 40, word, comment),
                            parseMode: ParseMode.Markdown);

                        using (var db = PerchikDB.Context)
                        {
                            var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.Now.AddSeconds(40));
                            db.AddRestriction(restriction);
                        }
                    }

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
                Perchik.RestrictUserAsync(message.Chat.Id, message.ReplyToMessage.From.Id, until, true);

                using (var db = PerchikDB.Context)
                {


                    var existingUser = db.Users.Where(
                        x => x.Id == message.ReplyToMessage.From.Id
                        ).FirstOrDefault();

                    if (existingUser != null)
                    {
                        existingUser.Restricted = false;
                        db.SaveChanges();
                    }

                }

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
                            forSeconds: 60);

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

        private static void onEveryoneCommand(object sender, RegExArgs e)
        {
            try
            {
                Message message = e.Message;

                if (message.Chat.Type == ChatType.Private)
                    return;

                int[] users_whitelist = { 204678400
                                         /*тут огурец*/ };
                if (!Perchik.isUserAdmin(message.Chat.Id, message.From.Id) &&
                    !users_whitelist.Any(id => id == message.From.Id))
                        return;


                using (var db = PerchikDB.Context)
                {
                    var users = db.Users.ToList();
                    string message_str = string.Empty;

                    int max_users_in_message = 10;
                    List<Message> sended_messages = new List<Message>();

                    int i = 0;
                    foreach (var user in users)
                    {
                        string firstname = user.FirstName.Replace('[', '<').Replace(']', '>');
                        message_str += $"[{firstname}](tg://user?id={user.Id})\n";
                        if (i % max_users_in_message == 0 || i == users.Count() - 1)
                        {
                            var msg = Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: message_str,
                                parseMode: ParseMode.Markdown).Result;
                            sended_messages.Add(msg);
                            message_str = string.Empty;
                        }

                        i++;
                    }

                    Thread.Sleep(5000);
                    foreach (var m in sended_messages)
                    {
                        Bot.DeleteMessageAsync(
                               chatId: message.Chat.Id,
                               messageId: m.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
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
                    Perchik.RestrictUserAsync(message.Chat.Id, message.From.Id, until);


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

        private static async void onStatisticsCommand(object sender, RegExArgs e)
        {
            try
            {
                Message message = e.Message;
                string name = e.Match.Groups["name"]?.Value;
                if (name == null || name.Length == 0)
                {
                    name = message.From.Username ?? name;//Can be null

                    name = message.From.FirstName ?? name;//But FirstName can't

                }                                         // Last name isn't required, this will be unreachable code

                var update_button = new InlineKeyboardButton();
                update_button.CallbackData = "stats-update";
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

                Message msg = await Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: text,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.Markdown);

                bool generating = false;
                bothelper.RegisterCallbackQuery(update_button.CallbackData, e.Message.From.Id, async (_, o) => 
                {
                    if (generating)
                        return;

                    generating = true;

                    string new_text = string.Empty;
                    try
                    {
                        new_text = getStatisticsText(name);
                        
                    }
                    catch (Exception ex)
                    {
                        new_text = ex.Message;
                    }

                    if (!new_text.Equals(text))
                    {
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
                    }
                    generating = false;
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

        private static void onTopCommand(object sender, CommandEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            using (var db = PerchikDB.Context)
            {
                Message message = e.Message;

                long datenow = DbConverter.ToEpochTime(DateTime.Now.Date);
                long now = DbConverter.ToEpochTime(DateTime.Now);

                var users = db.Users
                    .AsNoTracking()
                    .Select(x => new
                    {
                        x.Id,
                        x.FirstName,
                        x.LastName,
                        activity = x.Messages.Where(m => m.Date > datenow).Sum(m => m.Text.Length) /
                                   (double)db.Messages.Where(m => m.Date > datenow).Sum(m => m.Text.Length)
                    })
                    .ToList();

                var usersDescending = users.OrderByDescending(x => x.activity);
                string msg_string = "*Топ 10 по активности за сегодня:*\n";
                for (int i = 0; i < 10 && i < users.Count; i++)
                {               
                    var user = usersDescending.ElementAt(i);

                    if (user.activity == 0)
                        continue;

                    //var user = users.ElementAt(i);
                    string first_name = user.FirstName?.Replace('[', '<').Replace(']', '>');
                    string last_name = user.LastName?.Replace('[', '<').Replace(']', '>');
                    string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);

                    msg_string += string.Format("{0}. {1} -- {2:F2}%\n", i + 1, full_name, user.activity * 100);
                }

               stopwatch.Stop();

                _ = Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: $"{msg_string}\n`{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                                parseMode: ParseMode.Markdown).Result;
            }
        }

        private static void onTopBansCommand(object sender, CommandEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var db = PerchikDB.Context)
            {
                Message message = e.Message;

                var users = db.Users
                    //.Include(x => x.Restrictions)
                    .OrderByDescending(x => x.Restrictions.Count)
                    .Take(10)
                    .Select(x => new
                    {
                        x.Id,
                        x.FirstName,
                        x.LastName,
                        x.Restrictions.Count 
                    })
                    .ToList();

                string msg_string = "*Топ 10 по банам:*\n";
                int i = 1;
                foreach (var user in users)
                {
                    string first_name = user.FirstName?.Replace('[', '<').Replace(']', '>');
                    string last_name = user.LastName?.Replace('[', '<').Replace(']', '>');
                    string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);
                    msg_string += $"{i++}. {full_name} -- {user.Count}\n";
                }

                stopwatch.Stop();

                _ = Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: $"{msg_string}\n`{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                                parseMode: ParseMode.Markdown).Result;
            }
        }
        private static void onPromoteCommand(object sender, CommandEventArgs e)
        {
            if (e.Message.Chat.Type == ChatType.Private)
                return;

            try
            {
                if (e.Message.From.Id == via_tcp_Id)
                {
                    Bot.PromoteChatMemberAsync(e.Message.Chat.Id, via_tcp_Id, true, false, false, true, true, true, true, true);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static async void onVoteban(object sender, CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
            {
                await Bot.SendTextMessageAsync(
                             chatId: e.Message.Chat.Id,
                             text: StringManager.FromFile("votebanusage.txt"),
                             parseMode: ParseMode.Markdown);
                return;
            }

            try
            {
                if (votebanning_groups.Contains(e.Message.Chat.Id))
                {
                    await Bot.SendTextMessageAsync(
                              chatId: e.Message.Chat.Id,
                              text: strManager["VOTEBAN_ALREADY"],
                              parseMode: ParseMode.Markdown);
                    return;
                }

                const int time_secs = 60 * 3; //3 minutes
                const int min_vote_count = 6;
                const double vote_ratio = 0.7;
                const int alert_period = 30;

                string username = e.Text.Replace("@", "").ToLower();

                using (var db = PerchikDB.Context)
                {

                    var user = db.Users.Where(u =>
                        (u.FirstName.ToLower().Contains(username)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(username)) ||
                        (u.UserName != null && u.UserName.ToLower().Contains(username))
                    ).FirstOrDefault();

                    if (user == null)
                    {
                        await Bot.SendTextMessageAsync(
                                  chatId: e.Message.Chat.Id,
                                  text: $"*Пользователя \"{e.Text}\" нет в базе.*",
                                  parseMode: ParseMode.Markdown);
                        return;
                    }

                    username = user.FirstName.Replace('[', '<').Replace(']', '>');
                    string userlink = $"[{username}](tg://user?id={user.Id})";

                    Message message = e.Message;
                    string[] opts = { "За", "Против" };
                    var poll_msg = await Bot.SendPollAsync(
                        chatId: message.Chat.Id,
                        question: string.Format(strManager["VOTEBAN_QUESTION"], username),
                        options: opts,
                        disableNotification: false,
                        isAnonymous: false);

                    var chat = await Bot.GetChatAsync(message.Chat.Id);
                    Logger.Log(LogType.Info, $"<{chat.Title}>: Voteban poll started for {username}:{user.Id}");

                    int legitvotes = 0, ignored = 0;
                    int forban = 0, againstban = 0;

                    Poll recent_poll = poll_msg.Poll;
                    List<PollAnswer> answers = new List<PollAnswer>();
                    votebanning_groups.Add(e.Message.Chat.Id);


                    bothelper.RegisterPoll(poll_msg.Poll.Id, (_, p) =>
                    {
                        if (p.pollAnswer == null)
                            return;

                        recent_poll = p.poll;
                        var pollanswer = p.pollAnswer;
                        var existingUser = db.Users.Where(x => x.Id == pollanswer.User.Id).FirstOrDefault();
                        if (existingUser != null)
                        {
                            if (pollanswer.OptionIds.Length > 0)
                            {
                                answers.Add(pollanswer);
                                Logger.Log(LogType.Info,
                                            $"<{chat.Title}>: Voteban {pollanswer?.User.FirstName}:{pollanswer?.User.Id} voted {pollanswer.OptionIds[0]}");
                            }
                            else
                            {
                                answers.RemoveAll(a => a.User.Id == pollanswer.User.Id);
                                Logger.Log(LogType.Info,
                                            $"<{chat.Title}>: Voteban {pollanswer?.User.FirstName}:{pollanswer?.User.Id} retracted vote");
                            }
                        }
                        else
                        {
                            Logger.Log(LogType.Info,
                                $"<{chat.Title}>: Voteban ignored user from another chat {pollanswer?.User.FirstName}:{pollanswer?.User.Id}");
                        }
                    });


                    List<Message> msg2delete = new List<Message>();

                    int alerts_count = time_secs / alert_period;
                    for (int alerts = 1; alerts < alerts_count; alerts++)
                    {
                        await Task.Delay(1000 * alert_period);

                        forban = answers.Sum(a => a.OptionIds[0] == 0 ? 1 : 0);
                        againstban = answers.Sum(a => a.OptionIds[0] == 1 ? 1 : 0);
                        legitvotes = answers.Count;
                        ignored = recent_poll.TotalVoterCount - legitvotes;

                        msg2delete.Add(await Bot.SendTextMessageAsync(
                                  chatId: message.Chat.Id,
                                  text: string.Format(strManager["VOTEBAN_ALERT"],
                                    user.FirstName, time_secs - alerts * alert_period, legitvotes, min_vote_count,
                                    forban, againstban),
                                  replyToMessageId: poll_msg.MessageId,
                                  parseMode: ParseMode.Markdown));

                        Logger.Log(LogType.Info,
                            $"<{chat.Title}>: Voteban poll status {forban}<>{againstban}, totalvotes: {recent_poll.TotalVoterCount}, ignored: {ignored}");
                    }

                    await Task.Delay(1000 * alert_period);

                    await Bot.StopPollAsync(message.Chat.Id, poll_msg.MessageId);
                    bothelper.RemovePoll(poll_msg.Poll.Id);
                    votebanning_groups.Remove(e.Message.Chat.Id);
                    msg2delete.ForEach(m => Bot.DeleteMessageAsync(m.Chat.Id, m.MessageId));

                    forban = answers.Sum(a => a.OptionIds[0] == 0 ? 1 : 0);
                    againstban = answers.Sum(a => a.OptionIds[0] == 1 ? 1 : 0);
                    legitvotes = answers.Count;
                    ignored = recent_poll.TotalVoterCount - legitvotes;

                    string igore_text = ignored > 0 ? string.Format(strManager["VOTEBAN_IGNORED"], ignored) : "";

                    if (legitvotes < min_vote_count)
                    {
                        await Bot.SendTextMessageAsync(
                                  chatId: message.Chat.Id,
                                  text: string.Format($"{strManager["VOTEBAN_NOTENOUGH"]}\n\n{igore_text}", legitvotes, min_vote_count,
                                    forban, againstban),
                                  replyToMessageId: poll_msg.MessageId,
                                  parseMode: ParseMode.Markdown);
                        Logger.Log(LogType.Info, $"<{chat.Title}>: {forban}<>{againstban} Poll result: Not enough votes");
                        return;
                    }

                    double ratio = (double)forban / (double)legitvotes;
                    if (ratio < vote_ratio)
                    {
                        await Bot.SendTextMessageAsync(
                                  chatId: message.Chat.Id,
                                  text: string.Format($"{strManager["VOTEBAN_RATIO"]}\n\n {igore_text}", ratio * 100),
                                  replyToMessageId: poll_msg.MessageId,
                                  parseMode: ParseMode.Markdown);
                        Logger.Log(LogType.Info, $"<{chat.Title}>: {forban}<>{againstban} Poll result: Decided not to ban");
                        return;
                    }

                    await FullyRestrictUserAsync(
                        chatId: message.Chat.Id,
                        userId: user.Id,
                        forSeconds: 60 * 15);

                    var restriction = DbConverter.GenRestriction(e.Message, DateTime.Now.AddSeconds(60 * 15));
                    db.Restrictions.Add(restriction);
                    db.SaveChanges();

                    await Bot.SendTextMessageAsync(
                                   chatId: message.Chat.Id,
                                   text: string.Format($"{strManager["VOTEBAN_BANNED"]}\n\n {igore_text}", userlink,
                                    forban, againstban),
                                   replyToMessageId: poll_msg.MessageId,
                                   parseMode: ParseMode.Markdown);

                    Logger.Log(LogType.Info,
                        $"<{chat.Title}>: Poll result: {forban}<>{againstban} The user has been banned!");

                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static void onOfftopUnban(object sender, CommandEventArgs e)
        {
            if (e.Message.Chat.Type != ChatType.Private)
                return;

            try
            {
                ChatPermissions permissions = new ChatPermissions();
                permissions.CanAddWebPagePreviews = true;
                permissions.CanChangeInfo = true;
                permissions.CanInviteUsers = true;
                permissions.CanPinMessages = true;
                permissions.CanSendMediaMessages = true;
                permissions.CanSendMessages = true;
                permissions.CanSendOtherMessages = true;
                permissions.CanSendPolls = true;

                Bot.RestrictChatMemberAsync(
                    chatId: offtopia_id,
                    userId: via_tcp_Id,
                    untilDate: DateTime.Now.AddSeconds(40),
                    permissions: permissions);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
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
                    await Perchik.SaveFileAsync(file.FileId, "./Data/nsfw");

                    if (ENABLE_FILTER)
                    {
                        _ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                        if (message.Chat.Type != ChatType.Private)
                        {
                            var until = DateTime.Now.AddSeconds(120);
                            _ = Perchik.RestrictUserAsync(message.Chat.Id, message.From.Id, until);

                            await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: String.Format(strManager.GetSingle("NSFW_TRIGGER"), message.From.FirstName, 2, 1 - nsfw_val),
                              parseMode: ParseMode.Markdown);
                        }
                    }
                }
                else
                {
                    await Perchik.SaveFileAsync(file.FileId, "./Data/photos");
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
                await Perchik.SaveFileAsync(message.Document.FileId, "./Data/documents", message.Document.FileName);
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
                if (Perchik.isUserAdmin(offtopia_id, m.From.Id))
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
                else { username = Perchik.MakeUserLink(message.From); }

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

        private static  void onRateUpdate(object sender, CallbackQueryArgs e)
        {
            try
            {
                string url = "https://min-api.cryptocompare.com/data/pricemultifull";

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var uriBuilder = new UriBuilder(url);
                    var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                    query["fsyms"] = "BTC,ETH,ETC,ZEC,LTC,BCH";
                    query["tsyms"] = "USD";

                    uriBuilder.Query = query.ToString();
                    url = uriBuilder.ToString();


                    HttpResponseMessage responseMessage = client.GetAsync(url).Result;
                    var responseJson = responseMessage.Content.ReadAsStringAsync().Result;

                    var json_object = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>();
                    json_object = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>>(responseJson);

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
                       text: StringManager.FromFile(strManager["INFO_PATH"]),
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

           // if (!Perchik.isUserAdmin(offtopia_id, e.Message.From.Id))
             //   return;

            try
            {
                _ = Bot.SendTextMessageAsync(
                         chatId: e.Message.Chat.Id,
                         text: strManager["STK"],
                         parseMode: ParseMode.Markdown,
                         replyMarkup: new ForceReplyMarkup()).Result;
                bothelper.RegisterNextstep(onStickerAnswer, e.Message);
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
                            text: strManager["STK_OK"],
                            parseMode: ParseMode.Markdown).Result;
                    _ = Bot.SendStickerAsync(
                        chatId: offtopia_id,
                        sticker: e.Message.Sticker.FileId);

                    bothelper.RemoveNextstepCallback(e.Message);
                }
                else
                {
                    if (Perchik.FindTextCommand(e.Message.Text, "stop"))
                    {
                        _ = Bot.SendTextMessageAsync(
                           chatId: e.Message.Chat.Id,
                           text: strManager["STK_CANCEL"],
                           parseMode: ParseMode.Markdown).Result;

                        return;
                    }

                    _ = Bot.SendTextMessageAsync(
                            chatId: e.Message.Chat.Id,
                            text: strManager["STK_WRONG"],
                            parseMode: ParseMode.Markdown,
                            replyMarkup: new ForceReplyMarkup()).Result;
                    bothelper.RegisterNextstep(onStickerAnswer, e.Message);
                }

            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
        //==========================
    }
}