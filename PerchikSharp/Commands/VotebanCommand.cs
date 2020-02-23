using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class VotebanCommand : INativeCommand
    {
        List<long> votebanning_groups = new List<long>();

        public string Command { get { return "voteban"; } }
        public async void OnExecution(object sender, TelegramBotClient bot, CommandEventArgs command)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                await bot.SendTextMessageAsync(
                             chatId: command.Message.Chat.Id,
                             text: StringManager.FromFile("votebanusage.txt"),
                             parseMode: ParseMode.Markdown);
                return;
            }

            try
            {
                if (votebanning_groups.Contains(command.Message.Chat.Id))
                {
                    await bot.SendTextMessageAsync(
                              chatId: command.Message.Chat.Id,
                              text: Program.strManager["VOTEBAN_ALREADY"],
                              parseMode: ParseMode.Markdown);
                    return;
                }

                const int time_secs = 60 * 3; //3 minutes
                const int min_vote_count = 6;
                const double vote_ratio = 0.7;
                const int alert_period = 30;

                string username = command.Text.Replace("@", "");

                using (var db = PerchikDB.GetContext())
                {

                    var user = db.Users
                    .AsNoTracking()
                    .Where(u =>
                        (u.FirstName.Contains(username, StringComparison.OrdinalIgnoreCase)) ||
                        (u.LastName.Contains(username, StringComparison.OrdinalIgnoreCase)) ||
                        (u.UserName.Contains(username, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                    if (user == null)
                    {
                        await bot.SendTextMessageAsync(
                                  chatId: command.Message.Chat.Id,
                                  text: $"*Пользователя \"{command.Text}\" нет в базе.*",
                                  parseMode: ParseMode.Markdown);
                        return;
                    }

                    username = user.FirstName.Replace('[', '<').Replace(']', '>');
                    string userlink = $"[{username}](tg://user?id={user.Id})";

                    Message message = command.Message;
                    string[] opts = { "За", "Против" };
                    var poll_msg = await bot.SendPollAsync(
                        chatId: message.Chat.Id,
                        question: string.Format(Program.strManager["VOTEBAN_QUESTION"], username),
                        options: opts,
                        disableNotification: false,
                        isAnonymous: false);

                    var chat = await bot.GetChatAsync(message.Chat.Id);
                    Logger.Log(LogType.Info, $"<{chat.Title}>: Voteban poll started for {username}:{user.Id}");

                    int legitvotes = 0, ignored = 0;
                    int forban = 0, againstban = 0;

                    Poll recent_poll = poll_msg.Poll;
                    List<PollAnswer> answers = new List<PollAnswer>();
                    votebanning_groups.Add(command.Message.Chat.Id);


                    (sender as Pieprz).RegisterPoll(poll_msg.Poll.Id, (_, p) =>
                    {
                        if (p.pollAnswer == null)
                            return;

                        recent_poll = p.poll;
                        var pollanswer = p.pollAnswer;
                        var existingUser = db.Users.Where(x => x.Id == pollanswer.User.Id).FirstOrDefault();
                        if (existingUser != null)
                        {
                            if (pollanswer.OptionIds.Length > 0)
                            {
                                answers.Add(pollanswer);
                                Logger.Log(LogType.Info,
                                            $"<{chat.Title}>: Voteban {pollanswer?.User.FirstName}:{pollanswer?.User.Id} voted {pollanswer.OptionIds[0]}");
                            }
                            else
                            {
                                answers.RemoveAll(a => a.User.Id == pollanswer.User.Id);
                                Logger.Log(LogType.Info,
                                            $"<{chat.Title}>: Voteban {pollanswer?.User.FirstName}:{pollanswer?.User.Id} retracted vote");
                            }
                        }
                        else
                        {
                            Logger.Log(LogType.Info,
                                $"<{chat.Title}>: Voteban ignored user from another chat {pollanswer?.User.FirstName}:{pollanswer?.User.Id}");
                        }
                    });


                    List<Message> msg2delete = new List<Message>();

                    int alerts_count = time_secs / alert_period;
                    for (int alerts = 1; alerts < alerts_count; alerts++)
                    {
                        await Task.Delay(1000 * alert_period);

                        forban = answers.Sum(a => a.OptionIds[0] == 0 ? 1 : 0);
                        againstban = answers.Sum(a => a.OptionIds[0] == 1 ? 1 : 0);
                        legitvotes = answers.Count;
                        ignored = recent_poll.TotalVoterCount - legitvotes;

                        msg2delete.Add(await bot.SendTextMessageAsync(
                                  chatId: message.Chat.Id,
                                  text: string.Format(Program.strManager["VOTEBAN_ALERT"],
                                    user.FirstName, time_secs - alerts * alert_period, legitvotes, min_vote_count,
                                    forban, againstban),
                                  replyToMessageId: poll_msg.MessageId,
                                  parseMode: ParseMode.Markdown));

                        Logger.Log(LogType.Info,
                            $"<{chat.Title}>: Voteban poll status {forban}<>{againstban}, totalvotes: {recent_poll.TotalVoterCount}, ignored: {ignored}");
                    }

                    await Task.Delay(1000 * alert_period);

                    await bot.StopPollAsync(message.Chat.Id, poll_msg.MessageId);
                    (sender as Pieprz).RemovePoll(poll_msg.Poll.Id);
                    votebanning_groups.Remove(command.Message.Chat.Id);
                    msg2delete.ForEach(m => bot.DeleteMessageAsync(m.Chat.Id, m.MessageId));

                    forban = answers.Sum(a => a.OptionIds[0] == 0 ? 1 : 0);
                    againstban = answers.Sum(a => a.OptionIds[0] == 1 ? 1 : 0);
                    legitvotes = answers.Count;
                    ignored = recent_poll.TotalVoterCount - legitvotes;

                    string igore_text = ignored > 0 ? string.Format(Program.strManager["VOTEBAN_IGNORED"], ignored) : "";

                    if (legitvotes < min_vote_count)
                    {
                        await bot.SendTextMessageAsync(
                                  chatId: message.Chat.Id,
                                  text: string.Format($"{Program.strManager["VOTEBAN_NOTENOUGH"]}\n\n{igore_text}", legitvotes, min_vote_count,
                                    forban, againstban),
                                  replyToMessageId: poll_msg.MessageId,
                                  parseMode: ParseMode.Markdown);
                        Logger.Log(LogType.Info, $"<{chat.Title}>: {forban}<>{againstban} Poll result: Not enough votes");
                        return;
                    }

                    double ratio = (double)forban / (double)legitvotes;
                    if (ratio < vote_ratio)
                    {
                        await bot.SendTextMessageAsync(
                                  chatId: message.Chat.Id,
                                  text: string.Format($"{Program.strManager["VOTEBAN_RATIO"]}\n\n {igore_text}", ratio * 100),
                                  replyToMessageId: poll_msg.MessageId,
                                  parseMode: ParseMode.Markdown);
                        Logger.Log(LogType.Info, $"<{chat.Title}>: {forban}<>{againstban} Poll result: Decided not to ban");
                        return;
                    }

                    await FullyRestrictUserAsync(
                        chatId: message.Chat.Id,
                        userId: user.Id,
                        forSeconds: 60 * 15);

                    var restriction = DbConverter.GenRestriction(command.Message, DbConverter.DateTimeUTC2.AddSeconds(60 * 15));
                    db.Restrictions.Add(restriction);
                    db.SaveChanges();

                    await bot.SendTextMessageAsync(
                                   chatId: message.Chat.Id,
                                   text: string.Format($"{Program.strManager["VOTEBAN_BANNED"]}\n\n {igore_text}", userlink,
                                    forban, againstban),
                                   replyToMessageId: poll_msg.MessageId,
                                   parseMode: ParseMode.Markdown);

                    Logger.Log(LogType.Info,
                        $"<{chat.Title}>: Poll result: {forban}<>{againstban} The user has been banned!");

                }
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }

        private static Task FullyRestrictUserAsync(ChatId chatId, int userId, int forSeconds = 40)
        {
            var until = DbConverter.DateTimeUTC2.AddSeconds(forSeconds);
            return Pieprz.RestrictUserAsync(chatId.Identifier, userId, until);
        }
    }
}
