namespace TrueStory
{
    public class AppInfo
    {
        public const string AppName = "TrueEditor";

        public static string GetFullName()
        {
            return AppName + " " + GetVersion();
        }

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Version.Major + "." + Version.Minor + "." + Version.Patch + (includeAlias ? " [" + Version.Alias + "]" :"");
        }

    }
}
