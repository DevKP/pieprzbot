using PerchikSharp.Db;
using System.Diagnostics;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class TopBansCommand : INativeCommand
    {
        public string Command => "topbans";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await using var db = PerchikDB.GetContext();
            var message = command.Message;

            var users = db.Users
                //.Include(x => x.Restrictions)
                .OrderByDescending(x => x.Restrictions.Count)
                .Take(10)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Restrictions.Count
                })
                .ToList();

            var msgString = "*Топ 10 по банам:*\n";
            int i = 1;
            foreach (var user in users)
            {
                var firstName = user.FirstName?.Replace('[', '<').Replace(']', '>');
                var lastName = user.LastName?.Replace('[', '<').Replace(']', '>');
                //string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);
                var fullName = $"`{firstName} {lastName}`";

                msgString += $"{i++}. {fullName} -- {user.Count}\n";
            }

            stopwatch.Stop();

            await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"{msgString}\n`{stopwatch.ElapsedMilliseconds / 1000.0}сек`",
                parseMode: ParseMode.Markdown);
        }
    }
}
