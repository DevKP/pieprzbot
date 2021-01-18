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
        public string RegEx => @"\bра[зс]бань?\b";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;
            if (!bot.IsUserAdmin(message.Chat.Id, message.From.Id))
                return;
            if (message.ReplyToMessage == null)
                return;

            try
            {
                var until = DbConverter.DateTimeUtc2.AddSeconds(1);
                await bot.RestrictUserAsync(message.Chat.Id, message.ReplyToMessage.From.Id, until, true);

                await using (var db = PerchikDB.GetContext())
                {
                    var existingUser = db.Users
                        .FirstOrDefault(x => x.Id == message.ReplyToMessage.From.Id);

                    if (existingUser != null)
                    {
                        existingUser.Restricted = false;
                        await db.SaveChangesAsync();
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
