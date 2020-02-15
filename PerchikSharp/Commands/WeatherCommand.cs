using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class WeatherCommand : IRegExCommand
    {
        public string RegEx { get { return @"погода\s([\w\s-]+)"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
            Message message = command.Message;
            Match weather_match = command.Match;

            string search_url = Uri.EscapeUriString(
                $"http://dataservice.accuweather.com/locations/v1/cities/autocomplete?apikey={Program.tokens["ACCUWEATHER"]}&q={weather_match.Groups[1].Value}&language=ru-ru");
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
                    await bot.SendTextMessageAsync(
                         chatId: message.Chat.Id,
                         text: $"*Количество запросов превышено!*",
                         parseMode: ParseMode.Markdown,
                         replyToMessageId: message.MessageId);
                    return;
                }

                location_json = JsonConvert.DeserializeObject(respone_str);

                location_code = location_json[0].Key;

                string current_url = $"http://dataservice.accuweather.com/currentconditions/v1/{location_code}?apikey={Program.tokens["ACCUWEATHER"]}&language=ru-ru&details=true";

                request = (HttpWebRequest)WebRequest.Create(current_url);
                response = (HttpWebResponse)request.GetResponse();
                resStream = response.GetResponseStream();

                reader = new StreamReader(resStream);
                respone_str = reader.ReadToEnd();

                weather_json = JsonConvert.DeserializeObject(respone_str);

                await bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text:
                          string.Format(Program.strManager["WEATHER_MESSAGE"],
                          location_json[0].LocalizedName, location_json[0].Country.LocalizedName, weather_json[0].WeatherText, weather_json[0].Temperature.Metric.Value,
                          weather_json[0].RealFeelTemperature.Metric.Value, weather_json[0].RelativeHumidity, weather_json[0].Wind.Direction.Localized, weather_json[0].Wind.Speed.Metric.Value),
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");

                await bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"*Нет такого .. {weather_match.Groups[1].Value.ToUpper()}!!😠*",
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (WebException w)
            {
                await bot.SendTextMessageAsync(
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
                        await bot.SendTextMessageAsync(
                               chatId: message.Chat.Id,
                               text: $"*Количество запросов превышено!*",//SMOKE WEED EVERYDAY
                               parseMode: ParseMode.Markdown,
                               replyToMessageId: message.MessageId);
                        return;
                    }

                    Logger.Log(LogType.Error, $"Exception: {w.Message}");
                    await bot.SendTextMessageAsync(
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
    }
}
