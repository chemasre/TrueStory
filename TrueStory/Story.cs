using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TrueStory
{
    internal class Story
    {
        internal const string storyFilename = ".truestory";

        internal string RootPath { get { return rootPath;  } }

        FileStream file;
        string rootPath;
        bool isClosed;

        Story(FileStream _file)
        {
            file = _file;
            rootPath = Path.GetDirectoryName(file.Name) ?? "";
        }

        internal static bool ExistsStory(string path)
        {
            bool result;

            result = File.Exists(path + Path.DirectorySeparatorChar + storyFilename);

            return result;
        }

        internal static Story CreateStory(string path)
        {
            FileStream file = File.Create(path + Path.DirectorySeparatorChar + storyFilename);

            Story result = new(file);

            return result;

        }

        internal static Story OpenStory(string path)
        {
            FileStream file = File.Open(path + Path.DirectorySeparatorChar + storyFilename, FileMode.Open);

            Story result = new(file);

            return result;

        }

        internal void Close()
        {
            if (isClosed) { return;  }

            file.Dispose();
            isClosed = true;
        }

        internal void CreateFolder(string name)
        {
            if(isClosed) { return; }

            Directory.CreateDirectory(rootPath + Path.DirectorySeparatorChar + name);


        }

        internal void CreateFile(string name)
        {
            if (isClosed) { return; }

            FileStream f = File.Create(rootPath + Path.DirectorySeparatorChar + name);
            f.Dispose();


        }

        internal void DeleteDirectory(string name)
        {
            if (isClosed) { return; }

            Directory.Delete(rootPath + Path.DirectorySeparatorChar + name, true);
        }

        internal void DeleteFile(string name)
        {
            if (isClosed) { return; }

            File.Delete(rootPath + Path.DirectorySeparatorChar + name);
        }
    }
}
