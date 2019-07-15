using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace PersikSharp
{
    class Perchik
    {
        public static string BotVersion = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;

        /// <summary>
        /// Called if none of the regular expressions matches.
        /// </summary>
        public event EventHandler<RegExArgs> onNoneMatched;

        private Dictionary<string, EventHandler<RegExArgs>> commandCallbacks =
       new Dictionary<string, EventHandler<RegExArgs>>();

        /// <summary>
        /// Registers a callback that will be called if the regular
        /// expression matches the message. <see cref="ParseMessage">
        /// </summary>
        /// <param name="regex">Regular expression string.</param>
        /// <param name="e">Method to be called.</param>
        public void AddCommandRegEx(string regex, EventHandler<RegExArgs> e)
        {
            commandCallbacks.Add(regex, e);
        }

        /// <summary>
        /// Checks the message for a match with each of the regular
        /// expressions and sends callbacks.
        /// </summary>
        /// <param name="msg">Message object from API</param>
        public void ParseMessageAsync(Message msg)
        {
            _ = Task.Run(() => ParseMessage(msg));
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
        public static string MakeUserLink(User user)
        {
            try
            {
                return string.Format("[{0}](tg://user?id={1})", user.FirstName.Replace('[',' ').Replace(']', ' '), user.Id);
            }
            catch (NullReferenceException)//If FirstName is null using id as name
            {
                return string.Format("[{0}](tg://user?id={1})", user.Id, user.Id);
            }

        }

        public static Task SaveFileAsync(string fileId, string folder, string fileName = null)
        {
            return Task.Run(() => SaveFile(fileId, folder, fileName));
        }

        private static async void SaveFile(string fileId, string folder, string fileName = null)
        {
            try
            {
                var file = Program.Bot.GetFileAsync(fileId).Result;
                MemoryStream docu = new MemoryStream();

                const int attempts = 5;
                for (int a = 0; a < attempts; a++)
                {
                    try
                    {
                        await Program.Bot.DownloadFileAsync(file.FilePath, docu);
                        break;
                    }
                    catch (HttpRequestException)
                    {
                        Logger.Log(LogType.Info, $"<Downloader>: Bad Request, attempt #{a}");
                        continue;
                    }
                }


                string file_ext = file.FilePath.Split('.')[1];
                if (fileName == null)
                    fileName = $"{fileId}.{file_ext}";

                bool exists = System.IO.Directory.Exists($"./{folder}/");
                if (!exists)
                    System.IO.Directory.CreateDirectory($"./{folder}/");
                using (FileStream file_stream = new FileStream($"./{folder}/{fileName}",
                    FileMode.Create, System.IO.FileAccess.Write))
                {
                    docu.WriteTo(file_stream);
                    file_stream.Flush();
                    file_stream.Close();
                }
                Logger.Log(LogType.Info, $"<Downloader>: Filename: {fileName} saved.");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Exception: {e.Message}");
            }
        }
    }
}
