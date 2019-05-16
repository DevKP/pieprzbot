﻿using System;
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

namespace PersikSharp
{
    class Program
    {
        public static TelegramBotClient Bot;
        private static Persik persik;
        private static ClarifaiClient clarifai;
        private static BotCallBacks botcallbacks;
        private static StringManager strManager = new StringManager();
        private static StringManager tokens = new StringManager();

        //private static Dictionary<string, string> tokens;

        private static bool exit = false;
        static void Main(string[] args)
        {
            Process current = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(current.ProcessName))
            {
                if (process.Id != current.Id)
                {
                    process.Kill();
                }
            }
            //foreach (Process process in Process.GetProcessesByName("PersikSharpRelease"))
            //{
            //    if (process.Id != current.Id)
            //    {
            //        process.Kill();
            //    }
            //}

            CommandLine.Inst().onSubmitAction += PrintString;
            CommandLine.Inst().StartUpdating();

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            try
            {
                persik = new Persik();
                Bot = new TelegramBotClient(tokens.GetSingle("TELEGRAM"));
                clarifai = new ClarifaiClient(tokens["CLARIFAI"]);
                if (clarifai.HttpClient.ApiKey == "")
                    throw new ArgumentException("CLARIFAI token isn't valid!");

                botcallbacks = new BotCallBacks(Bot);
            }
            catch(FileNotFoundException e)
            {
                Logger.Log(LogType.Fatal, $"No tokens file found! Exception: {e.Message}");
                Console.ReadKey();
                return;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"<{e.Source}> {e.Message}");
                Console.ReadKey();
                return;
            }


            botcallbacks.onTextMessage += onTextMessage;
            botcallbacks.onTextMessage += onTextMessageFilter;
            botcallbacks.onPhotoMessage += onPhotoMessage;
            botcallbacks.onStickerMessage += onStickerMessage;
            botcallbacks.onChatMembersAddedMessage += onChatMembersAddedMessage;
            botcallbacks.onDocumentMessage += onDocumentMessage;
            botcallbacks.onTextEdited += onTextEdited;


            botcallbacks.RegisterCommand("start", onStartCommand);
            botcallbacks.RegisterCommand("info", onInfoCommand);
            botcallbacks.RegisterCommand("rate", onRateCommand);
            botcallbacks.RegisterCommand("me", onMeCommand);
            botcallbacks.RegisterCommand("upal_otjalsa", onUpalOtjalsaCommand);
            botcallbacks.RegisterCommand("version", onVersionCommand);
            botcallbacks.RegisterCallbackQuery("update_rate", onRateUpdate);


            persik.AddCommandRegEx(@"\b(за)?бань?\b", onPersikBanCommand);                                    //забань
            persik.AddCommandRegEx(@"\bра[зс]бань?\b", onPersikUnbanCommand);                                 //разбань
            persik.AddCommandRegEx(@"([\w\s]+)\sили\s([\w\s]+)", onRandomChoice);                             //один ИЛИ два
            persik.AddCommandRegEx(@".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?", onByWord);//беларуссия
            persik.AddCommandRegEx(@"погода\s([\w\s]+)", onWeather);                                          //погода ГОРОД
            persik.AddCommandRegEx(@"\b(дур[ао]к|пид[аоэ]?р|говно|д[еыи]бил|г[оа]ндон|лох|хуй|чмо|скотина)\b", onBotInsulting);//CENSORED
            persik.AddCommandRegEx(@"\b(мозг|живой|красав|молодец|хорош|умный|умница)\b", onBotPraise);       //
            persik.AddCommandRegEx(@"\bрулетк[уа]?\b", onRouletteCommand);                                    //рулетка
            persik.onNoneMatched += onNoneMatchedCommand;

            //Update Message to group and me
            if(args.Length > 0)
                if (args.First().Equals("/u"))
                {
                    Bot.SendTextMessageAsync("204678400",
                        $"*Updated to version: {FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion}*",
                        ParseMode.Markdown);
                    Bot.SendTextMessageAsync("-1001125742098",
                        $"*Updated to version: {FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion}*",
                        ParseMode.Markdown);
                }


            var me = Bot.GetMeAsync().Result;
            Console.Title = me.FirstName;

