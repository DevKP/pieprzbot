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

        public void Open(string json_path)
        {
            string readContents;
            using (StreamReader streamReader = new StreamReader(json_path, Encoding.UTF8))
            {
                readContents = streamReader.ReadToEnd();
            }

            dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(readContents);
        }
        public List<string> GetList(string key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (dict == null)
                throw new NullReferenceException();

            return this.get_value(key);
        }

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

        public string GetSingle(string key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (dict == null)
                throw new NullReferenceException();

            return this.get_value(key).First();
        }

        public List<string> GetAll()
        {
            if (dict == null)
                throw new NullReferenceException();

            throw new NotImplementedException();
        }

        public List<string> GetKeysList()
        {
            if (dict == null)
                throw new NullReferenceException();

            throw new NotImplementedException();
        }
    }
}
