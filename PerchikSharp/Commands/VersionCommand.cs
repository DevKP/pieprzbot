﻿using System;
using PerchikSharp.Events;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class VersionCommand : INativeCommand
    {
        public string Command => "version";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;

            try
            {
                await bot.SendTextMessageAsync(
                       chatId: command.Message.Chat.Id,
                       text: $"*Version: {Pieprz.botVersion}*",
                       parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"Exception: {ex.Message}\nTrace:{ex.StackTrace}");
            }
        }
    }
}
