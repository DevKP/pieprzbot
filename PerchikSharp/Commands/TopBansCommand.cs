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
        public string Command { get { return "topbans"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var db = PerchikDB.Context)
            {
                Message message = command.Message;

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

                string msg_string = "*Топ 10 по банам:*\n";
                int i = 1;
                foreach (var user in users)
                {
                    string first_name = user.FirstName?.Replace('[', '<').Replace(']', '>');
                    string last_name = user.LastName?.Replace('[', '<').Replace(']', '>');
                    string full_name = string.Format("[{0} {1}](tg://user?id={2})", first_name, last_name, user.Id);
                    msg_string += $"{i++}. {full_name} -- {user.Count}\n";
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
