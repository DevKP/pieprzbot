using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PromoteCommand : INativeCommand
    {
        const long offtopia_id = -1001125742098;
        const int via_tcp_Id = 204678400;

        public string Command { get { return "promote"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            if (command.Message.Chat.Type == ChatType.Private)
                return;

            try
            {
                if (command.Message.From.Id == via_tcp_Id)
                {
                    await bot.PromoteChatMemberAsync(command.Message.Chat.Id, via_tcp_Id, true, false, false, true, true, true, true, true);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
