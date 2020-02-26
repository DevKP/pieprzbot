using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Commands
{
    class BananaCommand : IRegExCommand
    {
        public string RegEx { get { return "банан"; } }
        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var msg = command.Message;

            await bot.SendStickerAsync(msg.Chat.Id, "CAACAgIAAxkBAAIFEF5W9R46FVtcyEJaS_9i54K3LLW3AALsAgACtXHaBvbV2g1oKhUwGAQ");
        }
    }
}
