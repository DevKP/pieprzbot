using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PidrstatsCommand : INativeCommand
    {
        public string Command => "pidrstats";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var msg = command.Message;
            var bot = sender as Pieprz;

            await using var db = PerchikDB.GetContext();
            var message = command.Message;

            var pidrs = db.Users
                .AsNoTracking()
                .Where(p => p.Pidrs.Count > 0)
                .Select(x => new
                {
                    x.FirstName,
                    x.Pidrs.Count
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            //var usersDescending = users.OrderByDescending(x => x.activity);
            var msgString = "*Топ космо-пидоров:*\n";
            for (var i = 0; i < pidrs.Count; i++)
            {
                //var user = users.ElementAt(i);
                var firstName = pidrs[i].FirstName.Replace('[', '<').Replace(']', '>');
                msgString += $"{i + 1}. {firstName} — {pidrs[i].Count} раз.\n";
            }

            await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: msgString,
                parseMode: ParseMode.Markdown);
        }
    }
}
