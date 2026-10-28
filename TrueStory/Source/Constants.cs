namespace TrueStory
{
    public class Constants
    {
        // New activity

        public static readonly char[] forbiddenExtensionChars = new char[] { '.', '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        public const char extensionSeparator = ',';
        public const string activitySettingsExtension = "storysettings";

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

        // Log

        public const string logFileName = "log.txt";

    }
}
