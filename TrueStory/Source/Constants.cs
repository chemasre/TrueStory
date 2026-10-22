namespace TrueStory
{
    public class Constants
    {
        // Console

        public const string consolePrompt = "> ";


        // Folder view

        public const string parentFolderName = "..";
        public const string folderPrefix = "[Carpeta] ";

        // File editor

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

        public static int defaultFileChangeTime = 200;

        // Instructor

        public struct Range
        {
            public int min;
            public int max;
        };


        public const int minQuestionOpensWait = 1;
        public const int maxQuestionOpensWait = 2;

        public const int minQuestionSavesWait = 1;
        public const int maxQuestionSavesWait = 2;

        public const int minQuestionCharactersWait = 30 * 4;
        public const int maxQuestionCharactersWait = 60 * 4;

        public const int minQuestionCommandsWait = 2;
        public const int maxQuestionCommandsWait = 5;

        public const int minQuestionsTimeWait = 60 * 2;
        public const int maxQuestionsTimeWait = 60 * 5;

        public const int minQuestionsFetchWait = (int)(60 * 0.5f);
        public const int maxQuestionsFetchWait = (int)(60 * 1.5f);

        public const int questionsTimeWeight = 5;
        public const int questionsInputWeight = 75;
        public const int questionsOpensWeight = 1;
        public const int questionsSavesWeight = 1;
        public const int questionsCommandsWeight = 20;

        // Testing values

        //public const int minQuestionOpensWait = 1;
        //public const int maxQuestionOpensWait = 3;

        //public const int minQuestionSavesWait = 1;
        //public const int maxQuestionSavesWait = 3;

        //public const int minQuestionCharactersWait = 4 * 4;
        //public const int maxQuestionCharactersWait = 8 * 4;

        //public const int minQuestionCommandsWait = 1;
        //public const int maxQuestionCommandsWait = 3;

        //public const int minQuestionsTimeWait = 10;
        //public const int maxQuestionsTimeWait = 20;

        //public const int minQuestionsFetchWait = 5;
        //public const int maxQuestionsFetchWait = 10;

        //public const int questionsTimeWeight = 1;
        //public const int questionsInputWeight = 100;
        //public const int questionsOpensWeight = 1;
        //public const int questionsSavesWeight = 1;
        //public const int questionsCommandsWeight = 1;



        public const long questionWarningTime = 5;

        public const long inactiveWait = 3;

        public const float questionDialogHurryUp2 = 0.8f;
        public const float questionDialogHurryUp1 = 0.5f;

        // Log

        public const string logFileName = "log.txt";

    }
}
