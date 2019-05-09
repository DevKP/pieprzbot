﻿using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Args;
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
using System.Speech.Synthesis;
using Telegram.Bot.Types.InputFiles;

namespace PersikSharp
{
    class Program
    {
        private static TelegramBotClient Bot;
        private static ClarifaiClient clarifai;
        private static BotCallBacks botcallbacks;
        private static StringManager strManager = new StringManager();

        private static Dictionary<string, string> tokens;
        private static bool exit = false;
        static void Main(string[] args)
        {

            CommandLine.Inst().onSubmitAction += PrintString;
            CommandLine.Inst().StartUpdating();

            Console.OutputEncoding = Encoding.UTF8;
            LoadDictionary();

            try
            {
                using (StreamReader streamReader = new StreamReader(strManager.GetSingle("TOKENS_PATH"), Encoding.UTF8))
                {
                    tokens = JsonConvert.DeserializeObject<Dictionary<string, string>>(streamReader.ReadToEnd());
                }

                Bot = new TelegramBotClient(tokens["TELEGRAM"]);
                clarifai = new ClarifaiClient(tokens["CLARIFAI"]);
                if (clarifai.HttpClient.ApiKey == "")
                    throw new ArgumentException("CLARIFAI token isnt valid!");

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

            botcallbacks.onTextEdited += onTextEdited;

            botcallbacks.RegisterCommand("/start", onStartCommand);
            botcallbacks.RegisterCommand("/info", onInfoCommand);
            botcallbacks.RegisterCommand("/rate", onRateCommand);
            botcallbacks.RegisterCommand("/y", (x, y) => {
                _ = Bot.SendTextMessageAsync(y.Message.Chat.Id, "*ХУЙ*", ParseMode.Markdown);
            });

            botcallbacks.RegisterCallbackQuery("update_rate", onRateUpdate);

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

        //========JOKE DELETE ПЛЕЕЕЗЕ===========
        private static void JokeLol(object s, CallbackQueryArgs e)
        {
            var button = new InlineKeyboardButton();
            button.CallbackData = "hoba";
            button.Text = e.Callback.From?.FirstName;
            var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

            _ = Bot.SendTextMessageAsync(e.Callback.Message.Chat.Id, "У меня есть кнопка!", replyMarkup: inlineKeyboard);
        }
        //=====Utils========
        private static void LoadDictionary()
        {
            try
            {
                strManager.Open("dict.json");
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

        public static async void PrintString(object sender, string str)
        {
            CommandLine.Text = "";

            var match = Regex.Match(str, @"ban:(.*):(.*):", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                try
                {
                    var until = DateTime.Now.AddSeconds(int.Parse(match.Groups[2].Value));
                    await Bot.RestrictChatMemberAsync("-1001125742098", 
                        int.Parse(match.Groups[1].Value), until, false, false, false, false);
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

        private static async Task<bool> isUserAdmin(long chatId, int userId)
        {

            ChatMember[] chat_members = await Bot.GetChatAdministratorsAsync(chatId);
            if (Array.Find(chat_members, e => e.User.Id == 1) != null)
                return true;
            else
                return false;
        }

        //=====Persik Commands======
        private static async void onPersikCommand(Message message)//Вынести в отдельный класс
        {
            //=======Regular expressions==========

            string ban_regex = @"\b(за)?бань?\b";
            var ban_match = Regex.Match(message.Text, ban_regex, RegexOptions.IgnoreCase);
            if (ban_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {ban_regex}");
                onPersikBanCommand(message, ban_match);
                return;
            }

            string choice_regex = @"([\w\s]+)\sили\s([\w\s]+)";
            var choice_match = Regex.Match(message.Text, choice_regex, RegexOptions.IgnoreCase);
            if (choice_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {choice_regex}");
                onRandomChoice(message, choice_match);
                return;
            }

            string by_regex = @".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?";
            var by_match = Regex.Match(message.Text, by_regex, RegexOptions.IgnoreCase);
            if (by_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {by_regex}");
                onByWord(message, by_match);
                return;
            }

            string weather_regex = @"погода\s([\w\s]+)";
            var weather_match = Regex.Match(message.Text, weather_regex, RegexOptions.IgnoreCase);
            if (weather_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {weather_regex}");
                onWeather(message, weather_match);
                return;
            }

            string tts_regex = @"скажи([\w\s!?,\-.:]+)";
            var tts_match = Regex.Match(message.Text, weather_regex, RegexOptions.IgnoreCase);
            if (tts_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {tts_regex}");
                onTTS(message, tts_match);
                return;
            }

            string insult_regex = @"дур[ао]к|пид[аоэ]?р|говно|д[еыи]бил|г[оа]ндон|лох|хуй|чмо|скотина|🖕🏻";
            var insult_match = Regex.Match(message.Text, insult_regex, RegexOptions.IgnoreCase);
            if (insult_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {insult_regex}");
                onBotInsulting(message, insult_match);
                return;
            }

            string praise_regex = @"мозг|живой|красав|молодец|хорош|умный|умница";
            var praise_match = Regex.Match(message.Text, praise_regex, RegexOptions.IgnoreCase);
            if (praise_match.Success)
            {
                Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {praise_regex}");
                onBotPraise(message, praise_match);
                return;
            }

            //==========================

            if (message.ReplyToMessage?.Type == MessageType.Photo)
            {
                Logger.Log(LogType.Info,
                    $"[{message.Chat.Type.ToString()}:{message.Type.ToString()}]({message.From.FirstName}:{message.From.Id}) Predict IID: {message.ReplyToMessage.Photo[0].FileId}");

                var names = await PredictImage(message.ReplyToMessage.Photo[message.ReplyToMessage.Photo.Length - 1]);

                _ = Bot.SendTextMessageAsync(message.Chat.Id,
                    String.Format(strManager.GetSingle("PREDICTION"), message.From.FirstName, names[0], names[1], names[2]),
                    ParseMode.Markdown, replyToMessageId: message.ReplyToMessage.MessageId);

                Logger.Log(LogType.Info, $"Result: {names[0]}:{names[1]}:{names[2]}. IID: {message.ReplyToMessage.Photo[0].FileId}");

                return;
            }

            Logger.Log(LogType.Info, $"[PERSIK]({message.From.FirstName}:{message.From.Id}) -> {"NONE"}");
            _ = Bot.SendTextMessageAsync(message.Chat.Id, 
                strManager.GetRandom("HELLO"), ParseMode.Markdown, replyToMessageId: message.MessageId);
        }

        private static void onTTS(Message message, Match tts_match)
        {
            throw new NotImplementedException();
        }

        private static void onWeather(Message message, Match weather_match)//Переделать под другой АПИ
        {
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
                    _ = Bot.SendTextMessageAsync(message.Chat.Id,
                    $"*Количество запросов превышено, лол!*", ParseMode.Markdown, replyToMessageId: message.MessageId);
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

                _ = Bot.SendTextMessageAsync(message.Chat.Id,
                    $"*{location_json[0].LocalizedName}, {location_json[0].Country.LocalizedName}\n\n{weather_json[0].WeatherText}\nТемпература: {weather_json[0].Temperature.Metric.Value}°C*", ParseMode.Markdown, replyToMessageId: message.MessageId);

            }catch(ArgumentOutOfRangeException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");

                _ = Bot.SendTextMessageAsync(message.Chat.Id,
                    $"*Нет такого .. {weather_match.Groups[1].Value.ToUpper()}!!😠*", ParseMode.Markdown, replyToMessageId: message.MessageId);
            }catch(WebException w)
            {
                Stream resStream = w.Response.GetResponseStream();
                StreamReader reader = new StreamReader(resStream);
                if (reader.ReadToEnd().Contains("The allowed number of requests has been exceeded."))
                {
                    _ = Bot.SendTextMessageAsync(message.Chat.Id,
                    $"*Количество запросов превышено, лол!*", ParseMode.Markdown, replyToMessageId: message.MessageId);
                    return;
                }

                Logger.Log(LogType.Error, $"Exception: {w.Message}");
                _ = Bot.SendTextMessageAsync(message.Chat.Id,
                    w.Message, ParseMode.Markdown, replyToMessageId: message.MessageId);
            }catch(Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
        }

        private static async void onPersikBanCommand(Message message, Match ban_match)
        {
            if (message.Chat.Type == ChatType.Private)
                return;


            const int default_second = 40;
            int seconds = default_second;
            int number = default_second;
            string word = "сек.";

            var match = Regex.Match(message.Text, @"(\d{1,9})\W?([смчд])?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                number = int.Parse(match.Groups[1].Value);
                seconds = number;

                if (match.Groups[2].Length > 0)
                {
                    switch (match.Groups[2].Value.First())
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
            }

            try
            {
                var until = DateTime.Now.AddSeconds(seconds);
                if (message.ReplyToMessage != null)
                {
                    if (!await isUserAdmin(message.Chat.Id, message.From.Id))
                        return;

                    if (message.ReplyToMessage.From.Id == Bot.BotId)
                        return;

                    await Bot.RestrictChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id, until, false, false, false, false);
                    if (seconds >= 40)
                    {
                        _ = Bot.SendTextMessageAsync(message.Chat.Id,
                            String.Format(strManager.GetSingle("BANNED"), message.ReplyToMessage.From.FirstName, number, word), ParseMode.Markdown);
                    }
                    else
                    {
                        _ = Bot.SendTextMessageAsync(message.Chat.Id,
                            String.Format(strManager.GetSingle("SELF_PERMANENT"), message.ReplyToMessage.From.FirstName, number, word), ParseMode.Markdown);
                    }
                }
                else
                {
                    if (seconds >= 40)
                    {
                        await Bot.RestrictChatMemberAsync(message.Chat.Id, message.From.Id, until, false, false, false, false);
                        _ = Bot.SendTextMessageAsync(message.Chat.Id,
                            String.Format(strManager.GetSingle("SELF_BANNED"), message.From.FirstName, number, word), ParseMode.Markdown);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }

        private static void onByWord(Message message, Match by_match)
        {
            Bot.SendStickerAsync(message.Chat.Id, "CAADAgADGwAD0JwyGF7MX7q4n6d_Ag");
            if(message.Chat.Type != ChatType.Private)
            {
                try
                {
                    _ = Bot.SendTextMessageAsync(message.Chat.Id,
                        string.Format(strManager.GetSingle("BYWORD_BAN"), message.From.FirstName), ParseMode.Markdown);
                    var until = DateTime.Now.AddSeconds(60 * 5);
                    _ = Bot.RestrictChatMemberAsync(message.Chat.Id, message.From.Id, until, false, false, false, false);
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, $"Exception: {e.Message}");
                }
            }
        }

        private static void onBotPraise(Message message, Match praise_match)
        {
            Bot.SendStickerAsync(message.Chat.Id, "CAADAgADQQMAApFfCAABzoVI0eydHSgC");
        }

        private static async void onBotInsulting(Message message, Match insult_match)
        {

            try
            {
                await Bot.SendStickerAsync(message.Chat.Id, "CAADAgADJwMAApFfCAABfVrdPYRn8x4C");

                if(message.Chat.Type != ChatType.Private)
                {
                    await Task.Delay(2000);

                    var until = DateTime.Now.AddSeconds(120);
                    await Bot.RestrictChatMemberAsync(message.Chat.Id, message.From.Id, until, false, false, false, false);
                    _ = Bot.SendTextMessageAsync(message.Chat.Id, 
                        String.Format(strManager.GetSingle("BANNED"), message.From.FirstName, 2, "мин."), ParseMode.Markdown);
                    _ = Bot.SendStickerAsync(message.Chat.Id, "CAADAgADPQMAApFfCAABt8Meib23A_QC");
                }
            }catch(Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }

        }

        private static void onRandomChoice(Message message, Match choice_match)
        {
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

                Bot.SendTextMessageAsync(message.Chat.Id, String.Format(strManager.GetRandom("CHOICE"), result),
                    ParseMode.Markdown, replyToMessageId: message.MessageId);
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

                if ((float)nsfw_val > 0.8)
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
                        await Bot.RestrictChatMemberAsync(message.Chat.Id, message.From.Id, until, false, false, false, false);

                        await Bot.SendTextMessageAsync(message.Chat.Id,
                            String.Format(strManager.GetSingle("NSFW_TRIGGER"), message.From.FirstName, 2, 1 - nsfw_val), ParseMode.Markdown);
                    }
                }
                else
                {
                    bool exists = System.IO.Directory.Exists("./photos/");
                    if (!exists)
                        System.IO.Directory.CreateDirectory("./photos/");
                    using (FileStream file_stream = new FileStream($"./photos/{file.FileId}.jpg",
                        FileMode.Create, System.IO.FileAccess.Write))
                    {
                        photo.WriteTo(file_stream);
                        file_stream.Flush();
                        file_stream.Close();
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }

        //======Bot Updates=========
        private static async void onTextMessage(object sender, MessageArgs message_args)
        {
            Message m = message_args.Message;

            //Message to superchat from privat Example: !Hello World
            if (m.Chat.Type == ChatType.Private && m.Text[0] == '!')
            {
                if (await isUserAdmin(-1001125742098, m.From.Id))
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
                _ = Bot.RestrictChatMemberAsync(message.Chat.Id,
                        message.From.Id, until, false, false, false, false);
            }
            ///Spam Bot detection
            

            string msg_string = String.Format(strManager.GetSingle("NEW_MEMBERS"), username);
            _ = Bot.SendTextMessageAsync(message.Chat.Id, msg_string);
        }

        //=======Bot commands========
        private static void onRateCommand(object sender, MessageArgs message_args)
        {
            var msg = Bot.SendTextMessageAsync(message_args.Message.Chat.Id, "*Обновление...*",parseMode: ParseMode.Markdown).Result;
            var cq = new CallbackQuery();
            cq.Message = msg;
            cq.InlineMessageId = msg.MessageId.ToString();
            cq.From = msg.From;

            onRateUpdate(sender, new CallbackQueryArgs(cq));
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
                _ = Bot.EditMessageTextAsync(e.Callback.Message.Chat.Id, e.Callback.Message.MessageId, formated_str, replyMarkup: inlineKeyboard);
            }
            catch (WebException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}");
            }
        }

        private static void onStartCommand(object sender, MessageArgs message_args)
        {
            if (message_args.Message.Chat.Type == ChatType.Private)
                Bot.SendTextMessageAsync(message_args.Message.Chat.Id, String.Format(strManager.GetSingle("START"), message_args.Message.From.FirstName));
        }

        private static void onInfoCommand(object sender, MessageArgs message_args)
        {
            Message message = message_args.Message;

            Bot.SendTextMessageAsync(message.Chat.Id, strManager.GetSingle("INFO"), ParseMode.Markdown);
        }
        //==========================
    }
}
