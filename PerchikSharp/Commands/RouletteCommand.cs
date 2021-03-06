﻿using PerchikSharp.Db;
using System;
using System.Threading;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class RoulletteCommand : IRegExCommand
    {
        public string RegEx => @"\bрулетк[уа]?\b";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;

            try
            {
                var rand = new Random(DbConverter.DateTimeUtc2.Millisecond);
                if (rand.Next(0, 6) == 3)
                {
                    var until = DbConverter.DateTimeUtc2.AddSeconds(10 * 60); //10 minutes
                    await bot.RestrictUserAsync(message.Chat.Id, message.From.Id, until);


                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(Program.strManager.GetRandom("ROULETTEBAN"), bot.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    var msg = bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(Program.strManager.GetRandom("ROULETTEMISS"), bot.MakeUserLink(message.From)),
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
