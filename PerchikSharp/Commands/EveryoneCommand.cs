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
        public string Command { get { return "everyone"; } }
        public void OnExecution(object sender, CommandEventArgs command)
        {
            try
            {
                var bot = sender as Pieprz;
                Message message = command.Message;

                if (message.Chat.Type == ChatType.Private)
                    return;

                int[] users_whitelist = { 204678400
                                         /*тут огурец*/ };
                if (!bot.isUserAdmin(message.Chat.Id, message.From.Id) &&
                    !users_whitelist.Any(id => id == message.From.Id))
                    return;


                using (var db = PerchikDB.GetContext())
                {
                    var users = db.Users.ToList();
                    string message_str = string.Empty;

                    int max_users_in_message = 10;
                    List<Message> sended_messages = new List<Message>();

                    int i = 0;
                    foreach (var user in users)
                    {
                        string firstname = user.FirstName.Replace('[', '<').Replace(']', '>');
                        message_str += $"[{firstname}](tg://user?id={user.Id})\n";
                        if (i % max_users_in_message == 0 || i == users.Count() - 1)
                        {
                            var msg = bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: message_str,
                                parseMode: ParseMode.Markdown).Result;
                            sended_messages.Add(msg);
                            message_str = string.Empty;
                        }

                        i++;
                    }

                    Thread.Sleep(5000);
                    foreach (var m in sended_messages)
                    {
                        bot.DeleteMessageAsync(
                               chatId: message.Chat.Id,
                               messageId: m.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
