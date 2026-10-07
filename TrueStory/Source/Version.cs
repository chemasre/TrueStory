using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class Version
    {
        public const string AppName = "TrueStory";

        public const int Major = 0;
        public const int Minor = 3;
        public const int Patch = 0;

        public const string Alias = "october"; 

        public static string FullName()
        {
            return AppName + " v" + Major + "." + Minor + "." + Patch + " [" + Alias + "]";
        }

    }
}
