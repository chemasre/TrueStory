using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static bool HasAllowedExtension(string name)
        {
            string ext = Path.GetExtension(name);

            bool allowed = false;
            int i = 0;
            while (!allowed && i < Constants.allowedExtensions.Length)
            {
                if (ext.Substring(1).ToLower() == Constants.allowedExtensions[i]) { allowed = true; }
                else { i++;  }
            }

            return allowed;
        }

        public static void ReadAllBytes(Stream stream, byte[] bytes)
        {
            if (bytes.Length == 0) { return;  }

            int readed = 0;
            int readedTotal = 0;

            do
            {
                readed = stream.Read(bytes, readedTotal, bytes.Length - readedTotal);

                if (readed == 0) { throw new InvalidOperationException(); }
                else { readedTotal += readed;  }
            }
            while (readedTotal < bytes.Length);

        }

        public static void ReadAllBytes(BinaryReader reader, byte[] bytes)
        {
            if (bytes.Length == 0) { return; }

            int readed = 0;
            int readedTotal = 0;

            do
            {
                readed = reader.Read(bytes, readedTotal, bytes.Length - readedTotal);

                if (readed == 0) { throw new InvalidOperationException(); }
                else { readedTotal += readed; }
            }
            while (readedTotal < bytes.Length);

        }

        public static void Log(string message)
        {
            if (Switches.NoLog) { return;  }

            if (MainWindow.Log != null)
            {
                MainWindow.Log.Add(message);
            }

            Console.WriteLine(message);
        }
    }
}
