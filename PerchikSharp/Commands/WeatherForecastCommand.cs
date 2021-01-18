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
        public string RegEx => @"прогноз\s([\w\s-]+)";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;
            var weatherMatch = command.Match;

            string search_url = Uri.EscapeUriString(
                $"http://dataservice.accuweather.com/locations/v1/cities/autocomplete?apikey={Program.tokens["ACCUWEATHER"]}&q={weatherMatch.Groups[1].Value}&language=ru-ru");
            try
            {
                string respone_str = Pieprz.HttpRequestAsync(search_url).Result;
                if (respone_str.Contains("The allowed number of requests has been exceeded."))
                {
                    await bot.SendTextMessageAsync(
                         chatId: message.Chat.Id,
                         text: $"*Количество запросов превышено!*",
                         parseMode: ParseMode.Markdown,
                         replyToMessageId: message.MessageId);
                    return;
                }

                dynamic locationJson = JsonConvert.DeserializeObject(respone_str);
                int locationCode = locationJson[0].Key;

                var currentUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{locationCode}?apikey={Program.tokens["ACCUWEATHER"]}&language=ru-ru&metric=true&details=true";
                respone_str = Pieprz.HttpRequestAsync(currentUrl).Result;

                dynamic weatherJson = JsonConvert.DeserializeObject(respone_str);

                await bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text:
                          string.Format(Program.strManager["WEATHER_FORECAST_MESSAGE"],
                          locationJson[0].LocalizedName, locationJson[0].Country.LocalizedName, weatherJson.DailyForecasts[0].Day.LongPhrase,
                          weatherJson.DailyForecasts[0].Temperature.Minimum.Value, weatherJson.DailyForecasts[0].Temperature.Maximum.Value,
                          weatherJson.DailyForecasts[0].Day.RainProbability, weatherJson.DailyForecasts[0].Day.Wind.Speed.Value,
                          weatherJson.DailyForecasts[0].Day.Wind.Direction.Localized),
                          parseMode: ParseMode.Markdown,
                          replyToMessageId: message.MessageId);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");

                await bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"*Нет такого .. {weatherMatch.Groups[1].Value.ToUpper()}!!😠*",
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
                    var resStream = w.Response.GetResponseStream();
                    var reader = new StreamReader(resStream ?? throw new InvalidOperationException());
                    if ((await reader.ReadToEndAsync()).Contains("The allowed number of requests has been exceeded."))
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
