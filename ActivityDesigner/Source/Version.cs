namespace ActivityDesigner
{
    public class Version
    {
        public const string AppName = "TrueStory Activity Designer";

        public const int Major = 0;
        public const int Minor = 2;
        public const int Patch = 0;

        public const string Alias = "Operative version";

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
