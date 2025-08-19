using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCview.Core.Services;

namespace CCview.Core.JsonHandler
{
    public static class JsonUtils
    {
        public static JArray ExpectArray(JToken token, string file, string path)
        {
            if (token == null)
            {
                throw new JsonValidationException("Expected JSON array but found null.", file, path);
            }
            if (token.Type != JTokenType.Array)
            {
                throw new JsonValidationException($"Expected JSON array but found {token.Type}. Sample: {PreviewToken(token)}", file, path);
            }
            return (JArray)token;
        }

        public static void ExpectArrayLengthAtLeast(JArray array, int minimumLength, string file, string path)
        {
            if (array.Count < minimumLength)
            {
                throw new JsonValidationException($"Expected array of length >= {minimumLength} but found length {array.Count}. Sample: {PreviewToken(array)}", file, path);
            }
        }

        public static int GetIntAt(JArray array, int index, string file, string path)
        {
            string pathWithIndex = $"{path}[{index}]";
            if (index < 0 || index >= array.Count)
            {
                throw new JsonValidationException($"Missing expected index {index} in array.", file, pathWithIndex);
            }
            JToken token = array[index];
            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>();
            }
            // We allow integers stored as strings
            if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out int outNumber))
            {
                Logging.LogDebug(JsonValidationException.ExceptionMessage($"Expected integer at index {index} but a string as an integer. Sample: {PreviewToken(token)}", file, pathWithIndex));
                return outNumber;
            }
            throw new JsonValidationException($"Expected integer at index {index} but found {token.Type}. Sample: {PreviewToken(token)}", file, pathWithIndex);
        }

        public static string GetStringAt(JArray array, int index, string file, string path, bool allowEmpty = false)
        {
            string pathWithIndex = $"{path}[{index}]";
            if (index < 0 || index >= array.Count)
            {
                throw new JsonValidationException($"Missing expected index {index} in array.", file, pathWithIndex);
            }
            JToken token = array[index];
            if (token.Type == JTokenType.String)
            {
                string? tokenValue = token.Value<string>();
                if (allowEmpty)
                {
                    return tokenValue ?? "";
                }
                if (tokenValue == null || string.IsNullOrWhiteSpace(tokenValue))
                {
                    throw new JsonValidationException($"String at {pathWithIndex} is blank or empty", file, pathWithIndex);
                }
                return tokenValue;
            }
            if (token.Type == JTokenType.Null)
            {
                if (allowEmpty)
                {
                    return "";
                }
                throw new JsonValidationException($"String at {pathWithIndex} is blank or empty", file, pathWithIndex);
            }
            throw new JsonValidationException($"Expected string at index {index} but found {token.Type}. Sample: {PreviewToken(token)}", file, $"{path}[{index}]");
        }

        public static JToken GetTokenAt(JArray array, int index, string file, string path)
        {
            string pathWithIndex = $"{path}[{index}]";
            if (index < 0 || index >= array.Count)
            {
                throw new JsonValidationException($"Missing expected index {index} in array.", file, $"{path}[{index}]");
            }
            return array[index];
        }

        public static string PreviewToken(JToken token, int maxLen = 240)
        {
            try
            {
                string tokenAsString = token.ToString(Newtonsoft.Json.Formatting.None);
                if (tokenAsString.Length <= maxLen) return tokenAsString;
                return string.Concat(tokenAsString.AsSpan(0, maxLen), "...");
            }
            catch
            {
                return $"<{token.Type}>";
            }
        }
        /// <summary>
        /// Saves <paramref name="jObject"/> to <paramref name="path"/>.
        /// </summary>
        /// <param name="jObject">Json object to save.</param>
        /// <param name="path">Path for file to save.</param>
        /// <returns><paramref name="jObject"/> serialized as a string.</returns>
        public static string Save(JToken jObject, string path)
        {
            string json = JsonConvert.SerializeObject(jObject, Formatting.Indented);
            File.WriteAllText(path, json);
            return json;
        }
    }
}
