using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class WeatherCommand : IRegExCommand
    {
        public string RegEx => @"погода\s([\w\s-]+)";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;
            var weatherMatch = command.Match;

            var searchUrl = Uri.EscapeUriString(
                $"http://dataservice.accuweather.com/locations/v1/cities/autocomplete?apikey={Program.tokens["ACCUWEATHER"]}&q={weatherMatch.Groups[1].Value}&language=ru-ru");
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(searchUrl);
                var response = (HttpWebResponse)request.GetResponse();
                var resStream = response.GetResponseStream();

                var reader = new StreamReader(resStream ?? throw new InvalidOperationException());
                var responseStr = await reader.ReadToEndAsync();

                if (responseStr.Contains("The allowed number of requests has been exceeded."))
                {
                    await bot.SendTextMessageAsync(
                         chatId: message.Chat.Id,
                         text: $"*Количество запросов превышено!*",
                         parseMode: ParseMode.Markdown,
                         replyToMessageId: message.MessageId);
                    return;
                }

                dynamic locationJson = JsonConvert.DeserializeObject(responseStr);

                int locationCode = locationJson[0].Key;

                var currentUrl = $"http://dataservice.accuweather.com/currentconditions/v1/{locationCode}?apikey={Program.tokens["ACCUWEATHER"]}&language=ru-ru&details=true";

                request = (HttpWebRequest)WebRequest.Create(currentUrl);
                response = (HttpWebResponse)request.GetResponse();
                resStream = response.GetResponseStream();

                reader = new StreamReader(resStream ?? throw new NullReferenceException());
                responseStr = await reader.ReadToEndAsync();

                dynamic weather_json = JsonConvert.DeserializeObject(responseStr);

                await bot.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text:
                          string.Format(Program.strManager["WEATHER_MESSAGE"],
                          locationJson[0].LocalizedName, locationJson[0].Country.LocalizedName, weather_json[0].WeatherText, weather_json[0].Temperature.Metric.Value,
                          weather_json[0].RealFeelTemperature.Metric.Value, weather_json[0].RelativeHumidity, weather_json[0].Wind.Direction.Localized, weather_json[0].Wind.Speed.Metric.Value),
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
                    var reader = new StreamReader(resStream ?? throw new NullReferenceException());
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
