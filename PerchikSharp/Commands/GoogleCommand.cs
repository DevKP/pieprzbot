using System;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class GoogleCommand : INativeCommand
    {
        public string Command => "google";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            try
            {
                if (command.Text == string.Empty)
                    return;

                var bot = sender as Pieprz;
                var text = command.Text;

                var url = Uri.EscapeUriString($"https://ru.lmgtfy.com/?q={text}")
                    .Replace('[', '<')
                    .Replace(']', '>');

                await bot.SendTextMessageAsync(
                                    chatId: command.Message.Chat.Id,
                                    replyToMessageId: command.Message.MessageId,
                                    text: $"[Загуглить]({url})",
                                    parseMode: ParseMode.Markdown);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
