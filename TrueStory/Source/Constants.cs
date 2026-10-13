using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class Constants
    {
        // Console

        public const string consolePrompt = "> ";


        // Folder view

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

        public static readonly char[] wordSeparators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',' };

        public const int minQuestionOpensWait = 1;
        public const int maxQuestionOpensWait = 3;

        public const int minQuestionSavesWait = 1;
        public const int maxQuestionSavesWait = 3;

        public const int minQuestionCharactersWait = 4 * 4;
        public const int maxQuestionCharactersWait = 8 * 4;

        public const int minQuestionCommandsWait = 1;
        public const int maxQuestionCommandsWait = 3;

        public const int minQuestionsTimeWait = 10;
        public const int maxQuestionsTimeWait = 20;

        public const int minQuestionsFetchWait = 5;
        public const int maxQuestionsFetchWait = 10;

        public const int questionsTimeWeight = 1;
        public const int questionsInputWeight = 100;
        public const int questionsOpensWeight = 1;
        public const int questionsSavesWeight = 1;
        public const int questionsCommandsWeight = 1;



        //public const int minQuestionOpensWait = 1;
        //public const int maxQuestionOpensWait = 3;

        //public const int minQuestionSavesWait = 1;
        //public const int maxQuestionSavesWait = 3;

        //public const long minQuestionCharactersWait = 300 * 4;
        //public const long maxQuestionCharactersWait = 400 * 4;


        //public const long minQuestionCommandsWait = 10;
        //public const long maxQuestionCommandsWait = 15;

        //public const long minQuestionsTimeWait = 60 * 5;
        //public const long maxQuestionsTimeWait = 60 * 10;

        //public const long minQuestionsFetchWait = 10;
        //public const long maxQuestionsFetchWait =     60;

        //public const int questionsTimeWeight = 10;
        //public const int questionsInputWeight = 60;
        //public const int questionsOpensWeight = 10;
        //public const int questionsSavesWeight = 10;
        //public const int questionsCommandsWeight = 10;


        public const long questionWarningTime = 5;

        public const long inactiveWait = 3;

        public const float questionDialogHurryUp2 = 0.8f;
        public const float questionDialogHurryUp1 = 0.5f;

    }
}
