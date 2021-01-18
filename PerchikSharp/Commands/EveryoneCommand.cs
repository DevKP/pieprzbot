using PerchikSharp.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class EveryoneCommand : INativeCommand
    {
        public string Command => "everyone";

        public void OnExecution(object sender, CommandEventArgs command)
        {
            try
            {
                var bot = sender as Pieprz;
                var message = command.Message;

                if (message.Chat.Type == ChatType.Private)
                    return;

                int[] usersWhitelist = { 204678400
                                         /*тут огурец*/ };
                if (!bot.IsUserAdmin(message.Chat.Id, message.From.Id) &&
                    usersWhitelist.All(id => id != message.From.Id))
                    return;


                using var db = PerchikDB.GetContext();
                var users = db.Users.ToList();
                var messageStr = string.Empty;

                const int maxUsersInMessage = 10;
                var sendedMessages = new List<Message>();

                int i = 0;
                foreach (var user in users)
                {
                    var firstname = user.FirstName.Replace('[', '<').Replace(']', '>');
                    messageStr += $"[{firstname}](tg://user?id={user.Id})\n";
                    if (i % maxUsersInMessage == 0 || i == users.Count() - 1)
                    {
                        var msg = bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: messageStr,
                            parseMode: ParseMode.Markdown).Result;
                        sendedMessages.Add(msg);
                        messageStr = string.Empty;
                    }

                    i++;
                }

                Thread.Sleep(5000);
                foreach (var m in sendedMessages)
                {
                    bot.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: m.MessageId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
