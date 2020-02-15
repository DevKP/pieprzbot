using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class MeCommand : INativeCommand
    {
        public string Command { get { return "me"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            if (command.Text == "")
                return;

            Message message = command.Message;
            string msg_text = $"{BotHelper.MakeUserLink(message.From)} *{command.Text}*";

            try
            {
                await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                if (message.ReplyToMessage != null)
                {
                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: msg_text,
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: message.ReplyToMessage.MessageId);
                }
                else
                {
                    await bot.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: msg_text,
                       parseMode: ParseMode.Markdown);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}\nTrace:{e.StackTrace}");
            }
        }
    }
}
