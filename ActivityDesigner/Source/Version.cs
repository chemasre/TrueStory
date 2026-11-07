namespace ActivityDesigner
{
    public class Version
    {
        public const string AppName = "TrueStory Activity Designer";

        public const string AppAuthor = "José Manuel Solís";

        public const int Major = 0;
        public const int Minor = 4;
        public const int Patch = 0;

        public const string Alias = "Save/Load profiles";

        public static string GetFullName()
        {
            return AppName + " " + GetVersion();
        }

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Major + "." + Minor + "." + Patch + (includeAlias ? " [" + Alias + "]" : "");
        }

    }
}
