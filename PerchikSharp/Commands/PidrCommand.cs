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
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            var msg = command.Message;
            using(var db = Db.PerchikDB.GetContext())
            {
                var pidr =
                    db.Pidrs
                    .AsNoTracking()
                    .Where(p => p.UserId == msg.From.Id && p.ChatId == msg.Chat.Id && p.Date.Date == DateTime.Now.Date)
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
                       text: "*Собираю секретные данные о пользователях... 🧐*",
                       parseMode: ParseMode.Markdown);

                    await bot.SendChatActionAsync(msg.Chat.Id, ChatAction.Typing);
                    await Task.Delay(3000);

                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: "*Барабанная дробь... 🥁🥁*",
                       parseMode: ParseMode.Markdown);

                    await bot.SendChatActionAsync(msg.Chat.Id, ChatAction.Typing);
                    await Task.Delay(5000);

                    long lastday = DbConverter.ToEpochTime(DateTime.Now.AddDays(-1).Date);
                    var users =
                        db.Users
                        .AsNoTracking()
                        .Where(u => u.Messages.Any(m => m.Date > lastday))
                        .Select(x => new
                        {
                            x.Id,
                            x.FirstName
                        }).ToList();

                    var new_pidr = users[new Random(DateTime.Now.Second).Next(0, users.Count)];

                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: $"*Наш пидр на сегодня - *[{new_pidr.FirstName}](tg://user?id={new_pidr.Id} 🥳",
                       parseMode: ParseMode.Markdown);

                    db.Pidrs.Add(new Db.Tables.Pidr()
                    {
                        UserId = new_pidr.Id,
                        ChatId = msg.Chat.Id,
                        Date = DateTime.Now
                    });
                    db.SaveChanges();
                }
                else
                {
                    await bot.SendTextMessageAsync(
                       chatId: msg.Chat.Id,
                       text: $"*Пидр был определен и сегодня это - *[{pidr.FirstName}](tg://user?id={pidr.UserId}) 💩",
                       parseMode: ParseMode.Markdown);
                }
            }
            
        }
    }
}
