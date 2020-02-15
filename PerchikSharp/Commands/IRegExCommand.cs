using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;

namespace PerchikSharp.Commands
{
    interface IRegExCommand
    {
        public string RegEx { get; }
        public void OnExecution(object sender, TelegramBotClient bot, RegExArgs command);
    }
}
