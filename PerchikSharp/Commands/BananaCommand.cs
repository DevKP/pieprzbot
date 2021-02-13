using PerchikSharp.Events;

namespace PerchikSharp.Commands
{
    class BananaCommand : IRegExCommand
    {
        public string RegEx => "банан";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var msg = command.Message;

            if (bot != null)
                await bot.SendStickerAsync(msg.Chat.Id,
                    "CAACAgIAAxkBAAIFEF5W9R46FVtcyEJaS_9i54K3LLW3AALsAgACtXHaBvbV2g1oKhUwGAQ");
        }
    }
}
