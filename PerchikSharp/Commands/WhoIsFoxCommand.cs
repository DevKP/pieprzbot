using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class WhoIsFoxCommand : IRegExCommand
    {
        public string RegEx => @"(кто|где|(по)?зови)(.*)?лис(ичк[ау]|иц[ау]|[аяюу])";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;

            await bot.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "@FreyjaAnastasievna 🦊",
                       parseMode: ParseMode.Markdown);
        }
    }
}
