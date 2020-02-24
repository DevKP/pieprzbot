using PerchikSharp.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class UnbanCommand : IRegExCommand
    {
        public string RegEx { get { return @"\bра[зс]бань?\b"; } }
        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            Message message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;
            if (!bot.isUserAdmin(message.Chat.Id, message.From.Id))
                return;
            if (message.ReplyToMessage == null)
                return;

            try
            {
                var until = DbConverter.DateTimeUTC2.AddSeconds(1);
                await bot.RestrictUserAsync(message.Chat.Id, message.ReplyToMessage.From.Id, until, true);

                using (var db = PerchikDB.GetContext())
                {
                    var existingUser = db.Users
                        .Where(x => x.Id == message.ReplyToMessage.From.Id)
                        .FirstOrDefault();

                    if (existingUser != null)
                    {
                        existingUser.Restricted = false;
                        db.SaveChanges();
                    }

                }

                await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(Program.strManager.GetRandom("UNBANNED"), bot.MakeUserLink(message.ReplyToMessage.From)),
                        parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
