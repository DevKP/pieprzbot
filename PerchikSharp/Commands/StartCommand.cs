using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class StartCommand : INativeCommand
    {
        public string Command => "start";

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
