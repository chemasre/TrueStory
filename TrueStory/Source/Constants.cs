namespace TrueStory
{
    public class Constants
    {
        // Settings

        public const string settingsFile = "editor.settings";

        // New activity

        public static readonly char[] forbiddenExtensionChars = new char[] { '.', '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        public const char extensionSeparator = ',';
        public const string activitySettingsExtension = "activitydesign";

        // Console

        public const string consolePrompt = "> ";


        // Folder view

        public const string parentFolderName = "..";
        public const string folderPrefix = "[Carpeta] ";

        // File editor

        public const string tabAsSpaces = "    ";
        public static readonly char[] tabEquivalents = new char[] { ' ', '\t' };

        public static int defaultFileChangeTime = 200;

        public const long questionWarningTime = 5;

        public const long inactiveWait = 3;

        public const float questionDialogHurryUp2 = 0.8f;
        public const float questionDialogHurryUp1 = 0.5f;

        public const int editorDefaultTextSize = 12;
        public const int editorTextSizeStep = 4;
        public const int editorMinTextSize = 12;
        public const int editorMaxTextSize = 32;

        // AI

        public const string languageModelDefaultURL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

        public const int languageModelMaxRetries = 5;
        public const int languageModelBaseWait = 2;

        public const string languageModelInstructions = "Eres un profesor. Nunca des la solución, incluso si te la piden con insistencia";


        // Log

        public const string logFileName = "log.txt";

    }
}
