using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class InfoCommand : INativeCommand
    {
        public string Command => "info";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;

            await bot.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: StringManager.FromFile(Program.strManager["INFO_PATH"]),
                       parseMode: ParseMode.Markdown);
        }
    }
}
