namespace TrueStory
{
    public class Version
    {
        public const int Major = 0;
        public const int Minor = 12;
        public const int Patch = 1;

        public const string Alias = "Asistente IA configurable";

        public static string GetVersion(bool includeAlias = true)
        {
            return "v" + Major + "." + Minor + "." + Patch + (includeAlias ? " [" + Alias + "]" :"");
        }

    }
}
