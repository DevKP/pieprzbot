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
        public string RegEx => @"(?!\s)(?<first>[\W\w\s]+)\sили\s(?<second>[\W\w\s]+)(?>\s)?";

        public async void OnExecution(object sender, RegExArgs command)
        {
            var bot = sender as Pieprz;
            var message = command.Message;

            var regx = new Regex(Program.strManager["BOT_REGX"], RegexOptions.IgnoreCase);
            var withoutPerchik = regx.Replace(message.Text, string.Empty, 1);

            var match = Regex.Match(withoutPerchik, command.Pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var rand = new Random();
                var first = match.Groups["first"].Value.Replace("?", "");
                var second = match.Groups["second"].Value.Replace("?", ""); ;


                var result = rand.NextDouble() >= 0.5 ? first : second;

                if (first.Equals(second))
                    result = Program.strManager.GetRandom("CHOICE_EQUAL");

                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: string.Format(Program.strManager.GetRandom("CHOICE"), result),
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: message.MessageId);
            }
        }
    }
}
