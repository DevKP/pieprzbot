using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using PerchikSharp.Events;
using Telegram.Bot.Types;

namespace PerchikSharp
{
    class RegExHelper
    {

        public event EventHandler<RegExArgs> onNoneMatched;
        private readonly Dictionary<string, EventHandler<RegExArgs>> _regExs;

        public RegExHelper() =>
            _regExs = new Dictionary<string, EventHandler<RegExArgs>>();

        /// <summary>
        /// Registers a callback that will be called if the regular
        /// expression matches the message. </see cref="CheckMessage">
        /// </summary>
        /// <param name="regex">Regular expression string.</param>
        /// <param name="e">Method to be called.</param>
        public void AddRegEx(string regex, EventHandler<RegExArgs> e) =>
            _regExs.Add(regex, e);


        /// <summary>
        /// Checks the message for a match with each of the regular
        /// expressions and sends callbacks.
        /// </summary>
        /// <param name="msg">Message object from API</param>
        public void CheckMessage(Message msg)
        {
            var text = msg.Text;

            var atLeastOneMatch = false;
            foreach (var (pattern, command) in _regExs)
            {
                var commandMatch = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (!commandMatch.Success) continue;

                atLeastOneMatch = true;

                Logger.Log(LogType.Info, $"<{this.GetType().Name}>({msg.From.FirstName}:{msg.From.Id}) -> {pattern}");

                command?.Invoke(this, new RegExArgs(msg, commandMatch, pattern));
            }

            if (!atLeastOneMatch)
                onNoneMatched?.Invoke(this, new RegExArgs(msg, null, null));
        }
    }
}
