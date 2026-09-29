using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TrueStory
{
    public class Story : IDisposable
    {
        public enum Mode
        {
            Create,
            Append,
            Read
        }

        public struct Header
        {
            public string publicKey;
            public string author;
        }

        public event CreateFileHandler? OnCreateFile;
        public event CreateFileHandler? OnCreateFolder;
        public event CreateFileHandler? OnDeleteFile;
        public event CreateFileHandler? OnDeleteFolder;
        public event LoadFileHandler? OnLoadFile;
        public event SaveFileHandler? OnSaveFile;


        public delegate void CreateFileHandler(string name);
        public delegate void CreateFolderHandler(string name);
        public delegate void DeleteFileHandler(string name);
        public delegate void DeleteFolderHandler(string name);
        public delegate string LoadFileHandler(string name);
        public delegate void SaveFileHandler(string name, string text);


        public const string StoryFilename = ".truestory";

        public string RootPath { get { return rootPath;  } }

        FileStream file;
        string rootPath;
        Mode mode;

        bool disposed;

        public Story(string _rootPath, Mode _mode, Header? header = null)
        {
            rootPath = _rootPath;
            mode = _mode;

            string filePath = _rootPath + Path.DirectorySeparatorChar + StoryFilename;

            Debug.Assert(!(_mode == Mode.Create && header == null), "Se ha de proveer una cabecera al crear la historia");

            if (_mode == Mode.Create) { file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write); }
            else if (_mode == Mode.Append) { file = new FileStream(filePath, FileMode.Append, FileAccess.Write);  }
            else // _mode == Mode.Read
            { file = new FileStream(filePath, FileMode.Open, FileAccess.Read);  }

        }

        public static bool ExistsStory(string path)
        {
            bool result;

            result = File.Exists(path + Path.DirectorySeparatorChar + StoryFilename);

            return result;
        }

        public void AddCreateFolder(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            OnCreateFolder?.Invoke(name);

        }

        public void AddCreateFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            OnCreateFile?.Invoke(name);

        }

        public void AddDeleteDirectory(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            OnDeleteFolder?.Invoke(name);
        }

        public void AddDeleteFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            OnDeleteFile?.Invoke(name);
        }

        public string LoadFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string? result = OnLoadFile?.Invoke(name);

            Debug.Assert(result != null);

            return result;
        }

        public void SaveFile(string name, string text)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            OnSaveFile?.Invoke(name, text);
        }

        public void Dispose()
        {
            disposed = true;
        }
    }
}
