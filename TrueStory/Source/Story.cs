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

        public struct FileChange
        {
            public int Offset;
            public int Added;
            public string AddedText;
            public int Removed;
        }

        public enum RecordType
        {
            Header,
            CreateFile,
            CreateFolder,
            DeleteFile,
            DeleteDirectory,
            LoadFile,
            SaveFile
        }

        public struct Record
        {
            public RecordType type;
            public long date;
            public byte[] data;
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

        const string DataSeparator = "[$$%%&&]";

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

            if (_mode == Mode.Create)
            {
                Debug.Assert(header.HasValue);

                file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);

                Record r = new();
                r.type = RecordType.Header;
                r.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                r.data = Encoding.UTF8.GetBytes(header.Value.publicKey + DataSeparator + header.Value.author);

                AddRecord(r);
            }
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

            Record record = new();

            record.type = RecordType.CreateFolder;
            record.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            record.data = Encoding.UTF8.GetBytes(name);

            AddRecord(record);

            OnCreateFolder?.Invoke(name);

        }

        void AddRecord(Record record)
        {
            int bufferSize = sizeof(int) + sizeof(RecordType) + sizeof(long) + sizeof(int) + record.data.Length;

            int offset = 0;
            byte[] buffer = new byte[bufferSize];

            byte[] bytes = BitConverter.GetBytes(bufferSize);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            bytes = BitConverter.GetBytes((int)record.type);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            bytes = BitConverter.GetBytes(record.date);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(long);

            bytes = BitConverter.GetBytes(record.data.Length);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            record.data.CopyTo(buffer, offset);

            file.Write(buffer);
            file.Flush();

        }

        public void AddCreateFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.CreateFile;
            record.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            record.data = Encoding.UTF8.GetBytes(name);

            AddRecord(record);

            OnCreateFile?.Invoke(name);

        }

        public void AddDeleteDirectory(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.DeleteDirectory;
            record.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            record.data = Encoding.UTF8.GetBytes(name);

            AddRecord(record);

            OnDeleteFolder?.Invoke(name);
        }

        public void AddDeleteFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.DeleteFile;
            record.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            record.data = Encoding.UTF8.GetBytes(name);

            AddRecord(record);

            OnDeleteFile?.Invoke(name);
        }

        public string LoadFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string? result = OnLoadFile?.Invoke(name);

            Debug.Assert(result != null);

            Record record = new();

            record.type = RecordType.DeleteFile;
            record.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            record.data = Encoding.UTF8.GetBytes(name + DataSeparator + result);

            AddRecord(record);


            return result;
        }

        public void SaveFile(string name, string text, FileChange[] changes)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.DeleteFile;
            record.date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            StringBuilder builder = new();
            builder.Append(name);
            builder.Append(DataSeparator);
            builder.Append(changes.Length);
            foreach (FileChange c in changes)
            {
                builder.Append(DataSeparator);
                builder.Append(c.Offset);
                builder.Append(DataSeparator);
                builder.Append(c.Added);
                builder.Append(DataSeparator);
                builder.Append(c.AddedText);
                builder.Append(DataSeparator);
                builder.Append(c.Removed);

            }
            builder.Append(DataSeparator);
            builder.Append(text);

            record.data = Encoding.UTF8.GetBytes(builder.ToString());

            AddRecord(record);

            OnSaveFile?.Invoke(name, text);
        }

        public void Dispose()
        {
            file.Dispose();

            disposed = true;
        }


    }
}
