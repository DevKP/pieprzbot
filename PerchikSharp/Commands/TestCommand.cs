using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class TestCommand : INativeCommand
    {
        public string Command => "test";

        public void OnExecution(object sender, CommandEventArgs command)
        {
        }
    }
}
