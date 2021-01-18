using Clarifai.API;
using Clarifai.API.Requests.Models;
using Clarifai.DTOs.Inputs;
using Clarifai.DTOs.Predictions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PerchikSharp.Commands;
using PerchikSharp.Db;
using PersikSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp
{
    internal class Program
    {
        public static Pieprz bot;
        static ClarifaiClient _clarifai;
        public static StringManager strManager = new StringManager();
        public static StringManager tokens = new StringManager();
        static RegExHelper _commands;

        static PerschikDB database;

        static readonly CancellationTokenSource ExitTokenSource = new CancellationTokenSource();
        static readonly CancellationToken ExitToken = ExitTokenSource.Token;

        private const long OfftopiaId = -1001125742098;
        private const int ViaTcpId = 204678400;

        private static async Task Main(string[] args)
        {

            Logger.Log(LogType.Info, $"Bot version: {Pieprz.botVersion}");

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            var file = new FileInfo("./Data/");
            file.Directory?.Create();

            database = new PerschikDB("./Data/database.db");
            database.Create();
            PerchikDB.ConnectionString = tokens["MYSQL"];

            _commands = new RegExHelper();

            Init();

            //Update Message to group and me
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "--update":
                            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
                            var changelog = string.Empty;
                            try
                            {
                                changelog = $"\n\n*Изменения:*\n{StringManager.FromFile("changelog.txt")}";
                                System.IO.File.Delete("changelog.txt");
                            }catch(FileNotFoundException)
                            {
                                
                            }

                            var text = $"*Перчик жив! 🌶*\nВерсия: {version}{changelog}";
                            _ = bot.SendTextMessageAsync(ViaTcpId,
                                                         text,
                                                         ParseMode.Markdown);
                            _ = bot.SendTextMessageAsync(OfftopiaId,
                                                         text,
                                                         ParseMode.Markdown);
                            break;
                        case "--close":
                            return;
                    }
                }
            }


            Console.Title = bot.Me.FirstName;

            try
            {
                bot.StartReceiving(Array.Empty<UpdateType>());
                Logger.Log(LogType.Info, $"Start listening for @{bot.Me.Username}");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"Exeption: {e.Message}");
                Console.ReadKey();
            }

            await StartDatabaseCheckAsync(5000);

            bot.StopReceiving();
        }


        private static void Init()
        {
            try
            {
                bot = new Pieprz(tokens["TELEGRAM"]);
                _clarifai = new ClarifaiClient(tokens["CLARIFAI"]);

                if (_clarifai.HttpClient.ApiKey == string.Empty)
                    throw new ArgumentException("CLARIFAI token isn't valid!");
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

            bot.onTextMessage += TextMessage;
            bot.onPhotoMessage += PhotoMessage;
            bot.onChatMembersAddedMessage += ChatMembersAddedMessage;
            bot.onDocumentMessage += DocumentMessage;
            bot.OnMessage += Bot_OnMessage;

            bot.RegexName = strManager["BOT_REGX"];
            bot.RegExCommand(new TestRegExCommand());
            bot.RegExCommand(new WeatherCommand());
            bot.RegExCommand(new WeatherForecastCommand());
            bot.RegExCommand(new UnbanCommand());
            bot.RegExCommand(new StatisticsCommand());
            bot.RegExCommand(new RoulletteCommand());
            bot.RegExCommand(new RandomCommand());
            bot.RegExCommand(new BanCommand());
            bot.RegExCommand(new KickCommand());
            bot.RegExCommand(new PraiseCommand());
            bot.RegExCommand(new InsultingCommand());
            bot.RegExCommand(new ByWordCommand());
            bot.RegExCommand(new WhoIsFoxCommand());
            bot.RegExCommand(new BananaCommand());
            bot.onNoneRegexMatched += PerchikCommand;


            bot.NativeCommand(new StartCommand());
            bot.NativeCommand(new InfoCommand());
            bot.NativeCommand(new RateCommand());
            bot.NativeCommand(new MeCommand());
            bot.NativeCommand(new VersionCommand());
            bot.NativeCommand(new PickleCommand());
            bot.NativeCommand(new StickerCommand());
            bot.NativeCommand(new TopBansCommand());
            bot.NativeCommand(new TopCommand());
            bot.NativeCommand(new VotebanCommand());
            bot.NativeCommand(new OfftopUnbanCommand());
            bot.NativeCommand(new EveryoneCommand());
            bot.NativeCommand(new AboutCommand());
            bot.NativeCommand(new PidrCommand());
            bot.NativeCommand(new DeleteCommand());
            bot.NativeCommand(new PidrmeCommand());
            bot.NativeCommand(new PidrstatsCommand());
            bot.NativeCommand(new GoogleCommand());
            bot.NativeCommand(new TestCommand());




            bot.onTextMessage += (_, a) => _commands.CheckMessage(a.Message);
            _commands.AddRegEx("(420|трав(к)?а|шишки|марихуана)", ((_, e) =>
            {
                bot.SendStickerAsync(e.Message.Chat.Id, "CAADAgAD0wMAApzW5wrXuBCHqOjyPQI",
                    replyToMessageId: e.Message.MessageId);
            }));




            bot.NativeCommand("fox", (_, e) => bot.SendTextMessageAsync(e.Message.Chat.Id, "🦊"));
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

        private static void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
           Task.Run(() => AddMsgToDatabase(sender, e.Message));
        }

        private static async Task StartDatabaseCheckAsync(int timeOut, CancellationToken token = default(CancellationToken))
        {
            while (!ExitToken.IsCancellationRequested)
            {
                CheckUserRestrictions();
                await Task.Delay(timeOut, token);
            }
        }

        private static async void CheckUserRestrictions()
        {
            try
            {
                await using var dbv2 = PerchikDB.GetContext();
                var users = dbv2.Users
                    .AsNoTracking()
                    .Where(u => u.Restricted)
                    .Select(x => new 
                    {
                        x.Id,
                        x.FirstName,
                        Restriction = x.Restrictions
                            .OrderByDescending(restriction => restriction.Until)
                            .FirstOrDefault()
                    })
                    .ToList();

                foreach (var user in users)
                {
                    var restriction = user.Restriction;
                    if (DateTime.UtcNow <= restriction.Until) continue;

                    if (dbv2.Users != null)
                        dbv2.Users
                            .FirstOrDefault(u => u.Id == user.Id)
                            .Restricted = false;

                    await dbv2.SaveChangesAsync();

                    await bot.SendTextMessageAsync(
                        chatId: restriction.ChatId,
                        text: string.Format(strManager["UNBANNED"], $"[{user.FirstName}](tg://user?id={user.Id})"),
                        parseMode: ParseMode.Markdown);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        //=====Persik Commands======
        private static async void PerchikCommand(object s, RegExArgs e)
        {
            Message message = e.Message;
            if (message.ReplyToMessage?.Type == MessageType.Photo)
            {
                Logger.Log(LogType.Info,
                    $"[{message.Chat.Type.ToString()}:{message.Type.ToString()}]({message.From.FirstName}:{message.From.Id}) Predict IID: {message.ReplyToMessage.Photo[0].FileId}");

                var names = await PredictImage(message.ReplyToMessage.Photo[^1]);

                await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetSingle("PREDICTION"), message.From.FirstName, names[0], names[1], names[2]),
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: message.MessageId);

                Logger.Log(LogType.Info, $"Result: {names[0]}:{names[1]}:{names[2]}. IID: {message.ReplyToMessage.Photo[0].FileId}");

                return;
            }
            else
            {
                Logger.Log(LogType.Info, $"<Perchik>({e.Message.From.FirstName}:{e.Message.From.Id}) -> NONE");
                await bot.SendTextMessageAsync(
                           chatId: e.Message.Chat.Id,
                           text: strManager.GetRandom("HELLO"),
                           parseMode: ParseMode.Markdown,
                           replyToMessageId: e.Message.MessageId);
            }
        }

        private static async Task<List<string>> PredictImage(PhotoSize ps)
        {
            var file = await bot.GetFileAsync(ps.FileId);
            var photo = new MemoryStream();
            await bot.DownloadFileAsync(file.FilePath, photo);


            var fileImage = new ClarifaiFileImage(photo.GetBuffer());
            var request = _clarifai.PublicModels.GeneralModel.Predict(fileImage, language: "ru");
            var result = await request.ExecuteAsync();

            var predictions = new List<string>();
            for (var i = 0; predictions.Count < 3; i++)
            {
                if (result.Get().Data[i].Name != "нет человек")
                    predictions.Add(result.Get().Data[i].Name);
            }

            return predictions;
        }

        private static async void NsfwDetect(Message message)//Упростить
        {
            const bool ENABLE_FILTER = true;

            try
            {
                var file = await bot.GetFileAsync(message.Photo[^1].FileId);
                var photo = new MemoryStream();
                await bot.DownloadFileAsync(file.FilePath, photo);

                var file_image = new ClarifaiFileImage(photo.GetBuffer());
                var request = _clarifai.PublicModels.NsfwModel.Predict(file_image, language: "en");
                var result = await request.ExecuteAsync();
                var nsfwVal = result.Get().Data.Find(x => x.Name == "nsfw").Value;

                if (nsfwVal != null && (float)nsfwVal > 0.7)
                {
                    await bot.SaveFileAsync(file.FileId, "./Data/nsfw");

                    if (ENABLE_FILTER)
                    {
                        await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                        if (message.Chat.Type != ChatType.Private)
                        {
                            var until = DateTime.UtcNow.AddSeconds(120);
                            await bot.RestrictUserAsync(message.Chat.Id, message.From.Id, until);

                            await using (var db = PerchikDB.GetContext())
                            {
                                var restriction = DbConverter.GenRestriction(message, until);
                                db.AddRestriction(restriction);
                            }

                            await bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: string.Format(strManager.GetSingle("NSFW_TRIGGER"), message.From.FirstName, 2, 1 - nsfwVal),
                              parseMode: ParseMode.Markdown);
                        }
                    }
                }
                else
                {
                    await bot.SaveFileAsync(file.FileId, "./Data/photos");
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        //======Bot Updates=========

        private static async void DocumentMessage(object sender, MessageArgs e)
        {
            var message = e.Message;

            try
            {
                await bot.SaveFileAsync(message.Document.FileId, "./Data/documents", message.Document.FileName);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }


        private static async void TextMessage(object sender, MessageArgs message_args)
        {
            var m = message_args.Message;

            if (m.Chat.Type != ChatType.Private ||
                m.Text[0] != '!') 
                return;

            var msg = m.Text.Substring(1, m.Text.Length - 1);
            await bot.SendTextMessageAsync(OfftopiaId, $"*{msg}*", ParseMode.Markdown);

            Logger.Log(LogType.Info, $"({m.From.FirstName}:{m.From.Id})(DM): {msg}");
        }

        private static async void AddMsgToDatabase(object s, Message msg)
        {
            try
            {
                await using var db = PerchikDB.GetContext();

                db.UpsertChat(DbConverter.GenChat(msg.Chat));

                var user = db.GetUserbyId(msg.From.Id);
                await db.UpsertUser(DbConverter.GenUser(msg.From, user?.Description), msg.Chat.Id);

                db.AddMessage(DbConverter.GenMessage(msg));
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static void PhotoMessage(object sender, MessageArgs message_args)
        {
            var message = message_args.Message;

            NsfwDetect(message);
        }

        private static async void ChatMembersAddedMessage(object sender, MessageArgs message_args)
        {
            try
            {
                var telegramChat = await bot.GetChatAsync(message_args.Message.Chat.Id);
                await using (var db = PerchikDB.GetContext())
                {
                    db.UpsertChat(DbConverter.GenChat(telegramChat));
                }

                if (message_args.Message.From.IsBot)
                    return;

                var message = message_args.Message;

                string username;
                if (message.From.Username != null)
                {
                    username = $"@{message.From.Username}";
                }
                else { username = bot.MakeUserLink(message.From); }

                var msgString = string.Format(strManager["NEW_MEMBERS"], username);
                await bot.SendTextMessageAsync(message.Chat.Id, msgString, ParseMode.Html);


                if(message.From.Id == ViaTcpId)
                {
                    Thread.Sleep(2000);
                    await bot.PromoteChatMemberAsync(message.Chat.Id, ViaTcpId, true, false, false, true, true, true, true, true);
                }


            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}