            try
            {
                Bot.StartReceiving(Array.Empty<UpdateType>());
                Logger.Log(LogType.Info, $"Start listening for @{me.Username}");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Fatal, $"Exeption: {e.Message}");
                Console.ReadKey();
            }


            while (!exit)
                Thread.Sleep(1000);

            Bot.StopReceiving();
            CommandLine.Inst().StopUpdating();
        }

        //=====Utils======== ВЫНЕСТИ ВСЕ В ОТДЕЛЬНОЕ МЕСТО
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
            catch(Exception e)
            {
                Logger.Log(LogType.Fatal, $"<{e.Source}> {e.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        public static async void PrintString(object sender, CommandLineEventArgs e)
        {
            CommandLine.Text = "";

            string str = e.Text;

            var match = Regex.Match(str, @"ban:(.*):(.*):", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                try
                {
                    var until = DateTime.Now.AddSeconds(int.Parse(match.Groups[2].Value));
                    await Bot.RestrictChatMemberAsync(
                        chatId: "-1001125742098",
                        userId: int.Parse(match.Groups[1].Value),
                        untilDate: until,
                        canSendMessages: false,
                        canSendMediaMessages: false,
                        canSendOtherMessages: false,
                        canAddWebPagePreviews: false);
                }
                catch (Exception exp)
                {
                    Logger.Log(LogType.Error, $"Exception: {exp.Message}");
                }

                Logger.Log(LogType.Info, $"User {match.Groups[1].Value} - RESTRICTED!");
                return;
            }
            if (str[0] == '!') { 
                 await Bot.SendTextMessageAsync("-1001125742098", str.Substring(1, str.Length - 1), ParseMode.Markdown);
                Logger.Log(LogType.Info, $"(ME) {str}");
                return;
            }
            if (str.Contains("exit"))
                exit = true;
            else
                Logger.Log(LogType.Info, $"{str}  <- Syntax Error!");
        }

        private static bool isUserAdmin(long chatId, int userId)
        {

            ChatMember[] chat_members = Bot.GetChatAdministratorsAsync(chatId).Result;
            if (Array.Find(chat_members, e => e.User.Id == userId) != null)
                return true;
            else
                return false;
        }

        private static async void SaveFile(string fileId, string folder, string fileName = null)
        {
            try
            {
                var file = Bot.GetFileAsync(fileId).Result;
                MemoryStream docu = new MemoryStream();

                const int attempts = 5;
                for (int a = 0; a < attempts; a++)
                {
                    try
                    {
                        await Bot.DownloadFileAsync(file.FilePath, docu);
                        break;
                    }
                    catch (HttpRequestException)
                    {
                        Logger.Log(LogType.Info, $"<Downloader>: Bad Request, attempt #{a}");
                        continue;
                    }
                }


                string file_ext = file.FilePath.Split('.')[1];
                if (fileName == null)
                    fileName = $"{fileId}.{file_ext}";

                bool exists = System.IO.Directory.Exists($"./{folder}/");
                if (!exists)
                    System.IO.Directory.CreateDirectory($"./{folder}/");
                using (FileStream file_stream = new FileStream($"./{folder}/{fileName}",
                    FileMode.Create, System.IO.FileAccess.Write))
                {
                    docu.WriteTo(file_stream);
                    file_stream.Flush();
                    file_stream.Close();
                }
                Logger.Log(LogType.Info, $"<Downloader>: Filename: {fileName} saved.");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
        }

        /// <summary>
        /// Generates clickable text link to user profile.
        /// </summary>
        /// <param name="user">User object.</param>
        /// <returns>
        /// Formated text string.
        /// </returns>
        public static string GetUserLink(User user)
        {
            try
            {
                return string.Format("[{0}](tg://user?id={1})", user.FirstName, user.Id);
            }
            catch (NullReferenceException)//If FirstName is null using id as name
            {
                return string.Format("[{0}](tg://user?id={1})", user.Id, user.Id);
            }

        }

        //=====Persik Commands======
        private static async void onPersikCommand(Message message)
        {
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

            persik.ParseMessage(message);
        }

        private static void onWeather(object sender, PersikEventArgs a)//Переделать под другой АПИ
        {
            Message message = a.Message;
            Match weather_match = a.Match;

            string search_url = System.Uri.EscapeUriString(
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

                if(respone_str.Contains("The allowed number of requests has been exceeded."))
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
            }catch(ArgumentOutOfRangeException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");

                _ = Bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"*Нет такого .. {weather_match.Groups[1].Value.ToUpper()}!!😠*",
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }catch(WebException w)
            {
                Stream resStream = w.Response.GetResponseStream();
                StreamReader reader = new StreamReader(resStream);
                if (reader.ReadToEnd().Contains("The allowed number of requests has been exceeded."))
                {
                    _ = Bot.SendTextMessageAsync(
                           chatId: message.Chat.Id,
                           text: $"*Количество запросов превышено, лол!*",
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
            }catch(Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
        }

        private static void onNoneMatchedCommand(object sender, PersikEventArgs e)
        {
            Logger.Log(LogType.Info, $"[PERSIK]({e.Message.From.FirstName}:{e.Message.From.Id}) -> {"NONE"}");
            _ = Bot.SendTextMessageAsync(
                       chatId: e.Message.Chat.Id,
                       text: strManager.GetRandom("HELLO"),
                       parseMode: ParseMode.Markdown,
                       replyToMessageId: e.Message.MessageId);
        }

        private static async void onPersikBanCommand(object sender, PersikEventArgs e)//Переделать
        {
            Message message = e.Message;

            if (message.Chat.Type == ChatType.Private)
                return;

            const int default_second = 40;
            int seconds = default_second;
            int number = default_second;
            string word = "сек.";

            var match = Regex.Match(message.Text, @"(?<number>\d{1,9})\W?(?<letter>[смчд])?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                number = int.Parse(match.Groups["number"].Value);
                seconds = number;

                if (match.Groups["letter"].Length > 0)
                {
                    switch (match.Groups["letter"].Value.First())
                    {
                        case 'с':
                            seconds = number;
                            word = "сек.";
                            break;
                        case 'м':
                            seconds *= 60;
                            word = "мин.";
                            //SMOKE WEED EVERYDAY
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
            }

            try
            {
                var until = DateTime.Now.AddSeconds(seconds);
                if (message.ReplyToMessage != null)
                {
                    if (!isUserAdmin(message.Chat.Id, message.From.Id))
                        return;
                    
                    if (message.ReplyToMessage.From.Id == Bot.BotId)
                        return;

                    await Bot.RestrictChatMemberAsync(
                            chatId: message.Chat.Id,
                            userId: message.ReplyToMessage.From.Id,
                            untilDate: until,
                            canSendMessages: false,
                            canSendMediaMessages: false,
                            canSendOtherMessages: false,
                            canAddWebPagePreviews: false);
;
                    if (seconds >= 40)
                    {
                        _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(strManager.GetSingle("BANNED"), GetUserLink(message.ReplyToMessage.From), number, word),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        _ = Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(strManager.GetSingle("SELF_PERMANENT"), GetUserLink(message.ReplyToMessage.From), number, word),
                            parseMode: ParseMode.Markdown);
                    }
                }
                else
                {
                    if (seconds >= 40)
                    {
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
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), GetUserLink(message.From), number, word),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        until = DateTime.Now.AddSeconds(40);
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
                            text: String.Format(strManager.GetSingle("SELF_BANNED"), GetUserLink(message.From), 40, word),
                            parseMode: ParseMode.Markdown);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }

        private static void onPersikUnbanCommand(object sender, PersikEventArgs e)
        {
            Message message = e.Message;

            if (message.Chat.Type == ChatType.Private)
                return;
            if (!isUserAdmin(message.Chat.Id, message.From.Id))
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

                _ = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetRandom("UNBANNED"), GetUserLink(message.ReplyToMessage.From)),
                        parseMode: ParseMode.Markdown);
            }
            catch(Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        private static void onByWord(object sender, PersikEventArgs e)
        {
            Message message = e.Message;

            Bot.SendStickerAsync(message.Chat.Id, "CAADAgADGwAD0JwyGF7MX7q4n6d_Ag");
            if(message.Chat.Type != ChatType.Private)
            {
                try
                {
                    _ =  Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(strManager.GetSingle("BYWORD_BAN"), message.From.FirstName),
                        parseMode: ParseMode.Markdown);

                    var until = DateTime.Now.AddSeconds(60 * 5);
                    _ = Bot.RestrictChatMemberAsync(
                        chatId: message.Chat.Id,
                        userId: message.From.Id,
                        untilDate: until,
                        canSendMessages: false,
                        canSendMediaMessages: false,
                        canSendOtherMessages: false,
                        canAddWebPagePreviews: false);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error, $"Exception: {ex.Message}");
                }
            }
        }

        private static void onBotPraise(object sender, PersikEventArgs e)
        {
            Message message = e.Message;
            Bot.SendStickerAsync(message.Chat.Id, "CAADAgADQQMAApFfCAABzoVI0eydHSgC");
        }

        private static async void onBotInsulting(object sender, PersikEventArgs e)
        {
            Message message = e.Message;
            try
            {
                await Bot.SendStickerAsync(message.Chat.Id, "CAADAgADJwMAApFfCAABfVrdPYRn8x4C");

                if(message.Chat.Type != ChatType.Private)
                {
                    await Task.Delay(2000);

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
                        text: String.Format(strManager.GetSingle("BANNED"), message.From.FirstName, 2, "мин."),
                        parseMode: ParseMode.Markdown);

                    _ = Bot.SendStickerAsync(message.Chat.Id, "CAADAgADPQMAApFfCAABt8Meib23A_QC");
                }
            }catch(Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }

        }

        private static void onRandomChoice(object sender, PersikEventArgs e)
        {
            Message message = e.Message;

            string only_choice_str = "";
            var temp_match = Regex.Match(message.Text, @"(п[eеэpр]+[pрeеэ][ч][ик]+?(к|ч[eеэ]к))", RegexOptions.IgnoreCase);
            if (temp_match.Groups[1].Index + temp_match.Groups[1].Length < message.Text.Length)
                only_choice_str = message.Text.Substring(temp_match.Groups[1].Index + temp_match.Groups[1].Length);
            else
                only_choice_str = message.Text.Replace(temp_match.Groups[1].Value, "");

            var match = Regex.Match(only_choice_str, @"([\w\s]+)\sили\s([\w\s]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Random rand = new Random();
                string result = "none";

                if (rand.NextDouble() >= 0.5)
                {
                    result = match.Groups[1].Value;
                }
                else
                {
                    result = match.Groups[2].Value;
                }

                _ = Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: String.Format(strManager.GetRandom("CHOICE"), result),
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: message.MessageId).Result;
            }
        }

        private static void onRouletteCommand(object sender, PersikEventArgs e)
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
                    var until = DateTime.Now.AddSeconds(10*60); //10 minutes
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
                        text: String.Format(strManager.GetRandom("ROULETTEBAN"), GetUserLink(message.From)),
                        parseMode: ParseMode.Markdown).Result;
                }
                else
                {
                    var msg = Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(strManager.GetRandom("ROULETTEMISS"), GetUserLink(message.From)),
                        parseMode: ParseMode.Markdown).Result;

                    Thread.Sleep(10 * 1000); //wait 10 seconds

                    Bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: msg.MessageId);
                    Bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId);
                }
            }catch(Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        private static async Task<List<string>> PredictImage(PhotoSize ps)
        {
            var file = await Bot.GetFileAsync(ps.FileId);
            MemoryStream photo = new MemoryStream();
            await Bot.DownloadFileAsync(file.FilePath, photo);


            ClarifaiFileImage file_image = new ClarifaiFileImage(photo.GetBuffer());
            PredictRequest<Concept> request = clarifai.PublicModels.GeneralModel.Predict(file_image, language: "ru");
            var result = await request.ExecuteAsync();

            List<string> predictions = new List<string>();

            for (int i = 0; predictions.Count < 3; i++)
            {   if(result.Get().Data[i].Name != "нет человек")
                    predictions.Add(result.Get().Data[i].Name);
            }

            return predictions;
        }

        private static async void NSFWDetect(Message message)//Упростить
        {
            
            try
            {
                var file = await Bot.GetFileAsync(message.Photo[message.Photo.Length - 1].FileId);
                MemoryStream photo = new MemoryStream();
                await Bot.DownloadFileAsync(file.FilePath, photo);

                ClarifaiFileImage file_image = new ClarifaiFileImage(photo.GetBuffer());
                PredictRequest<Concept> request = clarifai.PublicModels.NsfwModel.Predict(file_image, language: "en");
                var result = await request.ExecuteAsync();
                var nsfw_val = result.Get().Data.Find(x => x.Name == "nsfw").Value;

                if ((float)nsfw_val > 1.0)//Set to 0.8 to fix
                {
                    await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);


                    bool exists = System.IO.Directory.Exists("./nsfw/");
                    if (!exists)
                        System.IO.Directory.CreateDirectory("./nsfw/");

                    using (FileStream file_stream = new FileStream($"./nsfw/{file.FileId}.jpg", 
                        FileMode.Create, System.IO.FileAccess.Write))
                    {
                        photo.WriteTo(file_stream);
                        file_stream.Flush();
                        file_stream.Close();
                    }


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
                else
                {
                    SaveFile(file.FileId, "photos");
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }

        //======Bot Updates=========

        private static void onDocumentMessage(object sender, MessageArgs e)
        {
            Message message = e.Message;

            try
            {
                SaveFile(message.Document.FileId, "documents", message.Document.FileName);
                //Logger.Log(LogType.Info, $"<Document>: Filename: {message.Document.FileName} downloaded.");
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }


        private static void onTextMessage(object sender, MessageArgs message_args)
        {
            Message m = message_args.Message;

            //Message to superchat from privat Example: !Hello World
            if (m.Chat.Type == ChatType.Private && m.Text[0] == '!')
            {
                if (isUserAdmin(-1001125742098, m.From.Id))
                {
                    string msg = m.Text.Substring(1, m.Text.Length - 1);
                    _ = Bot.SendTextMessageAsync("-1001125742098", $"*{msg}*", ParseMode.Markdown);

                    Logger.Log(LogType.Info, $"({m.From.FirstName}:{m.From.Id}) (ME) {msg}");
                }
            }

            if (Regex.IsMatch(m.Text, @".*п[eеэpр]+[pрeеэ][ч][ик]+?(к|ч[eеэ]к).*", RegexOptions.IgnoreCase))
            {
                onPersikCommand(m);
            }
        }

        private static void onTextEdited(object sender, MessageArgs message_args)
        {
            Message message = message_args.Message;

            Logger.Log(LogType.Info, $"[EDITED MESSAGE] ({message.From.FirstName}:{message.From.Id}): {message.Text}");
            onTextMessage(sender, message_args);
        }

        private static void onTextMessageFilter(object sender, MessageArgs e)
        {
            if (e.Message.Chat.Type == ChatType.Supergroup)
            {
                if (e.Message.Text.Contains("LE9Xo1hHKm6CkkJpGg3Qrg"))
                {
                    _ = Bot.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                    Logger.Log(LogType.Info, $"<TextFilter> Message deleted.");
                }
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
            ///Spam Bot detection
            

            string msg_string = String.Format(strManager.GetRandom("NEW_MEMBERS"), username);
            _ = Bot.SendTextMessageAsync(message.Chat.Id, msg_string);
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
            catch(Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }

            
        }

        private static void onRateUpdate(object sender, CallbackQueryArgs e)
        {
            //Message message = message_args.Message;
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


                //_ = Bot.SendTextMessageAsync(message.Chat.Id, formated_str, replyMarkup: inlineKeyboard);
                _ = Bot.EditMessageTextAsync(
                     chatId: e.Callback.Message.Chat.Id,
                     messageId: e.Callback.Message.MessageId,
                     replyMarkup: inlineKeyboard,
                     text: formated_str).Result;
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }

        private static void onStartCommand(object sender, CommandEventArgs message_args)
        {
            try
            {
                if (message_args.Message.Chat.Type == ChatType.Private)
                _ = Bot.SendTextMessageAsync(
                          chatId: message_args.Message.Chat.Id,
                          text: String.Format(strManager.GetSingle("START"), message_args.Message.From.FirstName)).Result;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
        }

        private static void onInfoCommand(object sender, CommandEventArgs message_args)
        {
            Message message = message_args.Message;

            _ = Bot.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: strManager.GetSingle("INFO"),
                       parseMode: ParseMode.Markdown).Result;
        }
        private static void onMeCommand(object sender, CommandEventArgs message_args)
        {
            if (message_args.Text == "")
                return;

            Message message = message_args.Message;
            string msg_text = $"{GetUserLink(message.From)} *{message_args.Text}*";

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
            catch(Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
            //Bot.SendTextMessageAsync(message.Chat.Id, message_args.Text, ParseMode.Markdown);
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
                      performer:"Жизнь",
                      title:"Не слушать!"
                    ).Result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        private static void onVersionCommand(object sender, CommandEventArgs e)
        {
            try
            {
                _ = Bot.SendTextMessageAsync(
                       chatId: e.Message.Chat.Id,
                       text: $"*Version: {FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion}*",
                       parseMode: ParseMode.Markdown).Result;
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}");
            }
        }

        //==========================
    }
}
