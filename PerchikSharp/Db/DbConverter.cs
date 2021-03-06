﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp.Db
{
    class DbConverter
    {
        public static Tables.Pidr GenPidr(Telegram.Bot.Types.Message message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            return new Tables.Pidr()
            {
                UserId = message.From.Id,
                ChatId = message.Chat.Id,
                Date = DateTimeUtc2
            };

        }
        public static Tables.User GenUser(Telegram.Bot.Types.User user, string about = null)
        {
            _ = user ?? throw new ArgumentNullException(nameof(user));
                

            return new Tables.User()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.Username,
                Restricted = false,
                Description = about
            };
        }
        public static Tables.Chat GenChat(Telegram.Bot.Types.Chat chat)
        {
            _ = chat ?? throw new ArgumentNullException(nameof(chat));

            return new Tables.Chat()
            {
                Id = chat.Id,
                Title = chat.Title,
                Description = chat.Description
            };
        }
        public static Tables.Message GenMessage(Telegram.Bot.Types.Message message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            return new Tables.Message()
            {
               MessageId = message.MessageId,
               UserId = message.From.Id,
               ChatId = message.Chat.Id,
               Text = message.Text,
               Date = ToEpochTime(message.Date),
               Type = message.Type
            };
        }
        public static Tables.Restriction GenRestriction(Telegram.Bot.Types.Message message, DateTime until)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            return new Tables.Restriction()
            {
                ChatId = message.Chat.Id,
                UserId = message.From.Id,
                Date = DateTime.UtcNow,
                Until = until
            };
        }

        /// <summary>
        /// Converts the given date value to epoch time.
        /// </summary>
        public static long ToEpochTime(DateTime dateTime)
        {
            var date = dateTime.ToUniversalTime();
            var ticks = date.Ticks - new DateTime(year: 1970, month: 1, day: 1,
                hour: 0, minute: 0, second: 0, millisecond: 0, kind: DateTimeKind.Utc).Ticks;

            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }

        /// <summary>
        /// Converts the given date value to epoch time.
        /// </summary>
        public static long ToEpochTime(DateTimeOffset dateTime)
        {
            var date = dateTime.ToUniversalTime();
            var ticks = date.Ticks - new DateTimeOffset(year: 1970, month: 1, day: 1,
                hour: 0, minute: 0, second: 0, offset: TimeSpan.Zero).Ticks;

            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }

        /// <summary>
        /// Converts the given epoch time to a <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/> kind.
        /// </summary>
        public static DateTime ToDateTimeFromEpoch(long intDate)
        {
            var timeInTicks = intDate * TimeSpan.TicksPerSecond;
            return new DateTime(1970, month: 1, day: 1,
                hour: 0, minute: 0, second: 0, millisecond: 0, kind: DateTimeKind.Utc).AddTicks(timeInTicks);
        }

        /// <summary>
        /// Converts the given epoch time to a UTC <see cref="DateTimeOffset"/>.
        /// </summary>
        public static DateTimeOffset ToDateTimeOffsetFromEpoch(long intDate)
        {
            var timeInTicks = intDate * TimeSpan.TicksPerSecond;
            return new DateTimeOffset(year: 1970, month: 1, day: 1,
                hour: 0, minute: 0, second: 0, offset: TimeSpan.Zero).AddTicks(timeInTicks);
        }

        public static DateTime DateTimeUtc2 => DateTime.UtcNow.AddHours(2);
    }
}
