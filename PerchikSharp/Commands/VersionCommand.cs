using PerchikSharp;
using PerchikSharp.Commands;
using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class VersionCommand : INativeCommand
    {
        public string Command { get { return "version"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            try
            {
                await bot.SendTextMessageAsync(
                       chatId: command.Message.Chat.Id,
                       text: $"*Version: {Pieprz.BotVersion}*",
                       parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
