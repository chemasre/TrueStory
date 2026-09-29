using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class Utils
    {
        public static bool IsValidFileName(string text)
        {
            if (text.Length == 0) { return false; }

            char[] invalid = Path.GetInvalidFileNameChars();
            bool valid = true;
            int i = 0;
            while (valid && i < invalid.Length)
            {
                valid = valid && !text.Contains(invalid[i]);

                if (valid) { i++; }
            }

            return valid;
        }

        public static bool IsValidFolderName(string text)
        {
            if (text.Length == 0) { return false; }

            char[] invalid = Path.GetInvalidPathChars();
            bool valid = true;
            int i = 0;
            while (valid && i < invalid.Length)
            {
                valid = valid && !text.Contains(invalid[i]);

                if (valid) { i++; }
            }

            return valid;
        }

    }
}
