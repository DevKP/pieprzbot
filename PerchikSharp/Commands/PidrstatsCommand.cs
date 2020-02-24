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
        public string Command { get { return "pidrstats"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            var msg = command.Message;

            using (var db = PerchikDB.GetContext())
            {
                Message message = command.Message;

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
                string msg_string = "*Топ космо-пидоров:*\n";
                for (int i = 0; i < pidrs.Count; i++)
                {
                    //var user = users.ElementAt(i);
                    string first_name = pidrs[i].FirstName.Replace('[', '<').Replace(']', '>');
                    msg_string += string.Format("{0}. {1} — {2} раз.\n", i + 1, first_name, pidrs[i].Count);
                }

                await bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: msg_string,
                                parseMode: ParseMode.Markdown);
            }
        }
    }
}
