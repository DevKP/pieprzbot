using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PerchikSharp.Commands
{
    class StatisticsCommand : IRegExCommand
    {
        public string RegEx { get { return @"инфо\s?(?<name>[\w\W\s]+)?"; } }

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            try
            {
                Message message = command.Message;
                string name = command.Match.Groups["name"]?.Value;
                if (name == null || name.Length == 0)
                {
                    if (message.ReplyToMessage == null)
                    {
                        name = message.From.Username ?? name;//Can be null

                        name = message.From.FirstName ?? name;//But FirstName can't
                    }
                    else
                    {
                        name = message.ReplyToMessage.From.Username ?? name;//Can be null

                        name = message.ReplyToMessage.From.FirstName ?? name;//But FirstName can't
                    }

                }                                         // Last name isn't required, this will be unreachable code

                var update_button = new InlineKeyboardButton();
                update_button.CallbackData = "stats-" + Guid.NewGuid().ToString("n").Substring(0, 8);
                update_button.Text = Program.strManager["RATE_UPDATE_BTN"];

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { update_button } });

                string text = string.Empty;
                try
                {
                    text = getStatisticsText(name);
                }
                catch (Exception ex)
                {
                    inlineKeyboard = null;
                    text = ex.Message;
                }

                Logger.Log(LogType.Info, $"User {message.From.FirstName}:{message.From.Id} created info message with Data: {update_button.CallbackData}");

                Message msg = await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: text,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.Markdown);

                Pieprz botHelper = (sender as Pieprz);
                botHelper.RegisterCallbackQuery(update_button.CallbackData, 0, name, async (_, o) =>
                {
                    string new_text = string.Empty;
                    try
                    {
                        new_text = getStatisticsText(o.obj as string);

                    }
                    catch (Exception ex)
                    {
                        new_text = ex.Message;
                    }

                    try
                    {
                        await bot.EditMessageTextAsync(
                            chatId: msg.Chat.Id,
                            messageId: o.Callback.Message.MessageId,
                            replyMarkup: inlineKeyboard,
                            text: new_text,
                            parseMode: ParseMode.Markdown);
                    }
                    catch (Exception)
                    {

                    }
                });
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static string getStatisticsText(string search)
        {
            var sw = new Stopwatch();
            sw.Start();
            using (var db = PerchikDB.GetContext())
            {
                string name = search.Replace("@", "");

                long today = DbConverter.ToEpochTime(DateTime.UtcNow.Date);
                long lastday = DbConverter.ToEpochTime(DateTime.UtcNow.AddDays(-1).Date);

                

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
                        msgLastday = x.Messages.Where(m => m.Date > lastday && m.Date < today).Count(),
                        msgToday = x.Messages.Where(m => m.Date > today).Count(),
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

                TimeSpan remaining = new TimeSpan(0);
                if (user.Restricted)
                {
                    remaining = user.Until - DbConverter.DateTimeUTC2;
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
                            (user.Description != null ? $"*О себе:* \n{ user.Description }\n\n" : "") +
                            (remaining.Ticks != 0 ? $"💢`Сейчас забанен, осталось: { $"{remaining:hh\\:mm\\:ss}`\n" }" : "") +
                            $"`{sw.ElapsedMilliseconds / 1000.0}сек`";
            }
        }
    }
}
