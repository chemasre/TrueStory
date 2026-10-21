using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryPlayer
{
    public class Version
    {
        public const string AppName = "TrueStory Player";

        public const int Major = 0;
        public const int Minor = 1;
        public const int Patch = 1;

        public const string Alias = "Fixed question lines";

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
