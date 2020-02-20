using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Diagnostics;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class TopCommand : INativeCommand
    {
        public string Command { get { return "top"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var db = PerchikDB.GetContext())
            {
                Message message = command.Message;

                long datenow = DbConverter.ToEpochTime(DateTime.Now.Date);
                long now = DbConverter.ToEpochTime(DateTime.Now);

                var users = db.Users
                    .AsNoTracking()
                    .Select(x => new
                    {
                        x.Id,
                        x.FirstName,
                        x.LastName,
                        activity = x.Messages.Where(m => m.Date > datenow).Sum(m => m.Text.Length) /
                                   (double)db.Messages.Where(m => m.Date > datenow).Sum(m => m.Text.Length)
                    })
                    .ToList();

                var usersDescending = users.OrderByDescending(x => x.activity);
                string msg_string = "*Топ 10 по активности за сегодня:*\n";
                for (int i = 0; i < 10 && i < users.Count; i++)
                {
                    var user = usersDescending.ElementAt(i);

                    if (user.activity == 0)
                        continue;

                    //var user = users.ElementAt(i);
                    string first_name = user.FirstName?.Replace('[', '<').Replace(']', '>');
                    string last_name = user.LastName?.Replace('[', '<').Replace(']', '>');
                    //string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);
                    string full_name = string.Format("`{0} {1}`", first_name, last_name);

                    msg_string += string.Format("{0}. {1} -- {2:F2}%\n", i + 1, full_name, user.activity * 100);
                }

                stopwatch.Stop();

                await bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: $"{msg_string}\n`{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                                parseMode: ParseMode.Markdown);
            }
        }
    }
}
