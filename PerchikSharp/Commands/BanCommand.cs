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
    class BanCommand : IRegExCommand
    {
        public string RegEx { get { return @"(?<ban>\b(за)?бань?\b)\s?(?<number>\d{1,9})?\s?(?<letter>[смчд](\w+)?)?\s?(?<comment>[\w\W\s]+)?"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
            Message message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;


            const int default_second = 40;
            int seconds = default_second;
            int number = default_second;
            string word = "сек.";
            string comment = "...";

            if (command.Match.Success)
            {
                if (command.Match.Groups["number"].Value != string.Empty)
                {
                    number = int.Parse(command.Match.Groups["number"].Value);
                    seconds = number;
                }

                if (command.Match.Groups["letter"].Value != string.Empty)
                {
                    switch (command.Match.Groups["letter"].Value.First())
                    {
                        case 'с':
                            seconds = number;
                            word = "сек.";
                            break;
                        case 'м':
                            seconds *= 60;
                            word = "мин.";
                            break;
                        case 'ч':
                            word = "ч.";
                            seconds *= 3600;
                            break;
                        case 'д':
                            word = "д.";
                            seconds *= 86400;
                            break;
                    }
                }

                if (command.Match.Groups["comment"].Value != string.Empty)
                {
                    comment = command.Match.Groups["comment"].Value;
                }
            }

            try
            {
                if (message.ReplyToMessage != null)
                {
                    if (!BotHelper.isUserAdmin(message.Chat.Id, message.From.Id))
                        return;

                    if (message.ReplyToMessage.From.Id == bot.BotId)
                        return;

                    await (sender as BotHelper).FullyRestrictUserAsync(
                            chatId: message.Chat.Id,
                            userId: message.ReplyToMessage.From.Id,
                            forSeconds: seconds);

                    if (seconds >= default_second)
                    {
                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(Program.strManager.GetSingle("BANNED"), BotHelper.MakeUserLink(message.ReplyToMessage.From), number, word, comment, BotHelper.MakeUserLink(message.From)),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        seconds = int.MaxValue;
                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(Program.strManager.GetSingle("SELF_PERMANENT"), BotHelper.MakeUserLink(message.ReplyToMessage.From), number, word, comment),
                            parseMode: ParseMode.Markdown);
                    }

                    using (var db = PerchikDB.GetContext())
                    {
                        var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.Now.AddSeconds(seconds));
                        db.AddRestriction(restriction);
                    }

                    await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
                else
                {
                    if (seconds >= default_second)
                    {
                        await (sender as BotHelper).FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                forSeconds: seconds);

                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(Program.strManager.GetSingle("SELF_BANNED"), BotHelper.MakeUserLink(message.From), number, word, comment),
                            parseMode: ParseMode.Markdown);

                        using (var db = PerchikDB.GetContext())
                        {
                            var restriction = DbConverter.GenRestriction(message, DateTime.Now.AddSeconds(seconds));
                            db.AddRestriction(restriction);
                        }
                    }
                    else
                    {
                        await (sender as BotHelper).FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id);

                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(Program.strManager.GetSingle("SELF_BANNED"), BotHelper.MakeUserLink(message.From), 40, word, comment),
                            parseMode: ParseMode.Markdown);

                        using (var db = PerchikDB.GetContext())
                        {
                            var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.Now.AddSeconds(40));
                            db.AddRestriction(restriction);
                        }
                    }

                    await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
