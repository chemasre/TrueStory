using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TrueStory
{
    public class Story
    {
        public struct Header
        {
            public string publicKey;
            public string author;
        }


        public const string storyFilename = ".truestory";

        public string RootPath { get { return rootPath;  } }

        FileStream file;
        string rootPath;
        bool isClosed;

        Story(FileStream file, Header header)
        {
            rootPath = Path.GetDirectoryName(file.Name) ?? "";
        }

        public static bool ExistsStory(string path)
        {
            bool result;

            result = File.Exists(path + Path.DirectorySeparatorChar + storyFilename);

            return result;
        }

        public static Story CreateStory(string path, Header header)
        {
            FileStream file = File.Create(path + Path.DirectorySeparatorChar + storyFilename);

            Story result = new(file, header);

            return result;

        }

        public static Story OpenStory(string path)
        {
            FileStream file = File.Open(path + Path.DirectorySeparatorChar + storyFilename, FileMode.Open);

            Story result = new(file, new Header());

            return result;

        }

        public void Close()
        {
            if (isClosed) { return;  }

            file.Dispose();
            isClosed = true;
        }

        public void CreateFolder(string name)
        {
            if(isClosed) { return; }

            Directory.CreateDirectory(rootPath + Path.DirectorySeparatorChar + name);


        }

        public void CreateFile(string name)
        {
            if (isClosed) { return; }

            FileStream f = File.Create(rootPath + Path.DirectorySeparatorChar + name);
            f.Dispose();


        }

        public void DeleteDirectory(string name)
        {
            if (isClosed) { return; }

            Directory.Delete(rootPath + Path.DirectorySeparatorChar + name, true);
        }

        public void DeleteFile(string name)
        {
            if (isClosed) { return; }

            File.Delete(rootPath + Path.DirectorySeparatorChar + name);
        }

    }
}
