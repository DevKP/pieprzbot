using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class TestCommand : INativeCommand
    {
        public string Command { get { return "test"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
        }
    }
}
