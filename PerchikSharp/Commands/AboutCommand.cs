using PerchikSharp.Db;
using System;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class AboutCommand : INativeCommand
    {
        public string Command => @"about";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            try
            {
                var bot = sender as Pieprz;
                if (string.IsNullOrEmpty(command.Text))
                {
                    if (bot != null)
                        await bot.SendTextMessageAsync(
                            chatId: command.Message.Chat.Id,
                            text: StringManager.FromFile("aboutusage.txt"),
                            parseMode: ParseMode.Markdown);
                    return;
                }

                var msg = command.Message;
                await using var db = PerchikDB.GetContext();
                await db.UpsertUser(DbConverter.GenUser(msg.From, command.Text), msg.Chat.Id);
                

                if (bot != null)
                    await bot.SendTextMessageAsync(
                        chatId: command.Message.Chat.Id,
                        replyToMessageId: command.Message.MessageId,
                        text: "*Описание обновлено.*",
                        parseMode: ParseMode.Markdown);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
