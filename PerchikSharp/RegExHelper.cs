using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    class RegExHelper
    {

        public event EventHandler<RegExArgs> onNoneMatched;
        private Dictionary<string, EventHandler<RegExArgs>> regExs;

        public RegExHelper()
        {
            regExs = new Dictionary<string, EventHandler<RegExArgs>>();
        }

        /// <summary>
        /// Registers a callback that will be called if the regular
        /// expression matches the message. <see cref="CheckMessage">
        /// </summary>
        /// <param name="regex">Regular expression string.</param>
        /// <param name="e">Method to be called.</param>
        public void AddRegEx(string regex, EventHandler<RegExArgs> e)
        {
            regExs.Add(regex, e);
        }

        /// <summary>
        /// Checks the message for a match with each of the regular
        /// expressions and sends callbacks.
        /// </summary>
        /// <param name="msg">Message object from API</param>
        public void CheckMessage(Message msg)
        {
            string text = msg.Text;

            bool AtLeastOneMatch = false;
            foreach (var command in regExs)
            {
                var command_match = Regex.Match(text, command.Key, RegexOptions.IgnoreCase);
                if (command_match.Success)
                {
                    Logger.Log(LogType.Info, $"<{this.GetType().Name}>({msg.From.FirstName}:{msg.From.Id}) -> {command.Key}");

                    AtLeastOneMatch = true;

                    RegExArgs args = new RegExArgs(msg, command_match, command.Key);
                    //_ = Task.Run(() => command.Value?.Invoke(this, args));
                    command.Value?.Invoke(this, args);
                }
            }

            if (!AtLeastOneMatch)
            {
                RegExArgs args = new RegExArgs(msg, null, null);
                //_ = Task.Run(() => onNoneMatched?.Invoke(this, args));
                onNoneMatched?.Invoke(this, args);
            }
        }
    }
}
