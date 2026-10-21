namespace TrueStory
{
    public class Version
    {
        public const string AppName = "TrueStory Editor";

        public const int Major = 0;
        public const int Minor = 6;
        public const int Patch = 2;

        public const string Alias = "Fixed question lines display"; 

        public static string GetFullName()
        {
            return AppName + " " + GetVersion();
        }

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Major + "." + Minor + "." + Patch + (includeAlias ? " [" + Alias + "]" :"");
        }

    }
}
