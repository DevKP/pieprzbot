using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class RandomCommand : IRegExCommand
    {
        public string RegEx { get { return @"(?!\s)(?<first>[\W\w\s]+)\sили\s(?<second>[\W\w\s]+)(?>\s)?"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, RegExArgs command)
        {
            Message message = command.Message;

            Regex regx = new Regex(Program.strManager["BOT_REGX"], RegexOptions.IgnoreCase);
            string without_perchik = regx.Replace(message.Text, string.Empty, 1);

            var match = Regex.Match(without_perchik, command.Pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Random rand = new Random();
                string result;
                string first = match.Groups["first"].Value.Replace("?", "");
                string second = match.Groups["second"].Value.Replace("?", ""); ;


                result = rand.NextDouble() >= 0.5 ? first : second;

                if (first.Equals(second))
                    result = Program.strManager.GetRandom("CHOICE_EQUAL");

                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: String.Format(Program.strManager.GetRandom("CHOICE"), result),
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: message.MessageId);
            }
        }
    }
}
