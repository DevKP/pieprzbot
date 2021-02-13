using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Diagnostics;
using System.Linq;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class TopCommand : INativeCommand
    {
        public string Command => "top";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await using var db = PerchikDB.GetContext();
            var message = command.Message;

            var dateNow = DbConverter.ToEpochTime(DateTime.UtcNow.Date);
            var now = DbConverter.ToEpochTime(DateTime.UtcNow);

            var users = db.Users
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    activity = x.Messages.Where(m => m.Date > dateNow).Sum(m => m.Text.Length) /
                               (double)db.Messages.Where(m => m.Date > dateNow).Sum(m => m.Text.Length)
                })
                .ToList();

            var usersDescending = users.OrderByDescending(x => x.activity);
            var msgString = "*Топ 10 по активности за сегодня:*\n";
            for (var i = 0; i < 10 && i < users.Count; i++)
            {
                var user = usersDescending?.ElementAt(i);

                if (Math.Abs(user.activity) < 0.01)
                    continue;

                var firstName = user.FirstName?.Replace('[', '<').Replace(']', '>');
                var lastName = user.LastName?.Replace('[', '<').Replace(']', '>');
                var fullName = $"`{firstName} {lastName}`";

                msgString += $"{i + 1}. {fullName} -- {user.activity * 100:F2}%\n";
            }

            stopwatch.Stop();

            await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"{msgString}\n`{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                parseMode: ParseMode.Markdown);
        }
    }
}
