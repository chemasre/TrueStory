using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
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

        public struct HeaderData
        {
            public string publicKey;
            public string author;
        }

        public struct FileChangeData
        {
            public int Offset;
            public int Added;
            public string AddedText;
            public int Removed;
            public long DeltaTime;
        }

        public struct ExecutedCommandData
        {
            public string command;
            public string output;
        }

        public struct QuestionData
        {
            public string question;
            public string answer;
            public int usedTime;
            public int totalTime;
        }

        public enum RecordType
        {
            Header,
            CreateFile,
            CreateFolder,
            DeleteFile,
            DeleteDirectory,
            LoadFile,
            SaveFile,
            ExecuteCommand
        }

        public struct Record
        {
            public RecordType type;
            public byte[] data;
        }

        public event CreateFileHandler? OnCreateFile;
        public event CreateFileHandler? OnCreateFolder;
        public event CreateFileHandler? OnDeleteFile;
        public event CreateFileHandler? OnDeleteFolder;
        public event LoadFileHandler? OnLoadFile;
        public event SaveFileHandler? OnSaveFile;
        public event ExecuteCommandHandler? OnExecuteCommand;


        public delegate void CreateFileHandler(string name);
        public delegate void CreateFolderHandler(string name);
        public delegate void DeleteFileHandler(string name);
        public delegate void DeleteFolderHandler(string name);
        public delegate string LoadFileHandler(string name);
        public delegate void SaveFileHandler(string name, string text);
        public delegate string ExecuteCommandHandler(string name);


        public const string StoryFilename = ".truestory";

        public string RootPath { get { return rootPath;  } }

        FileStream file;
        string rootPath;
        Mode mode;

        bool disposed;

        public Story(string _rootPath, Mode _mode, HeaderData? header = null)
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

                BinaryBuffer buffer = new();

                buffer.WriteString(header.Value.publicKey);
                buffer.WriteString(header.Value.author);

                r.data = buffer.GetBytes();

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

            BinaryBuffer buffer = new();

            buffer.WriteString(name);

            record.data = buffer.GetBytes();

            AddRecord(record);

            OnCreateFolder?.Invoke(name);

        }

        void AddRecord(Record record)
        {
            int bufferSize = sizeof(int) + sizeof(RecordType) + sizeof(int) + record.data.Length;

            int offset = 0;
            byte[] buffer = new byte[bufferSize];

            byte[] bytes = BitConverter.GetBytes(bufferSize);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            bytes = BitConverter.GetBytes((int)record.type);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

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

            BinaryBuffer buffer = new();

            buffer.WriteString(name);

            record.data = buffer.GetBytes();

            AddRecord(record);

            OnCreateFile?.Invoke(name);

        }

        public void AddDeleteDirectory(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.DeleteDirectory;

            BinaryBuffer buffer = new();

            buffer.WriteString(name);

            record.data = buffer.GetBytes();

            AddRecord(record);

            OnDeleteFolder?.Invoke(name);
        }

        public void AddDeleteFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.DeleteFile;

            BinaryBuffer buffer = new();

            buffer.WriteString(name);

            record.data = buffer.GetBytes();

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

            record.type = RecordType.LoadFile;

            BinaryBuffer buffer = new();

            buffer.WriteString(name);
            buffer.WriteString(result);

            record.data = buffer.GetBytes();

            AddRecord(record);


            return result;
        }

        public void SaveFile(string name, string text, FileChangeData[] changes)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.SaveFile;

            BinaryBuffer buffer = new();

            buffer.WriteString(name);
            buffer.WriteInt(changes.Length);

            foreach (FileChangeData c in changes)
            {
                buffer.WriteInt(c.Offset);
                buffer.WriteInt(c.Added);
                buffer.WriteString(c.AddedText);
                buffer.WriteInt(c.Removed);
                buffer.WriteLong(c.DeltaTime);

            }

            buffer.WriteString(text);

            record.data = buffer.GetBytes();

            AddRecord(record);

            OnSaveFile?.Invoke(name, text);
        }

        public string ExecuteCommand(string command)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string result = OnExecuteCommand?.Invoke(command) ?? "";

            Record record = new();

            record.type = RecordType.ExecuteCommand;

            BinaryBuffer buffer = new();

            buffer.WriteString(command);
            buffer.WriteString(result);

            record.data = buffer.GetBytes();

            AddRecord(record);

            return result;


        }

        public void Dispose()
        {
            file.Dispose();

            disposed = true;
        }


    }
}
