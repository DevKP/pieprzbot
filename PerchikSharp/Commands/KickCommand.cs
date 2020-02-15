using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class KickCommand : IRegExCommand
    {
        public string RegEx { get { return @"\bкик\b"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
            Message message = command.Message;

            if (message.Chat.Type == ChatType.Private)
                return;
            if (!BotHelper.isUserAdmin(message.Chat.Id, message.From.Id))
                return;
            if (message.ReplyToMessage == null)
                return;

            try
            {
                await bot.KickChatMemberAsync(
                    chatId: message.Chat.Id,
                    userId: message.ReplyToMessage.From.Id);

                await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(Program.strManager.GetRandom("KICK"), BotHelper.MakeUserLink(message.ReplyToMessage.From)),
                        parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
