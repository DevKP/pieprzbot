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
        const int ViaTcpId = 204678400;
        public string RegEx => @"(?<ban>\b(за)?бань?\b)\s?(?<number>\d{1,9})?\s?(?<letter>[смчд](\w+)?)?\s?(?<comment>[\w\W\s]+)?";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;


            const int defaultSecond = 40;
            var seconds = defaultSecond;
            var number = defaultSecond;
            var word = "сек.";
            var comment = "...";

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
                    if (!bot.IsUserAdmin(message.Chat.Id, message.From.Id) 
                        && message.From.Id != ViaTcpId)
                        return;

                    if (message.ReplyToMessage.From.Id == bot.BotId)
                        return;

                    await (sender as Pieprz).FullyRestrictUserAsync(
                            chatId: message.Chat.Id,
                            userId: message.ReplyToMessage.From.Id,
                            forSeconds: seconds);

                    if (seconds >= defaultSecond)
                    {
                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(Program.strManager.GetSingle("BANNED"), bot.MakeUserLink(message.ReplyToMessage.From), number, word, comment, bot.MakeUserLink(message.From)),
                            parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        seconds = int.MaxValue;
                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: string.Format(Program.strManager.GetSingle("SELF_PERMANENT"), bot.MakeUserLink(message.ReplyToMessage.From), number, word, comment),
                            parseMode: ParseMode.Markdown);
                    }

                    await using (var db = PerchikDB.GetContext())
                    {
                        var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.UtcNow.AddSeconds(seconds));
                        db.AddRestriction(restriction);
                    }

                    await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
                else
                {
                    if (seconds >= defaultSecond)
                    {
                        await (sender as Pieprz).FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                forSeconds: seconds);

                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            string.Format(Program.strManager.GetSingle("SELF_BANNED"), bot.MakeUserLink(message.From), number, word, comment),
                            parseMode: ParseMode.Markdown);

                        await using var db = PerchikDB.GetContext();
                        var restriction = DbConverter.GenRestriction(message, DateTime.UtcNow.AddSeconds(seconds));
                        db.AddRestriction(restriction);
                    }
                    else
                    {
                        await (sender as Pieprz).FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id);

                        await bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: String.Format(Program.strManager.GetSingle("SELF_BANNED"), bot.MakeUserLink(message.From), 40, word, comment),
                            parseMode: ParseMode.Markdown);

                        await using var db = PerchikDB.GetContext();
                        var restriction = DbConverter.GenRestriction(message.ReplyToMessage, DateTime.UtcNow.AddSeconds(40));
                        db.AddRestriction(restriction);
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
