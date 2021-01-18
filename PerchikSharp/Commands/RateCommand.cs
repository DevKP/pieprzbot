using Newtonsoft.Json;
using PerchikSharp.Db;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PerchikSharp.Commands
{
    class RateCommand : INativeCommand
    {
        public string Command => "rate";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;
            try
            {
                var msg = await bot.SendTextMessageAsync(
                              chatId: command.Message.Chat.Id,
                              text: "*Обновление...*",
                              parseMode: ParseMode.Markdown);

                var cq = new CallbackQuery
                {
                    Message = msg,
                    InlineMessageId = msg.MessageId.ToString(),
                    From = msg.From
                };

                onRateUpdate(sender, new CallbackQueryArgs(cq));
                (sender as Pieprz).CallbackQuery("update_rate", this.onRateUpdate);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }

        }

        private async void onRateUpdate(object sender, CallbackQueryArgs e)
        {
            try
            {
                var url = "https://min-api.cryptocompare.com/data/pricemultifull";

                using var client = new HttpClient { BaseAddress = new Uri(url) };
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var uriBuilder = new UriBuilder(url);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                query["fsyms"] = "BTC,ETH,ETC,ZEC,LTC,BCH";
                query["tsyms"] = "USD";

                uriBuilder.Query = query.ToString() ?? string.Empty;
                url = uriBuilder.ToString();


                var responseMessage = client.GetAsync(url).Result;
                var responseJson = responseMessage.Content.ReadAsStringAsync().Result;

                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>>(responseJson);

                var templateStr = "1 {0} = {1}$ ({2:f2}% / 24h){3}\n";
                var formatedStr = "";

                foreach (var (CURRENCY_SYMBOL, _) in jsonObject["RAW"])
                {
                    float CHANGEPCT24HOUR = jsonObject["RAW"][CURRENCY_SYMBOL]["USD"]["CHANGEPCT24HOUR"];
                    float PRICE = jsonObject["RAW"][CURRENCY_SYMBOL]["USD"]["PRICE"];


                    string symbol = "💹";
                    if (CHANGEPCT24HOUR < 0)
                        symbol = "🔻";

                    formatedStr += String.Format(templateStr, CURRENCY_SYMBOL, PRICE, CHANGEPCT24HOUR, symbol);
                }
                formatedStr += $"\nОбновлено {DbConverter.DateTimeUtc2.ToShortTimeString()}";


                var button = new InlineKeyboardButton
                {
                    CallbackData = "update_rate", 
                    Text = Program.strManager.GetSingle("RATE_UPDATE_BTN")
                };
                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

                await Program.bot.EditMessageTextAsync(
                    chatId: e.Callback.Message.Chat.Id,
                    messageId: e.Callback.Message.MessageId,
                    replyMarkup: inlineKeyboard,
                    text: formatedStr);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
