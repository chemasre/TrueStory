namespace TrueStory
{
    public class AppInfo
    {
        public const string AppName = "TrueEditor Activity Designer";

        public const string AppAuthor = "Sinestesia";

        public static string GetFullName()
        {
            return AppName + " " + Version.GetVersion();
        }


    }
}
