using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PromoteCommand : INativeCommand
    {
        const long OfftopiaId = -1001125742098;
        const int ViaTcpId = 204678400;

        public string Command => "promote";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            if (command.Message.Chat.Type == ChatType.Private)
                return;

            var bot = sender as Pieprz;
            try
            {
                if (command.Message.From.Id == ViaTcpId)
                {
                    await bot.PromoteChatMemberAsync(command.Message.Chat.Id, ViaTcpId, true, false, false, true, true, true, true, true);
                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
