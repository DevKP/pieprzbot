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
using PersikSharp.Tables;
using System.Net.Http.Headers;
using System.Web;
using PerchikSharp;

namespace PersikSharp
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
            //CloseAnotherInstance();

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            FileInfo file = new FileInfo("./Data/");
            file.Directory.Create();
            database = new PerschikDB("./Data/database.db");
            database.Create();

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



                    //CAUTION
                    //Диверсия, убрать, очень опасно#########################################
                    //await FullyRestrictUserAsync(
                    //               chatId: message.Chat.Id,
                    //               userId: message.ReplyToMessage.From.Id,
                    //               forSeconds: int.MaxValue);

                    //await Bot.SendTextMessageAsync(
                    //    chatId: message.Chat.Id,
                    //    text: string.Format(strManager.GetSingle("SELF_PERMANENT"), Perchik.MakeUserLink(message.ReplyToMessage.From), number, word, comment),
                    //    parseMode: ParseMode.Markdown);

                    //_ = database.AddRestrictionAsync(new DbUser()
                    //{
                    //    Id = e.Message.ReplyToMessage.From.Id,
                    //    FirstName = e.Message.ReplyToMessage.From.FirstName,
                    //    LastName = e.Message.ReplyToMessage.From.LastName,
                    //    Username = e.Message.ReplyToMessage.From.Username,
                    //    LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    //}, e.Message.Chat.Id, int.MaxValue);

                    //_ = Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                    //return;

                    //#######################################################################



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

                        _ = database.AddRestrictionAsync(new DbUser()
                        {
                            Id = e.Message.From.Id,
                            FirstName = e.Message.From.FirstName,
                            LastName = e.Message.From.LastName,
                            Username = e.Message.From.Username,
                            LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        }, e.Message.Chat.Id, seconds);
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

                        _ = database.AddRestrictionAsync(new DbUser()
                        {
                            Id = e.Message.From.Id,
                            FirstName = e.Message.From.FirstName,
                            LastName = e.Message.From.LastName,
                            Username = e.Message.From.Username,
                            LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        }, e.Message.Chat.Id, 40);
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
                if (!Perchik.isUserAdmin(message.Chat.Id, message.From.Id))
                    if (!users_whitelist.Any( id => id == message.From.Id))
                        return;


                var users = database.GetRows<DbUser>();
                string message_str = string.Empty;

                int max_users_in_message = 10;
                List<Message> sended_messages = new List<Message>();

                for (int i = 0; i < users.Count; i++)
                {
                    string firstname = users[i].FirstName.Replace('[', '<').Replace(']', '>');
                    message_str += $"[{firstname}](tg://user?id={users[i].Id})\n";
                    if (i % max_users_in_message == 0 || i == users.Count - 1)
                    {
                        var msg = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: message_str,
                            parseMode: ParseMode.Markdown).Result;
                        sended_messages.Add(msg);
                        message_str = string.Empty;
                    }
                }

                Thread.Sleep(5000);
                foreach (var m in sended_messages)
                {
                    Bot.DeleteMessageAsync(
                           chatId: message.Chat.Id,
                           messageId: m.MessageId);
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


                bothelper.RegisterCallbackQuery(update_button.CallbackData, e.Message.From.Id, async (_, o) => 
                {
                    string new_text = string.Empty;
                    try
                    {
                        new_text = getStatisticsText(name);
                    }
                    catch (Exception ex)
                    {
                        new_text = ex.Message;
                    }

                    if (new_text != text)
                    {
                        await Bot.EditMessageTextAsync(
                                chatId: msg.Chat.Id,
                                messageId: msg.MessageId,
                                replyMarkup: inlineKeyboard,
                                text: new_text,
                                parseMode: ParseMode.Markdown);
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
            string upper_name = search.ToUpper().Replace("@", "");

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
                throw new Exception($"*Пользователя \"{search}\" нет в базе.*");
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

            TimeSpan remaining = new TimeSpan(0);
            if (user.RestrictionId != null)
            {
                List<DbRestriction> restriction = database.GetRowsByFilterAsync<DbRestriction>(r => r.Id == user.RestrictionId).Result;
                DateTime unban_time = DateTime.Parse(restriction?.First().DateTimeTo);

                remaining = unban_time - DateTime.Now;
            }

            return $"*Имя: {user.FirstName} {user.LastName}\n" +
                        $"ID: {user.Id}\n" +
                        $"Ник: {user.Username}\n\n" +
                        string.Format("Активность: {0:F2}%\n", user_activity * 100) +
                        $"Сообщений сегодня: { u_messages_today_count }\n" +
                        $"Сообщений вчера: { u_messages_lastday_count }\n" +
                        $"Всего сообщений: { u_messages_count }\n" +
                        $"Банов: { restrictions_count }\n\n*" +
                        (remaining.Ticks != 0 ? $"💢`Сейчас забанен, осталось: { $"{remaining:hh\\:mm\\:ss}`" }\n" : "");
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
                    return m.Text?.Length ?? 0;
                });
                total_symbols += total_text_length;

                int user_text_length = u_messages_today.Sum(m =>
                {
                    return m.Text?.Length ?? 0;
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
                string first_name = user.FirstName?.Replace('[', '<').Replace(']', '>');
                string last_name = user.LastName?.Replace('[', '<').Replace(']', '>');
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
                string first_name = user.FirstName?.Replace('[', '<').Replace(']', '>');
                string last_name = user.LastName?.Replace('[', '<').Replace(']', '>');
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

                const int time_secs = 60 * 2; //2 minutes
                const int min_vote_count = 6;
                const double vote_ratio = 0.7;
                const int alert_period = 30;

                DbUser user = database.FindUser(e.Text.Replace("@","")).First();
                string username = $"@{user.Username}" ?? user.FirstName;
                username = username.Replace('[', '<').Replace(']', '>');
                string userlink = $"[{username}](tg://user?id={user.Id})";

                Message message = e.Message;
                string[] opts = { "За", "Против" };
                var poll_msg = await Bot.SendPollAsync(
                    chatId: message.Chat.Id,
                    question: string.Format(strManager["VOTEBAN_QUESTION"], username),
                    options: opts,
                    disableNotification: true);

                var chat = await Bot.GetChatAsync(message.Chat.Id);
                Logger.Log(LogType.Info, $"<{chat.Title}>: Voteban poll started for {username}:{user.Id}");

                Poll recent_poll = poll_msg.Poll;
                PollOption[] options = recent_poll.Options;
                bothelper.RegisterPoll(poll_msg.Poll.Id, (_, p) => recent_poll = p.poll);
                votebanning_groups.Add(e.Message.Chat.Id);

                List<Message> msg2delete = new List<Message>();

                int alerts_count = time_secs / alert_period;
                for (int alerts = 1; alerts < alerts_count; alerts++)
                {
                    await Task.Delay(1000 * alert_period);
                    msg2delete.Add(await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: string.Format(strManager["VOTEBAN_ALERT"],
                                userlink, time_secs - alerts * alert_period, recent_poll.TotalVoterCount, min_vote_count,
                                recent_poll.Options[0].VoterCount, recent_poll.Options[1].VoterCount),
                              replyToMessageId: poll_msg.MessageId,
                              parseMode: ParseMode.Markdown));

                    options = recent_poll.Options;
                    Logger.Log(LogType.Info, 
                        $"<{chat.Title}>: Poll status {options[0].VoterCount}<>{options[1].VoterCount}, votes: {recent_poll.TotalVoterCount}");
                }

                await Task.Delay(1000 * alert_period);

                await Bot.StopPollAsync(message.Chat.Id, poll_msg.MessageId);
                bothelper.RemovePoll(poll_msg.Poll.Id);
                votebanning_groups.Remove(e.Message.Chat.Id);
                msg2delete.ForEach(m => Bot.DeleteMessageAsync(m.Chat.Id, m.MessageId));

                if (recent_poll.TotalVoterCount < min_vote_count)
                {
                    await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: string.Format(strManager["VOTEBAN_NOTENOUGH"], recent_poll.TotalVoterCount, min_vote_count,
                                recent_poll.Options[0].VoterCount, recent_poll.Options[1].VoterCount),
                              replyToMessageId: poll_msg.MessageId,
                              parseMode: ParseMode.Markdown);
                    Logger.Log(LogType.Info, $"<{chat.Title}>: {options[0].VoterCount}<>{options[1].VoterCount} Poll result: Not enough votes");
                    return;
                }

                double ratio = (double)recent_poll.Options[0].VoterCount / (double)recent_poll.TotalVoterCount;
                if (ratio < vote_ratio)
                {
                    await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: string.Format(strManager["VOTEBAN_RATIO"], ratio * 100),
                              replyToMessageId: poll_msg.MessageId,
                              parseMode: ParseMode.Markdown);
                    Logger.Log(LogType.Info, $"<{chat.Title}>: {options[0].VoterCount}<>{options[1].VoterCount} Poll result: Decided not to ban");
                    return;
                }

                await FullyRestrictUserAsync(
                    chatId: message.Chat.Id,
                    userId: user.Id,
                    forSeconds: 60 * 15);

                await database.AddRestrictionAsync(user, e.Message.Chat.Id, 60 * 15);

                await Bot.SendTextMessageAsync(
                               chatId: message.Chat.Id,
                               text: string.Format(strManager["VOTEBAN_BANNED"], userlink,
                                recent_poll.Options[0].VoterCount, recent_poll.Options[1].VoterCount),
                               parseMode: ParseMode.Markdown);

                Logger.Log(LogType.Info, 
                    $"<{chat.Title}>: Poll result: {options[0].VoterCount}<>{options[1].VoterCount} The user has been banned!");

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
            DatabaseUpdate(sender, message_args.Message);
        }

        private static void DatabaseUpdate(object s, Message e)
        {
            try
            {
                DateTime myDateTime = DateTime.Now;
                string sqlFormattedDate = myDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                database.InsertRowAsync(new DbMessage()
                {
                    Id = e.MessageId,
                    UserId = e.From.Id,
                    Text = e.Text,
                    DateTime = sqlFormattedDate
                });


                var user = database.GetRowsByFilterAsync<DbUser>(u => u.Id == e.From.Id).Result;
                int? restrictionId = user.Count != 0 ? user.First().RestrictionId : null;

                database.InsertOrReplaceRowAsync(new DbUser()
                {
                    Id = e.From.Id,
                    FirstName = e.From.FirstName,
                    LastName = e.From.LastName,
                    Username = e.From.Username,
                    LastMessage = sqlFormattedDate,
                    RestrictionId = restrictionId
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
            try
            {
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
                    Bot.PromoteChatMemberAsync(message.Chat.Id, via_tcp_Id, true, false, false, true, true, true, true, true);
                }


                //remove to enable
                return;


                var user = database.GetRowsByFilterAsync<DbUser>(u => u.Id == message.From.Id).Result;
                if (user.Count == 0)
                {
                    database.InsertOrReplaceRowAsync(new DbUser()
                    {
                        Id = message.From.Id,
                        FirstName = message.From.FirstName,
                        LastName = message.From.LastName,
                        Username = message.From.Username,
                        LastMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        RestrictionId = null
                    });
                }

                Perchik.RestrictUserAsync(message.Chat.Id, message.From.Id, DateTime.Now.AddYears(420));


                var human_button = new InlineKeyboardButton();
                human_button.CallbackData = Path.GetRandomFileName();
                human_button.Text = strManager["CAPTCHA_HUMAN_BTN"];

                var bot_button = new InlineKeyboardButton();
                bot_button.CallbackData = Path.GetRandomFileName();
                bot_button.Text = strManager["CAPTCHA_BOT_BTN"];

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { human_button, bot_button } });

                var captcha_msg = Bot.SendTextMessageAsync(
                     chatId: message_args.Message.Chat.Id,
                     replyMarkup: inlineKeyboard,
                     text: string.Format(strManager["CAPTCHA"], Perchik.MakeUserLink(message.From)),
                     parseMode: ParseMode.Markdown).Result;

                bothelper.RegisterCallbackQuery(human_button.CallbackData, message.From.Id, onBotCheckButtonNoBot);
                bothelper.RegisterCallbackQuery(bot_button.CallbackData, message.From.Id, onBotCheckButtonBot);

                //Thread.Sleep(1000 * 60);

                //CallbackQuery fake_button_callback = new CallbackQuery();
                //fake_button_callback.From = message.From;
                //fake_button_callback.Message = message;

                //onBotCheckButtonBot(sender, new CallbackQueryArgs(fake_button_callback, message.From.Id));
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onBotCheckButtonNoBot(object sender, CallbackQueryArgs c)
        {
            try
            {
                Message message = c.Callback.Message;

                Bot.DeleteMessageAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId);

                var users = database.GetRowsByFilterAsync<DbUser>(u => u.Id == c.Callback.From.Id).Result;
                if (users.Count != 0 && users.First().RestrictionId != null)
                {
                    DbUser user = users.First();
                    var restriction = database.GetRowsByFilterAsync<DbRestriction>(r => r.Id == user.RestrictionId).Result;
                    var until = DateTime.Parse(restriction.First().DateTimeTo);
                    Perchik.RestrictUserAsync(message.Chat.Id, c.Callback.From.Id, until);

                }
                else
                {
                    Perchik.RestrictUserAsync(message.Chat.Id, c.Callback.From.Id, DateTime.Now.AddSeconds(1), canWriteMessages: true);
                }

                string username = "Ноунейм";
                username = $"@{c.Callback.From.Username}" ?? c.Callback.From.FirstName;

                string msg_string = String.Format(strManager["NEW_MEMBERS"], username);
                _ = Bot.SendTextMessageAsync(message.Chat.Id, msg_string);

                bothelper.RemoveCallbackQuery(c.Callback.Data);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void onBotCheckButtonBot(object sender, CallbackQueryArgs c)
        {
            Message message = c.Callback.Message;

            Bot.DeleteMessageAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId);

            Bot.KickChatMemberAsync(
                chatId: message.Chat.Id,
                userId: c.Callback.From.Id);

            Bot.UnbanChatMemberAsync(
                chatId: message.Chat.Id,
                userId: c.Callback.From.Id);

            Bot.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: string.Format(strManager["CAPTCHA_HUMAN"], Perchik.MakeUserLink(message.From)),
                  parseMode: ParseMode.Markdown);

            bothelper.RemoveCallbackQuery(c.Callback.Data);
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