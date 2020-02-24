using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PerchikSharp.Commands
{
    class StickerCommand : INativeCommand
    {
        const long offtopia_id = -1001125742098;
        const int via_tcp_Id = 204678400;

        public string Command { get { return "sticker"; } }
        public async void OnExecution(object sender, CommandEventArgs command)
        {
            
            if (command.Message.Chat.Type != ChatType.Private)
                return;

            var bot = sender as Pieprz;
            // if (!Perchik.isUserAdmin(offtopia_id, e.Message.From.Id))
            //   return;

            try
            {
                await bot.SendTextMessageAsync(
                         chatId: command.Message.Chat.Id,
                         text: Program.strManager["STK"],
                         parseMode: ParseMode.Markdown,
                         replyMarkup: new ForceReplyMarkup());
                (sender as Pieprz).RegisterNextstep(onStickerAnswer, command.Message);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }

        private static async void onStickerAnswer(object sender, NextstepArgs e)
        {
            var bot = sender as Pieprz;
            try
            {
                if (e.Message.Type == MessageType.Sticker)
                {

                   await Program.Bot.SendTextMessageAsync(
                            chatId: e.Message.Chat.Id,
                            text: Program.strManager["STK_OK"],
                            parseMode: ParseMode.Markdown);
                   await Program.Bot.SendStickerAsync(
                        chatId: offtopia_id,
                        sticker: e.Message.Sticker.FileId);

                    (sender as Pieprz).RemoveNextstepCallback(e.Message);
                }
                else
                {
                    if (bot.FindTextCommand(e.Message.Text, "stop"))
                    {
                        await Program.Bot.SendTextMessageAsync(
                           chatId: e.Message.Chat.Id,
                           text: Program.strManager["STK_CANCEL"],
                           parseMode: ParseMode.Markdown);

                        return;
                    }

                    await Program.Bot.SendTextMessageAsync(
                            chatId: e.Message.Chat.Id,
                            text: Program.strManager["STK_WRONG"],
                            parseMode: ParseMode.Markdown,
                            replyMarkup: new ForceReplyMarkup());
                    (sender as Pieprz).RegisterNextstep(onStickerAnswer, e.Message);
                }

            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
