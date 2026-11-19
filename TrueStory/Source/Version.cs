namespace TrueStory
{
    public class Version
    {
        public const string AppName = "TrueStory Activity Editor";

        public const int Major = 0;
        public const int Minor = 11;
        public const int Patch = 0;

        public const string Alias = "Reintentos en las consultas IA";

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
