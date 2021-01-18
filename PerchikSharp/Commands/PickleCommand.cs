using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class PickleCommand : INativeCommand
    {
        public string Command => "pickle";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;
            try
            {
                await using var stream = System.IO.File.OpenRead("P_20190512_225535_BF.jpg");
                await bot.SendPhotoAsync(
                    chatId: command.Message.Chat,
                    photo: stream
                );
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
