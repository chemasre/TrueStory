using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class Constants
    {
        public const string parentFolderName = "..";
        public const string folderPrefix = "[Carpeta] ";

        public const string tabAsSpaces = "    ";
        public static readonly char[] tabEquivalents = new char[] { ' ', '\t' };

        public static readonly string[] allowedExtensions = new string[]
        {
            "txt", "csv", "md",
            "htm", "html", "css", "js",
            "php",
            "c", "h",
            "hpp", "cpp",
            "cs",
            "java"
        };

        public static readonly char[] wordSeparators = new char[] { ';', '(', ')', '"', '\'', '.', ',' };


        public const long minQuestionInterval = 10;
        public const long maxQuestionInterval = 30;

        public const long questionWarningInterval = 5;

    }
}
