using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PersikSharp
{
    class StringManager
    {
        private readonly Random rand = new Random(DateTime.Now.Second);
        private Dictionary<string, List<string>> dict = null;

        /// <summary>
        /// Analogue of method GetSingle().
        /// </summary>
        /// <returns>
        /// String from dictionary.
        /// </returns>
        /// <param name="s">Key for the string in the dictionary.</param>
        public string this[string s]
        {
            get { return GetSingle(s); }
        }

        private List<string> get_value(string key)
        {
            try
            {
                return dict[key];
            }catch(KeyNotFoundException)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> String \"{key}\" not found!!");
                var errorString = new List<string>();
                errorString.Add($"Строка \"{key}\" не найдена!");
                return errorString;
            }
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
            }catch(FileNotFoundException fe)
            {
                Logger.Log(LogType.Fatal, $"No dictionary file found! Exception: {fe.Message}");
                
            }
        }
        public StringManager(FileStream file)
        {
            if (file == null)
                throw new ArgumentNullException();

            throw new NotImplementedException();
        }

        public StringManager()
        {
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

            return this.get_value(key);
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

            if (dict == null)
                throw new NullReferenceException();

            List<string> strings = this.get_value(key);
            if (strings.Count > 1)
                return strings[rand.Next(0, strings.Count - 1)];
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

            if (dict == null)
                throw new NullReferenceException();

            return this.get_value(key).First();
        }

        /// <summary>
        /// Returns all strings from dictionary.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        public List<string> GetAll()
        {
            if (dict == null)
                throw new NullReferenceException();

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all keys from dictionary.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        public List<string> GetKeysList()
        {
            if (dict == null)
                throw new NullReferenceException();

            throw new NotImplementedException();
        }
    }
}
