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
        public string Command { get { return "rate"; } }
        public async void OnExecution(object sender, CommandEventArgs command)
        {
            CallbackQuery cq;
            var bot = sender as Pieprz;
            try
            {
                var msg = await bot.SendTextMessageAsync(
                              chatId: command.Message.Chat.Id,
                              text: "*Обновление...*",
                              parseMode: ParseMode.Markdown);

                cq = new CallbackQuery();
                cq.Message = msg;
                cq.InlineMessageId = msg.MessageId.ToString();
                cq.From = msg.From;

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
                    formated_str += $"\nОбновлено {DbConverter.DateTimeUTC2.ToShortTimeString()}";


                    var button = new InlineKeyboardButton();
                    button.CallbackData = "update_rate";
                    button.Text = Program.strManager.GetSingle("RATE_UPDATE_BTN");
                    var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

                    await Program.bot.EditMessageTextAsync(
                         chatId: e.Callback.Message.Chat.Id,
                         messageId: e.Callback.Message.MessageId,
                         replyMarkup: inlineKeyboard,
                         text: formated_str);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
