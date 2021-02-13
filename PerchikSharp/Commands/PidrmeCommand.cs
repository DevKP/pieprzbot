using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Linq;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PidrmeCommand : INativeCommand
    {
        public string Command => "pidrme";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            try
            {
                var bot = sender as Pieprz;
                var msg = command.Message;
                var user = msg.From;
                var chat = msg.Chat;

                await using var db = PerchikDB.GetContext();
                var number = db.Pidrs
                    .AsNoTracking()
                    .Count(p => p.UserId == user.Id && p.ChatId == chat.Id);

                await bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: string.Format(Program.strManager["PIDR_DAY"], user.FirstName, user.Id, number),
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
