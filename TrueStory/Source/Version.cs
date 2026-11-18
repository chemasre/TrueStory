namespace TrueStory
{
    public class Version
    {
        public const string AppName = "TrueStory Activity Editor";

        public const int Major = 0;
        public const int Minor = 10;
        public const int Patch = 0;

        public const string Alias = "Consultas IA integradas";

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
