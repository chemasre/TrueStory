namespace TrueStory
{
    public class Version
    {
        public const string AppName = "TrueStory Activity Editor";

        public const int Major = 0;
        public const int Minor = 8;
        public const int Patch = 4;

        public const string Alias = "Find dialog"; 

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
