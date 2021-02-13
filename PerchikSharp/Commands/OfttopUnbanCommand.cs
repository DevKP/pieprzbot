using PerchikSharp.Db;
using System;
using PerchikSharp.Events;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class OfftopUnbanCommand : INativeCommand
    {
        const long OfftopiaId = -1001125742098;
        const int ViaTcpId = 204678400;

        public string Command => "offtopunban";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            if (command.Message.Chat.Type != ChatType.Private)
                return;

            try
            {
                var bot = sender as Pieprz;

                var permissions = new ChatPermissions
                {
                    CanAddWebPagePreviews = true,
                    CanChangeInfo = true,
                    CanInviteUsers = true,
                    CanPinMessages = true,
                    CanSendMediaMessages = true,
                    CanSendMessages = true,
                    CanSendOtherMessages = true,
                    CanSendPolls = true
                };

                await bot.RestrictChatMemberAsync(
                    chatId: OfftopiaId,
                    userId: command.Message.From.Id,
                    untilDate: DbConverter.DateTimeUtc2.AddSeconds(40),
                    permissions: permissions);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
