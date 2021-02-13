using PerchikSharp.Events;

namespace PerchikSharp.Commands
{
    class PraiseCommand : IRegExCommand
    {
        public string RegEx => @"\b(живой|красавчик|молодец|хороший|умный|умница)\b";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;
            await bot.SendStickerAsync(message.Chat.Id, "CAADAgADQQMAApFfCAABzoVI0eydHSgC");
        }
    }
}