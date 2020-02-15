using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;

namespace PerchikSharp.Commands
{
    class TestRegExCommand : IRegExCommand
    {
        public string RegEx { get { return @"Pa$$word"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
        }
    }
}
