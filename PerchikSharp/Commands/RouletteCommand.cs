﻿using PerchikSharp.Db;
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
        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            Message message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;

            try
            {
                Random rand = new Random(DbConverter.DateTimeUTC2.Millisecond);
                if (rand.Next(0, 6) == 3)
                {
                    var until = DbConverter.DateTimeUTC2.AddSeconds(10 * 60); //10 minutes
                    await bot.RestrictUserAsync(message.Chat.Id, message.From.Id, until);


                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(Program.strManager.GetRandom("ROULETTEBAN"), bot.MakeUserLink(message.From)),
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    var msg = bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(Program.strManager.GetRandom("ROULETTEMISS"), bot.MakeUserLink(message.From)),
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
