namespace TrueStory
{
    public class Version
    {
        public const int Major = 0;
        public const int Minor = 13;
        public const int Patch = 3;

        public const string Alias = "Cambio tamaño de fuente";

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Major + "." + Minor + "." + Patch + (includeAlias ? " [" + Alias + "]" : "");
        }

    }
}
