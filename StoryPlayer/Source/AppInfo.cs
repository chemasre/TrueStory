namespace TrueStory
{
    public class AppInfo
    {
        public const string AppName = "TrueStory Activity Player";

        public const string AppAuthor = "Sinestesia";

        public static string GetFullName()
        {
            return AppName + " " + Version.GetVersion();
        }

    }
}
