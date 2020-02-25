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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp
{
    class Program
    {
        public static Pieprz Bot;
        static ClarifaiClient clarifai;
        public static StringManager strManager = new StringManager();
        public static StringManager tokens = new StringManager();
        static RegExHelper commands;

        static PerschikDB database;

        static CancellationTokenSource exitTokenSource = new CancellationTokenSource();
        static CancellationToken exit_token = exitTokenSource.Token;

        const long offtopia_id = -1001125742098;
        const int via_tcp_Id = 204678400;

        static void Main(string[] args)
        {

            Logger.Log(LogType.Info, $"Bot version: {Pieprz.BotVersion}");

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            FileInfo file = new FileInfo("./Data/");
            file.Directory.Create();

            database = new PerschikDB("./Data/database.db");
            database.Create();
            PerchikDB.ConnectionString = tokens["MYSQL"];

            commands = new RegExHelper();

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


            Console.Title = Bot.Me.FirstName;

            try
            {
                Bot.StartReceiving(Array.Empty<UpdateType>());
                Logger.Log(LogType.Info, $"Start listening for @{Bot.Me.Username}");
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


        private static void Init()
        {
            try
            {
                Bot = new Pieprz(tokens["TELEGRAM"]);
                clarifai = new ClarifaiClient(tokens["CLARIFAI"]);

                if (clarifai.HttpClient.ApiKey == string.Empty)
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

            Bot.onTextMessage += onTextMessage;
            Bot.onPhotoMessage += onPhotoMessage;
            Bot.onChatMembersAddedMessage += onChatMembersAddedMessage;
            Bot.onDocumentMessage += onDocumentMessage;
            Bot.OnMessage += Bot_OnMessage;

            Bot.RegexName = strManager["BOT_REGX"];
            Bot.RegExCommand(new TestRegExCommand());
            Bot.RegExCommand(new WeatherCommand());
            Bot.RegExCommand(new WeatherForecastCommand());
            Bot.RegExCommand(new UnbanCommand());
            Bot.RegExCommand(new StatisticsCommand());
            Bot.RegExCommand(new RoulletteCommand());
            Bot.RegExCommand(new RandomCommand());
            Bot.RegExCommand(new BanCommand());
            Bot.RegExCommand(new KickCommand());
            Bot.RegExCommand(new PraiseCommand());
            Bot.RegExCommand(new InsultingCommand());
            Bot.RegExCommand(new ByWordCommand());           
            Bot.onNoneRegexMatched += onPerchikCommand;


            Bot.NativeCommand(new StartCommand());
            Bot.NativeCommand(new InfoCommand());
            Bot.NativeCommand(new RateCommand());
            Bot.NativeCommand(new MeCommand());
            Bot.NativeCommand(new VersionCommand());
            Bot.NativeCommand(new PickleCommand());
            Bot.NativeCommand(new StickerCommand());
            Bot.NativeCommand(new TopBansCommand());
            Bot.NativeCommand(new TopCommand());
            Bot.NativeCommand(new VotebanCommand());
            Bot.NativeCommand(new OfftopUnbanCommand());
            Bot.NativeCommand(new EveryoneCommand());
            Bot.NativeCommand(new AboutCommand());
            Bot.NativeCommand(new PidrCommand());
            Bot.NativeCommand(new DeleteCommand());
            Bot.NativeCommand(new PidrmeCommand());
            Bot.NativeCommand(new PidrstatsCommand());
            Bot.NativeCommand(new GoogleCommand());

            Bot.NativeCommand(new TestCommand());

            commands.AddRegEx("(420|трав(к)?а|шишки|марихуана)", ((_, e) =>
            {
                Bot.SendStickerAsync(e.Message.Chat.Id, "CAADAgAD0wMAApzW5wrXuBCHqOjyPQI",
                    replyToMessageId: e.Message.MessageId);
            }));


            Bot.NativeCommand("fox", (_, e) => Bot.SendTextMessageAsync(e.Message.Chat.Id, "🦊"));

            Bot.NativeCommand("migr", (_, e) =>
            {
                var users_old = database.GetRows<PersikSharp.Tables.DbUser>();
                var messages_old = database.GetRows<PersikSharp.Tables.DbMessage>();
                var restriction_old = database.GetRows<PersikSharp.Tables.DbRestriction>();

                using (var db = PerchikDB.GetContext())
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
                        using (var db = PerchikDB.GetContext())
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
                        using (var db = PerchikDB.GetContext())
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
                                if (i++ % 10 == 0)
                                    Logger.Log(LogType.Debug, $"Message #{i} ID {message.Id} : {message.Text}");

                            }
                            //db.AutoDetectChangesEnabled = false;
                        }
                    }
                    catch (Exception)
                    {
                        Logger.Log(LogType.Debug, $"ERROR {message.Id} : {message.Text}");
                    }
                }

                foreach (var restriction in restriction_old)
                {
                    try
                    {

                        using (var db = PerchikDB.GetContext())
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

        static void StartDatabaseCheck(object s)
        {
            CheckUserRestrictions();
        }

        static async void CheckUserRestrictions()
        {
            try
            {
                using (var dbv2 = PerchikDB.GetContext())
                {
                    var users = dbv2.Users
                        .AsNoTracking()
                        .Where(u => u.Restricted)
                        .Select(x => new 
                        {
                            x.Id,
                            x.FirstName,
                            Restriction = x.Restrictions
                                            .OrderByDescending(x => x.Until)
                                            .FirstOrDefault()
                        })
                        .ToList();

                    foreach (var user in users)
                    {
                        var restriction = user.Restriction;
                        if (DateTime.UtcNow > restriction.Until)
                        {
                            dbv2.Users
                                .FirstOrDefault(u => u.Id == user.Id)
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
        private static async void onPerchikCommand(object s, RegExArgs e)
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
            else
            {
                Logger.Log(LogType.Info, $"<Perchik>({e.Message.From.FirstName}:{e.Message.From.Id}) -> {"NONE"}");
                await Bot.SendTextMessageAsync(
                           chatId: e.Message.Chat.Id,
                           text: strManager.GetRandom("HELLO"),
                           parseMode: ParseMode.Markdown,
                           replyToMessageId: e.Message.MessageId);
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
                    await Bot.SaveFileAsync(file.FileId, "./Data/nsfw");

                    if (ENABLE_FILTER)
                    {
                        await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                        if (message.Chat.Type != ChatType.Private)
                        {
                            var until = DbConverter.DateTimeUTC2.AddSeconds(120);
                            await Bot.RestrictUserAsync(message.Chat.Id, message.From.Id, until);

                            await Bot.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: String.Format(strManager.GetSingle("NSFW_TRIGGER"), message.From.FirstName, 2, 1 - nsfw_val),
                              parseMode: ParseMode.Markdown);
                        }
                    }
                }
                else
                {
                    await Bot.SaveFileAsync(file.FileId, "./Data/photos");
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
                await Bot.SaveFileAsync(message.Document.FileId, "./Data/documents", message.Document.FileName);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }


        private static async void onTextMessage(object sender, MessageArgs message_args)
        {
            Message m = message_args.Message;

            //Message to superchat from privat Example: !Hello World
            if (m.Chat.Type == ChatType.Private && m.Text[0] == '!')
            {
                //if (BotHelper.isUserAdmin(offtopia_id, m.From.Id))
                //{
                    string msg = m.Text.Substring(1, m.Text.Length - 1);
                    await Bot.SendTextMessageAsync(offtopia_id, $"*{msg}*", ParseMode.Markdown);

                    Logger.Log(LogType.Info, $"({m.From.FirstName}:{m.From.Id})(DM): {msg}");
                //}
            }

            commands.CheckMessage(m);
        }

        private static async void AddMsgToDatabase(object s, Message msg)
        {
            try
            {
                using (var db = PerchikDB.GetContext())
                {

                    db.UpsertChat(DbConverter.GenChat(msg.Chat));

                    await db.UpsertUser(DbConverter.GenUser(msg.From), msg.Chat.Id);

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

        private static async void onChatMembersAddedMessage(object sender, MessageArgs message_args)
        {
            try
            {
                Chat telegram_chat = await Bot.GetChatAsync(message_args.Message.Chat.Id);
                using (var db = PerchikDB.GetContext())
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
                else { username = Bot.MakeUserLink(message.From); }

                string msg_string = String.Format(strManager["NEW_MEMBERS"], username);
                await Bot.SendTextMessageAsync(message.Chat.Id, msg_string, ParseMode.Html);


                if(message.From.Id == via_tcp_Id)
                {
                    Thread.Sleep(2000);
                    await Bot.PromoteChatMemberAsync(message.Chat.Id, via_tcp_Id, true, false, false, true, true, true, true, true);
                }


            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}