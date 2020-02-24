using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class InsultingCommand : IRegExCommand
    {
        public string RegEx { get { return @"\b(дур[ао]к|пид[аоэ]?р|говно|д[еыи]бил|г[оа]ндон|лох|хуй|чмо|скотина)\b"; } }
        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            Message message = command.Message;
            try
            {
                await bot.SendStickerAsync(message.Chat.Id, "CAADAgADJwMAApFfCAABfVrdPYRn8x4C");

                if (message.Chat.Type != ChatType.Private)
                {
                    await Task.Delay(2000);

                    await (sender as Pieprz).FullyRestrictUserAsync(
                                chatId: message.Chat.Id,
                                userId: message.From.Id,
                                forSeconds: 120);

                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: String.Format(Program.strManager.GetSingle("BANNED"), message.From.FirstName, 2, "мин."),
                        parseMode: ParseMode.Markdown);

                    await bot.SendStickerAsync(message.Chat.Id, "CAADAgADPQMAApFfCAABt8Meib23A_QC");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
