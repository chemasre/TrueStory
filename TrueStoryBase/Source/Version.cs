namespace TrueStory
{
    public class Version
    {
        public const int Major = 0;
        public const int Minor = 13;
        public const int Patch = 1;

        public const string Alias = "Chequeo de versiones";

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Major + "." + Minor + "." + Patch + (includeAlias ? " [" + Alias + "]" : "");
        }

    }
}
