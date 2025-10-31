using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Livesplit.Subnautica
{
    public static class Localization
    {
        private static IReadOnlyDictionary<string, string> _translations = new Dictionary<string, string>();
        private const string ResourcePath = "Livesplit.Subnautica.Resources.English.json";

        private static string StripJsonComments(string s)
        {
            s = Regex.Replace(s, @"^\s*//.*$", "", RegexOptions.Multiline);
            s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return s;
        }

        private static string EscapeInvalidStringChars(string s)
        {
            var sb = new StringBuilder(s.Length + 64);
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (inString)
                {
                    if (escaped)
                    {
                        sb.Append(c);
                        escaped = false;
                    }
                    else
                    {
                        if (c == '\\') { sb.Append(c); escaped = true; }
                        else if (c == '"') { sb.Append(c); inString = false; }
                        else if (c == '\n') sb.Append("\\n");
                        else if (c == '\r') sb.Append("\\r");
                        else if (c == '\t') sb.Append("\\t");
                        else if (c < 0x20) sb.Append("\\u" + ((int)c).ToString("X4"));
                        else sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"') { sb.Append(c); inString = true; }
                    else sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static T DeserializeWithComments<T>(string json)
        {
            var reader = new Utf8JsonReader(
                Encoding.UTF8.GetBytes(json),
                new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            return JsonSerializer.Deserialize<T>(ref reader, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public static void Load()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream(ResourcePath))
            {
                if (stream == null) throw new FileNotFoundException("Embedded resource not found: " + ResourcePath);
                using (var sr = new StreamReader(stream, Encoding.UTF8, true))
                {
                    string json = sr.ReadToEnd();
                    json = StripJsonComments(json);
                    json = EscapeInvalidStringChars(json);
                    var dict = DeserializeWithComments<Dictionary<string, string>>(json);
                    _translations = (dict != null) ? dict : new Dictionary<string, string>();
                }
            }
        }

        public static string GetDisplayName(object key)
        {
            if (_translations == null)
                throw new InvalidOperationException("Translations not loaded.");

            var keyString = key.ToString();

            if (_translations.TryGetValue(keyString, out var value))
                return value;

            var match = _translations.FirstOrDefault(kv => string.Equals(kv.Key, keyString, StringComparison.OrdinalIgnoreCase));

            return match.Value ?? keyString;
        }

        public static string GetRawName(object value)
        {
            if (_translations == null)
                throw new InvalidOperationException("Translations not loaded.");

            var valueString = value.ToString();

            var key = _translations.FirstOrDefault(x => x.Value.Equals(value)).Key;

            if (key != null)
                return key;

            var match = _translations.FirstOrDefault(kv => string.Equals(kv.Key, valueString, StringComparison.OrdinalIgnoreCase));

            return match.Value ?? valueString;
        }
    }
}
