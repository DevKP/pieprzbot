using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class RoulletteCommand : IRegExCommand
    {
        public string RegEx { get { return @"\bрулетк[уа]?\b"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
            Message message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;

            try
            {
                Random rand = new Random(DateTime.Now.Millisecond);
                if (rand.Next(0, 6) == 3)
                {
                    var until = DateTime.Now.AddSeconds(10 * 60); //10 minutes
                    await Pieprz.RestrictUserAsync(message.Chat.Id, message.From.Id, until);


                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(Program.strManager.GetRandom("ROULETTEBAN"), Pieprz.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    var msg = bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(Program.strManager.GetRandom("ROULETTEMISS"), Pieprz.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown).Result;

                    Thread.Sleep(10 * 1000); //wait 10 seconds

                    await bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: msg.MessageId);
                    await bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
