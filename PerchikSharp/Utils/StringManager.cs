﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PerchikSharp
{
    class StringManager
    {
        private readonly Random _rand = new Random(Guid.NewGuid().GetHashCode());
        private Dictionary<string, List<string>> _dict;

        /// <summary>
        /// Analogue of method </see cref="GetRandom">.
        /// </summary>
        /// <returns>
        /// String from dictionary.
        /// </returns>
        /// <param name="s">Key for the string in the dictionary.</param>
        public string this[string s] => this.GetRandom(s);

        private List<string> _get_value(string key)
        {
            try
            {
                return _dict[key];
            }
            catch (KeyNotFoundException)
            {
                Logger.Log(LogType.Error, $"<{this.GetType().Name}> String \"{key}\" not found!!");
                var errorString = new List<string> { $"Строка \"{key}\" не найдена!" };
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
            _ = path ?? throw new ArgumentNullException(nameof(path));
            _ = path.Length == 0 ? throw new ArgumentException(nameof(path.Length)) : path;

            using var streamReader = new StreamReader(path, Encoding.UTF8);
            var readContents = streamReader.ReadToEnd();
            return readContents;
        }

        public StringManager() { }

        public StringManager(string jsonPath)
        {
            _ = jsonPath ?? throw new ArgumentNullException(nameof(jsonPath));

            try
            {
                using var streamReader = new StreamReader(jsonPath, Encoding.UTF8);
                var readContents = streamReader.ReadToEnd();

                _dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
            }
            catch (FileNotFoundException fe)
            {
                Logger.Log(LogType.Fatal, $"No dictionary file found! Exception: {fe.Message}");

            }
        }

        public StringManager(FileStream file)
        {
            _ = file ?? throw new ArgumentNullException(nameof(file));

            using var reader = new StreamReader(file);
            var readContents = reader.ReadToEnd();
            
            _dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
        }

        /// <summary>
        /// Parse json file with a dictionary.
        /// </summary>
        /// <param name="jsonPath">File path</param>
        public void Open(string jsonPath)
        {
            using var streamReader = new StreamReader(jsonPath, Encoding.UTF8);
            var readContents = streamReader.ReadToEnd();

            _dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
        }

        public static StringManager Create(string jsonPath) => new StringManager(jsonPath);

        /// <summary>
        /// Returns all strings of the specified key.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        /// <param name="key">Key for the string in the dictionary.</param>
        public List<string> GetList(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            _ = _dict ?? throw new NullReferenceException(nameof(_dict));

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
            _ = key ?? throw new ArgumentNullException(nameof(key));
            _ = _dict ?? throw new NullReferenceException(nameof(_dict));

            var strings = this._get_value(key);
            if (strings.Count > 1)
                return strings[_rand.Next(0, strings.Count)];

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
            _ = key ?? throw new ArgumentNullException(nameof(key));
            _ = _dict ?? throw new NullReferenceException(nameof(_dict));

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
            _ = _dict ?? throw new NullReferenceException(nameof(_dict));

            return this._dict.Values.SelectMany(x => x).ToList();
        }

        /// <summary>
        /// Returns all keys from dictionary.
        /// </summary>
        /// <returns>
        /// List of strings.
        /// </returns>
        public List<string> GetKeysList()
        {
            _ = _dict ?? throw new NullReferenceException(nameof(_dict));

            return new List<string>(this._dict.Keys);
        }
    }
}