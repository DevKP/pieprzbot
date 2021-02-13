using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Diagnostics;
using System.Linq;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PerchikSharp.Commands
{
    class StatisticsCommand : IRegExCommand
    {
        public string RegEx => @"инфо\s?(?<name>[\w\W\s]+)?";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            try
            {
                var message = command.Message;
                var name = command.Match.Groups["name"]?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    var targetMessage = message.ReplyToMessage ?? message;
                    name = targetMessage.From.Username ?? targetMessage.From.FirstName;
                }

                var updateButton = new InlineKeyboardButton
                {
                    CallbackData = "stats-" + Guid.NewGuid().ToString("n").Substring(0, 8),
                    Text = Program.strManager["RATE_UPDATE_BTN"]
                };

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { updateButton } });

                string text;
                try
                {
                    text = GetStatisticsText(name);
                }
                catch (Exception ex)
                {
                    inlineKeyboard = null;
                    text = ex.Message;
                }

                Logger.Log(LogType.Info, $"User {message.From.FirstName}:{message.From.Id} created info message with Data: {updateButton.CallbackData}");

                var msg = await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: text,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.Markdown);

                var botHelper = (sender as Pieprz);
                botHelper?.RegisterCallbackQuery(updateButton.CallbackData, 0, name, async (_, o) =>
                {
                    string newText;
                    try
                    {
                        newText = GetStatisticsText(o.Obj as string);
                    }
                    catch (Exception ex)
                    {
                        newText = ex.Message;
                    }

                    try
                    {
                        await bot.EditMessageTextAsync(
                            chatId: msg.Chat.Id,
                            messageId: o.Callback.Message.MessageId,
                            replyMarkup: inlineKeyboard,
                            text: newText,
                            parseMode: ParseMode.Markdown);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                });
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static string GetStatisticsText(string search)
        {
            var sw = new Stopwatch();
            sw.Start();
            using var db = PerchikDB.GetContext();
            var name = search.Replace("@", "");

            var today = DbConverter.ToEpochTime(DateTime.UtcNow.Date);
            var lastday = DbConverter.ToEpochTime(DateTime.UtcNow.AddDays(-1).Date);

                

            var user = db.Users
                .AsNoTracking()
                .Where(u =>
                    (u.FirstName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                    (u.LastName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                    (u.UserName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                .Select(x => new
                {
                    x.Id,
                    x.Restricted,
                    x.Description,
                    x.FirstName,
                    x.LastName,
                    x.UserName,
                    x.Restrictions.OrderByDescending(x => x.Until).FirstOrDefault().Until,
                    msgLastday = x.Messages.Count(m => m.Date > lastday && m.Date < today),
                    msgToday = x.Messages.Count(m => m.Date > today),
                    msgTotal = x.Messages.Count,
                    RestrictionCount = x.Restrictions.Count,
                    activity = x.Messages.Where(m => m.Date > today).Sum(m => m.Text.Length) /
                               (double)db.Messages.Where(m => m.Date > today).Sum(m => m.Text.Length)
                })
                .FirstOrDefault();

            if (user == null)
            {
                throw new Exception($"*Пользователя \"{search}\" нет в базе.*");
            }

            var remaining = new TimeSpan(0);
            if (user.Restricted)
            {
                remaining = user.Until - DateTime.UtcNow;
            }


            sw.Stop();
            return $"*Имя: {user.FirstName} {user.LastName}\n*" +
                   $"*ID: {user.Id}\n*" +
                   $"*Ник:  {user.UserName}*\n\n" +
                   string.Format("*Активность:* {0:F2}%\n", user.activity * 100) +
                   $"*Сообщений сегодня:*  { user.msgToday }\n" +
                   $"*Сообщений вчера:* { user.msgLastday }\n" +
                   $"*Всего сообщений:* { user.msgTotal }\n" +
                   $"*Банов:* { user.RestrictionCount }\n\n" +
                   (user.Description != null ? $"*О себе:* \n_{ user.Description }_\n\n" : "") +
                   (remaining.Ticks != 0 ? $"💢`Сейчас забанен, осталось: { $"{remaining:hh\\:mm\\:ss}`\n" }" : "") +
                   $"`{sw.ElapsedMilliseconds / 1000.0}сек`";
        }
    }
}
