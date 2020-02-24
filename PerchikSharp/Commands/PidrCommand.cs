using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PidrCommand : INativeCommand
    {
        public string Command { get { return "pidr"; } }
        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;
            var msg = command.Message;
            using(var db = Db.PerchikDB.GetContext())
            {
                var pidr =
                    db.Pidrs
                    .AsNoTracking()
                    .Where(p => p.ChatId == msg.Chat.Id && p.Date.Date == DbConverter.DateTimeUTC2.Date)
                    .Select(x => new
                    {
                        x.UserId,
                        x.User.FirstName
                    })
                    .FirstOrDefault();


                if(pidr == null)
                {
                    await bot.SendChatActionAsync(msg.Chat.Id, ChatAction.Typing);
                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: Program.strManager["PIDR_ONE"],
                       parseMode: ParseMode.Markdown);
                    await bot.SendChatActionAsync(msg.Chat.Id, ChatAction.Typing);

                    await Task.Delay(2000);
                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: Program.strManager["PIDR_TWO"],
                       parseMode: ParseMode.Markdown);

                    await bot.SendChatActionAsync(msg.Chat.Id, ChatAction.Typing);
                    await Task.Delay(1000);

                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: Program.strManager["PIDR_THREE"],
                       parseMode: ParseMode.Markdown);

                    await bot.SendChatActionAsync(msg.Chat.Id, ChatAction.Typing);
                    await Task.Delay(5000);

                    long lastday = DbConverter.ToEpochTime(DateTime.UtcNow.AddDays(-1).Date);
                    var users =
                        db.Users
                        .AsNoTracking()
                        .Where(u => u.Messages.Any(m => m.Date > lastday))
                        .Select(x => new
                        {
                            x.Id,
                            x.FirstName
                        }).ToList();

                    var new_pidr = users[new Random(DbConverter.DateTimeUTC2.Second).Next(0, users.Count)];

                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: string.Format(Program.strManager["PIDR_DONE"], new_pidr.FirstName, new_pidr.Id),
                       parseMode: ParseMode.Markdown);

                    db.Pidrs.Add(new Db.Tables.Pidr()
                    {
                        UserId = new_pidr.Id,
                        ChatId = msg.Chat.Id,
                        Date = DbConverter.DateTimeUTC2
                    });
                    db.SaveChanges();
                }
                else
                {
                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: string.Format(Program.strManager["PIDR_EXIST"], pidr.FirstName),
                       parseMode: ParseMode.Markdown);
                }
            }
            
        }
    }
}
