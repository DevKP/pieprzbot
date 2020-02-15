using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PerchikSharp
{
    class StringManager
    {
        private readonly Random rand = new Random(Guid.NewGuid().GetHashCode());
        private Dictionary<string, List<string>> dict = null;

        /// <summary>
        /// Analogue of method <see cref="GetRandom">.
        /// </summary>
        /// <returns>
        /// String from dictionary.
        /// </returns>
        /// <param name="s">Key for the string in the dictionary.</param>
        public string this[string s]
        {
            get { return this.GetRandom(s); }
        }

        private List<string> _get_value(string key)
        {
            try
            {
                return this.dict[key];
            }
            catch (KeyNotFoundException)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> String \"{key}\" not found!!");
                var errorString = new List<string>();
                errorString.Add($"Строка \"{key}\" не найдена!");
                return errorString;
            }
        }

        /// <summary>
        /// Returns text file content.
        /// </summary>
        /// <returns>
        /// Text string.
        /// </returns>
        /// <param name="path">Path to text file.</param>
        public static string FromFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException();

            if (path.Length == 0)
                throw new ArgumentException();

            string readContents;
            using (StreamReader streamReader = new StreamReader(path, Encoding.UTF8))
            {
                readContents = streamReader.ReadToEnd();
            }
            return readContents;
        }

        public StringManager()
        {
        }

        public StringManager(string json_path)
        {
            if (json_path == null)
                throw new ArgumentNullException();

            try
            {
                string readContents;
                using (StreamReader streamReader = new StreamReader(json_path, Encoding.UTF8))
                {
                    readContents = streamReader.ReadToEnd();
                }

                dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
            }
            catch (FileNotFoundException fe)
            {
                Logger.Log(LogType.Fatal, $"No dictionary file found! Exception: {fe.Message}");

            }
        }

        public StringManager(FileStream file)
        {
            if (file == null)
                throw new ArgumentNullException();

            string readContents;
            using (StreamReader reader = new StreamReader(file))
            {
                readContents = reader.ReadToEnd();
            }

            dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
        }

        /// <summary>
        /// Parse json file with a dictionary.
        /// </summary>
        /// <param name="json_path">File path</param>
        public void Open(string json_path)
        {
            string readContents;
            using (StreamReader streamReader = new StreamReader(json_path, Encoding.UTF8))
            {
                readContents = streamReader.ReadToEnd();
            }

            dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
        }

        public static StringManager Create(string json_path)
        {
            return new StringManager(json_path);
        }

        /// <summary>
        /// Returns all strings of the specified key.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        /// <param name="key">Key for the string in the dictionary.</param>
        public List<string> GetList(string key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (dict == null)
                throw new NullReferenceException();

            return this._get_value(key);
        }

        /// <summary>
        /// Returns random string of the specified key.
        /// </summary>
        /// <returns>
        /// Random string from dictionary.
        /// </returns>
        /// <param name="key">Key for the string in the dictionary.</param>
        public string GetRandom(string key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (this.dict == null)
                throw new NullReferenceException();

            List<string> strings = this._get_value(key);
            if (strings.Count > 1)
                return strings[rand.Next(0, strings.Count)];
            else
                return strings.First();
        }

        /// <summary>
        /// Returns the first string of the specified key from the list.
        /// </summary>
        /// <returns>
        /// String from dictionary.
        /// </returns>
        /// <param name="key">Key for the string in the dictionary.</param>
        public string GetSingle(string key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (this.dict == null)
                throw new NullReferenceException();

            return this._get_value(key).First();
        }

        /// <summary>
        /// Returns all strings from dictionary.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        public List<string> GetAll()
        {
            if (this.dict == null)
                throw new NullReferenceException();

            return this.dict.Values.SelectMany(x => x).ToList();
        }

        /// <summary>
        /// Returns all keys from dictionary.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        public List<string> GetKeysList()
        {
            if (this.dict == null)
                throw new NullReferenceException();

            return new List<string>(this.dict.Keys);
        }
    }
}