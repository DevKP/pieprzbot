﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace PersikSharp
{
    public class PerchikEventArgs : EventArgs
    {
        public PerchikEventArgs(Message msg, Match m)
        { this.Message = msg; this.Match = m; }
        public Match Match { get; }
        public Message Message { get; }
    }
// comment
    class Perchik
    {
        /// <summary>
        /// Called if none of the regular expressions matches.
        /// </summary>
        public event EventHandler<PerchikEventArgs> onNoneMatched;

        private Dictionary<string, EventHandler<PerchikEventArgs>> commandCallbacks =
       new Dictionary<string, EventHandler<PerchikEventArgs>>();

        /// <summary>
        /// Registers a callback that will be called if the regular
        /// expression matches the message. <see cref="ParseMessage">
        /// </summary>
        /// <param name="regex">Regular expression string.</param>
        /// <param name="e">Method to be called.</param>
        public void AddCommandRegEx(string regex, EventHandler<PerchikEventArgs> e)
        {
            commandCallbacks.Add(regex, e);
        }

        /// <summary>
        /// Checks the message for a match with each of the regular
        /// expressions and sends callbacks.
        /// </summary>
        /// <param name="msg">Message object from API</param>
        public void ParseMessage(Message msg)
        {
            string text = msg.Text;

            bool AtLeastOneMatch = false;
            foreach (var command in commandCallbacks)
            {
                var command_match = Regex.Match(text, command.Key, RegexOptions.IgnoreCase);
                if (command_match.Success)
                {
                    AtLeastOneMatch = true; 

                    PerchikEventArgs args = new PerchikEventArgs(msg, command_match);
                    command.Value?.Invoke(this, args);
                    Logger.Log(LogType.Info, $"<{this.GetType().Name}>({msg.From.FirstName}:{msg.From.Id}) -> {command.Key}");
                }
            }

            if (!AtLeastOneMatch)
            {
                PerchikEventArgs args = new PerchikEventArgs(msg, null);
                onNoneMatched?.Invoke(this, args);
            }
        }

        /// <summary>
        /// Checks if there is a specified command in the string.
        /// </summary>
        /// <returns>
        /// Boolean.
        /// </returns>
        /// <param name="text">Text.</param>
        /// <param name="command">Command to find.</param>
        public static bool FindTextCommand(string text, string command)
        {
            var bot_username = Program.Bot.GetMeAsync().Result.Username;
            var match = Regex.Match(text, $"^\\/(?<command>\\w+)(?<botname>@{bot_username})?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups["command"].Value.Equals(command);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the user has administrator rights in the chat.
        /// </summary>
        /// <returns>
        /// Boolean.
        /// </returns>
        /// <param name="chatId">Chat ID.</param>
        /// <param name="userId">User ID.</param>
        public static bool isUserAdmin(long chatId, int userId)
        {
            try
            {
                ChatMember[] chat_members = Program.Bot.GetChatAdministratorsAsync(chatId).Result;
                if (Array.Find(chat_members, e => e.User.Id == userId) != null)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates clickable text link to user profile.
        /// </summary>
        /// <param name="user">User object.</param>
        /// <returns>
        /// Formated text string.
        /// </returns>
        public static string GetUserLink(User user)
        {
            try
            {
                return string.Format("[{0}](tg://user?id={1})", user.FirstName, user.Id);
            }
            catch (NullReferenceException)//If FirstName is null using id as name
            {
                return string.Format("[{0}](tg://user?id={1})", user.Id, user.Id);
            }

        }
    }
}
