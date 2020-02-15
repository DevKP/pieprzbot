using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;

namespace PerchikSharp.Commands
{
    interface INativeCommand
    {
        string Command { get; }
        void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command);
    }
}
