using PerchikSharp.Db;
using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class OfftopUnbanCommand : INativeCommand
    {
        const long offtopia_id = -1001125742098;
        const int via_tcp_Id = 204678400;

        public string Command { get { return "offtopunban"; } }
        public async void OnExecution(object sender, CommandEventArgs command)
        {
            if (command.Message.Chat.Type != ChatType.Private)
                return;

            try
            {
                var bot = sender as Pieprz;

                ChatPermissions permissions = new ChatPermissions();
                permissions.CanAddWebPagePreviews = true;
                permissions.CanChangeInfo = true;
                permissions.CanInviteUsers = true;
                permissions.CanPinMessages = true;
                permissions.CanSendMediaMessages = true;
                permissions.CanSendMessages = true;
                permissions.CanSendOtherMessages = true;
                permissions.CanSendPolls = true;

                await bot.RestrictChatMemberAsync(
                    chatId: offtopia_id,
                    userId: command.Message.From.Id,
                    untilDate: DbConverter.DateTimeUTC2.AddSeconds(40),
                    permissions: permissions);
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
