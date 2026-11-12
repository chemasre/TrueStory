using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace TrueStory
{
    public class Settings
    {
        public class Data
        {
            public List<Tuple<string, int>> IntValues { get; set; } = new();
            public List<Tuple<string, string>> StringValues { get; set; } = new();
        }

        Dictionary<string, int> intValues = new();
        Dictionary<string, string> stringValues = new();


        string file;

        public Settings(string _file)
        {
            if (!File.Exists(_file))
            {
                Data data = new();
                foreach (KeyValuePair<string, int> kp in intValues) { data.IntValues.Add(new Tuple<string, int>(kp.Key, kp.Value)); }
                foreach (KeyValuePair<string, string> kp in stringValues) { data.StringValues.Add(new Tuple<string, string>(kp.Key, kp.Value)); }
                JsonSerializerOptions options = new() { WriteIndented = true };
                string text = JsonSerializer.Serialize(data, options);
                File.WriteAllText(_file, text, Encoding.UTF8);
            }
            else
            {
                string text = File.ReadAllText(_file);
                Data data = JsonSerializer.Deserialize<Data>(text) ?? new();
                foreach (Tuple<string, int> t in data.IntValues) { intValues.Add(t.Item1, t.Item2);  }
                foreach (Tuple<string, string> t in data.StringValues) { stringValues.Add(t.Item1, t.Item2);  }
            }

            file = _file;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (intValues.ContainsKey(key)) { return intValues[key]; }
            else { return defaultValue;  }
        }

        public void SetInt(string key, int value)
        {
            if (intValues.ContainsKey(key)) { intValues[key] = value; }
            else { intValues.Add(key, value); }
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (stringValues.ContainsKey(key)) { return stringValues[key]; }
            else { return defaultValue;  }
        }

        public void SetString(string key, string value)
        {
            if (stringValues.ContainsKey(key)) { stringValues[key] = value; }
            else { stringValues.Add(key, value); }
        }

        public void Save()
        {
            Data data = new();
            foreach (KeyValuePair<string, int> kp in intValues)
            { data.IntValues.Add(new Tuple<string, int>(kp.Key, kp.Value)); }

            foreach (KeyValuePair<string, string> kp in stringValues)
            { data.StringValues.Add(new Tuple<string, string>(kp.Key, kp.Value)); }
            
            JsonSerializerOptions options = new() { WriteIndented = true };
            string text = JsonSerializer.Serialize(data, options);
            File.WriteAllText(file, text, Encoding.UTF8);
        }

    }
}
