using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;

namespace PerchikSharp.Commands
{
    class WeatherForecastCommand : IRegExCommand
    {
        public string RegEx { get { return @"прогноз\s([\w\s-]+)"; } }
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
                string respone_str = BotHelper.HttpRequestAsync(search_url).Result;
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

                string current_url = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{location_code}?apikey={Program.tokens["ACCUWEATHER"]}&language=ru-ru&metric=true&details=true";
                respone_str = BotHelper.HttpRequestAsync(current_url).Result;

                weather_json = JsonConvert.DeserializeObject(respone_str);

                await bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text:
                          string.Format(Program.strManager["WEATHER_FORECAST_MESSAGE"],
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
