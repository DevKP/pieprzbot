using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PidrmeCommand : INativeCommand
    {
        public string Command { get { return "pidrme"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            try
            {
                var msg = command.Message;
                var user = msg.From;
                var chat = msg.Chat;

                using (var db = PerchikDB.GetContext())
                {
                    int number = db.Pidrs
                        .AsNoTracking()
                        .Where(p => p.UserId == user.Id && p.ChatId == chat.Id)
                        .Count();

                    await bot.SendTextMessageAsync(
                           chatId: msg.Chat.Id,
                           text: string.Format(Program.strManager["PIDR_DAY"], user.FirstName, user.Id, number),
                           parseMode: ParseMode.Markdown);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
