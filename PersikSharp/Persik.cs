using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace PersikSharp
{
    public class PersikEventArgs : EventArgs
    {
        public PersikEventArgs(Message msg, Match m)
        { this.Message = msg; this.Match = m; }
        public Match Match { get; }
        public Message Message { get; }
    }
// comment
    class Persik
    {
        /// <summary>
        /// Called if none of the regular expressions matches.
        /// </summary>
        public event EventHandler<PersikEventArgs> onNoneMatched;

        private Dictionary<string, EventHandler<PersikEventArgs>> commandCallbacks =
       new Dictionary<string, EventHandler<PersikEventArgs>>();

        /// <summary>
        /// Registers a callback that will be called if the regular expression matches the message. (ParseMessage)
        /// </summary>
        /// <param name="regex">Regular expression string.</param>
        /// <param name="e">Method to be called.</param>
        public void AddCommandRegEx(string regex, EventHandler<PersikEventArgs> e)
        {
            commandCallbacks.Add(regex, e);
        }

        /// <summary>
        /// Checks the message for a match with each of the regular expressions and sends callbacks.
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

                    PersikEventArgs args = new PersikEventArgs(msg, command_match);
                    command.Value?.Invoke(this, args);
                    Logger.Log(LogType.Info, $"<{this.GetType().Name}>({msg.From.FirstName}:{msg.From.Id}) -> {command.Key}");
                }
            }

            if (!AtLeastOneMatch)
            {
                PersikEventArgs args = new PersikEventArgs(msg, null);
                onNoneMatched?.Invoke(this, args);
            }
        }

    }
}
