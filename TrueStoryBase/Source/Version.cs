namespace TrueStory
{
    public class Version
    {
        public const int Major = 0;
        public const int Minor = 14;
        public const int Patch = 0;

        public const string Alias = "Comandos predefinidos de consola";

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Major + "." + Minor + "." + Patch + (includeAlias ? " [" + Alias + "]" : "");
        }

    }
}
