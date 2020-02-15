using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;

namespace PerchikSharp.Commands
{
    class ByWordCommand : IRegExCommand
    {
        public string RegEx { get { return @".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
        }
    }
}
