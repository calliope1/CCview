using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.Core.JsonHandler
{
    public class JsonValidationException : Exception
    {
        public string SourceFile { get; }
        public string JsonPath { get; }
        public JsonValidationException(string message, string sourceFile = null!, string jsonPath = null!)
            : base(BuildMessage(message, sourceFile, jsonPath))
        {
            SourceFile = sourceFile;
            JsonPath = jsonPath;
        }
        static string BuildMessage(string message, string sourceFile, string jsonPath)
        {
            var prefix = sourceFile != null ? $"File: {sourceFile}" : $"File: <unknown>";
            var pathPart = jsonPath != null ? $"Path: {jsonPath}" : "";
            return $"{prefix}{pathPart} - {message}";
        }
        public static string ExceptionMessage(string message, string sourceFile, string jsonPath)
        {
            return BuildMessage(message, sourceFile, jsonPath);
        }
    }
}
