﻿using Microsoft.EntityFrameworkCore;
using PerchikSharp.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PerchikSharp.Events;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class VotebanCommand : INativeCommand
    {
        readonly List<long> _votebanningGroups = new List<long>();

        public string Command => "voteban";

        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;
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
                if (_votebanningGroups.Contains(command.Message.Chat.Id))
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

                var username = command.Text.Replace("@", "");

                await using var db = PerchikDB.GetContext();
                var user = db.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => (u.FirstName.Contains(username, StringComparison.OrdinalIgnoreCase)) ||
                                         (u.LastName.Contains(username, StringComparison.OrdinalIgnoreCase)) ||
                                         (u.UserName.Contains(username, StringComparison.OrdinalIgnoreCase)));

                if (user == null)
                {
                    await bot.SendTextMessageAsync(
                        chatId: command.Message.Chat.Id,
                        text: $"*Пользователя \"{command.Text}\" нет в базе.*",
                        parseMode: ParseMode.Markdown);
                    return;
                }

                username = user.FirstName.Replace('[', '<').Replace(']', '>');
                var userLink = $"[{username}](tg://user?id={user.Id})";

                var message = command.Message;
                string[] opts = { "За", "Против" };
                var pollMsg = await bot.SendPollAsync(
                    chatId: message.Chat.Id,
                    question: string.Format(Program.strManager["VOTEBAN_QUESTION"], username),
                    options: opts,
                    disableNotification: false,
                    isAnonymous: false);

                var chat = await bot.GetChatAsync(message.Chat.Id);
                Logger.Log(LogType.Info, $"<{chat.Title}>: Voteban poll started for {username}:{user.Id}");

                int legitvotes = 0, ignored = 0;
                int forban = 0, againstban = 0;

                var recentPoll = pollMsg.Poll;
                var answers = new List<PollAnswer>();
                _votebanningGroups.Add(command.Message.Chat.Id);


                (sender as Pieprz).RegisterPoll(pollMsg.Poll.Id, (_, p) =>
                {
                    if (p.PollAnswer == null)
                        return;

                    recentPoll = p.Poll;
                    var pollAnswer = p.PollAnswer;
                    var existingUser = db.Users.FirstOrDefault(x => x.Id == pollAnswer.User.Id);
                    if (existingUser != null)
                    {
                        if (pollAnswer.OptionIds.Length > 0)
                        {
                            answers.Add(pollAnswer);
                            Logger.Log(LogType.Info,
                                $"<{chat.Title}>: Voteban {pollAnswer?.User.FirstName}:{pollAnswer?.User.Id} voted {pollAnswer.OptionIds[0]}");
                        }
                        else
                        {
                            answers.RemoveAll(a => a.User.Id == pollAnswer.User.Id);
                            Logger.Log(LogType.Info,
                                $"<{chat.Title}>: Voteban {pollAnswer?.User.FirstName}:{pollAnswer?.User.Id} retracted vote");
                        }
                    }
                    else
                    {
                        Logger.Log(LogType.Info,
                            $"<{chat.Title}>: Voteban ignored user from another chat {pollAnswer?.User.FirstName}:{pollAnswer?.User.Id}");
                    }
                });


                var msg2delete = new List<Message>();

                const int alertsCount = time_secs / alert_period;
                for (var alerts = 1; alerts < alertsCount; alerts++)
                {
                    await Task.Delay(1000 * alert_period);

                    forban = answers.Sum(a => a.OptionIds[0] == 0 ? 1 : 0);
                    againstban = answers.Sum(a => a.OptionIds[0] == 1 ? 1 : 0);
                    legitvotes = answers.Count;
                    ignored = recentPoll.TotalVoterCount - legitvotes;

                    msg2delete.Add(await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(Program.strManager["VOTEBAN_ALERT"],
                            user.FirstName, time_secs - alerts * alert_period, legitvotes, min_vote_count,
                            forban, againstban),
                        replyToMessageId: pollMsg.MessageId,
                        parseMode: ParseMode.Markdown));

                    Logger.Log(LogType.Info,
                        $"<{chat.Title}>: Voteban poll status {forban}<>{againstban}, totalvotes: {recentPoll.TotalVoterCount}, ignored: {ignored}");
                }

                await Task.Delay(1000 * alert_period);

                await bot.StopPollAsync(message.Chat.Id, pollMsg.MessageId);
                (sender as Pieprz).RemovePoll(pollMsg.Poll.Id);
                _votebanningGroups.Remove(command.Message.Chat.Id);
                msg2delete.ForEach(m => bot.DeleteMessageAsync(m.Chat.Id, m.MessageId));

                forban = answers.Sum(a => a.OptionIds[0] == 0 ? 1 : 0);
                againstban = answers.Sum(a => a.OptionIds[0] == 1 ? 1 : 0);
                legitvotes = answers.Count;
                ignored = recentPoll.TotalVoterCount - legitvotes;

                var ignoreText = ignored > 0 ? string.Format(Program.strManager["VOTEBAN_IGNORED"], ignored) : "";

                if (legitvotes < min_vote_count)
                {
                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format($"{Program.strManager["VOTEBAN_NOTENOUGH"]}\n\n{ignoreText}", legitvotes, min_vote_count,
                            forban, againstban),
                        replyToMessageId: pollMsg.MessageId,
                        parseMode: ParseMode.Markdown);
                    Logger.Log(LogType.Info, $"<{chat.Title}>: {forban}<>{againstban} Poll result: Not enough votes");
                    return;
                }

                var ratio = (double)forban / (double)legitvotes;
                if (ratio < vote_ratio)
                {
                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format($"{Program.strManager["VOTEBAN_RATIO"]}\n\n {ignoreText}", ratio * 100),
                        replyToMessageId: pollMsg.MessageId,
                        parseMode: ParseMode.Markdown);
                    Logger.Log(LogType.Info, $"<{chat.Title}>: {forban}<>{againstban} Poll result: Decided not to ban");
                    return;
                }

                await bot.FullyRestrictUserAsync(
                    chatId: message.Chat.Id,
                    userId: user.Id,
                    forSeconds: 60 * 15);

                var restriction = DbConverter.GenRestriction(command.Message, DateTime.UtcNow.AddSeconds(60 * 15));
                await db.Restrictions.AddAsync(restriction);
                await db.SaveChangesAsync();

                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: string.Format($"{Program.strManager["VOTEBAN_BANNED"]}\n\n {ignoreText}", userLink,
                        forban, againstban),
                    replyToMessageId: pollMsg.MessageId,
                    parseMode: ParseMode.Markdown);

                Logger.Log(LogType.Info,
                    $"<{chat.Title}>: Poll result: {forban}<>{againstban} The user has been banned!");
            }
            catch (Exception exp)
            {
                Logger.Log(LogType.Error, $"Exception: {exp.Message}\nTrace: {exp.StackTrace}");
            }
        }
    }
}
