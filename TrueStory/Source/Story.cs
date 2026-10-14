using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace TrueStory
{
    public class Story : IDisposable
    {
        public const string signature = "TSRY";

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
            public QuestionType type;
            public string text;
            public QuestionContextData context;
            public string answer;
            public int usedTime;
            public int timeLimit;

            public QuestionData()
            {
                type = QuestionType.input;
                text = "";
                context = new QuestionContextData();
                answer = "";
                usedTime = 0;
                timeLimit = 0;

            }

        }

        public struct QuestionContextData
        {
            public string fileName;
            public QuestionContextLineData[] lines;
            public int currentLine;
            public string command;

            public QuestionContextData()
            {
                fileName = "";
                lines = Array.Empty<QuestionContextLineData>();
                currentLine = 0;
                command = "";
            }

        }

        public struct QuestionContextLineData
        {
            public int number;
            public string text;

            public QuestionContextLineData()
            {
                number = 0;
                text = "";
            }
        }

        public enum QuestionType
        {
            input,
            time,
            fileSave,
            fileOpen,
            command,

        }

        public struct PersistentData
        {
            public int instructorCharactersCounter;
            public int instructorCharactersWait;
            public int instructorCommandsCounter;
            public int instructorCommandsWait;
            public int instructorOpensCounter;
            public int instructorOpensWait;
            public int instructorSavesCounter;
            public int instructorSavesWait;
        }

        public enum ReadDirection
        {
            Forward,
            Backwards
        }

        public enum RecordType
        {
            Header,
            CreateFile,
            CreateDirectory,
            DeleteFile,
            DeleteDirectory,
            LoadFile,
            SaveFile,
            ExecuteCommand,
            CloseStory,
            OpenStory,
            Question

        }

        public struct Record
        {
            public RecordType type;
            public byte[] data;
        }

        public event OpenStoryHandler? OnOpenStory;
        public event CreateFileHandler? OnCreateFile;
        public event CreateFolderHandler? OnCreateFolder;
        public event DeleteFileHandler? OnDeleteFile;
        public event DeleteFolderHandler? OnDeleteFolder;
        public event LoadFileHandler? OnLoadFile;
        public event SaveFileHandler? OnSaveFile;
        public event ExecuteCommandHandler? OnExecuteCommand;
        public event QuestionHandler? OnQuestion;
        public event CloseStoryHandler? OnCloseStory;


        public delegate void OpenStoryHandler(PersistentData data);
        public delegate void CreateFileHandler(string name);
        public delegate void CreateFolderHandler(string name);
        public delegate void DeleteFileHandler(string name);
        public delegate void DeleteFolderHandler(string name);
        public delegate string LoadFileHandler(string name);
        public delegate void SaveFileHandler(string name, string text);
        public delegate string ExecuteCommandHandler(string name);
        public delegate QuestionData QuestionHandler(QuestionData q);
        public delegate PersistentData CloseStoryHandler();


        public const string StoryFilename = ".truestory";

        public string RootPath { get { return rootPath;  } }

        FileStream file;
        string rootPath;
        Mode mode;

        byte[] signatureBytes;

        bool disposed;

        public Story(string _rootPath, Mode _mode, HeaderData? header = null)
        {
            rootPath = _rootPath;
            mode = _mode;

            string filePath = _rootPath + Path.DirectorySeparatorChar + StoryFilename;

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            signatureBytes = encoding.GetBytes(Story.signature);

            Debug.Assert(!(_mode == Mode.Create && header == null), "Story: No header provided");

            if (_mode == Mode.Create)
            {
                Debug.Assert(header.HasValue);

                file = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite);

                AddSignature();

                Record r = new();
                r.type = RecordType.Header;

                BinaryBufferWriter buffer = new();

                buffer.WriteString(Guid.NewGuid().ToString());
                buffer.WriteString(header.Value.publicKey);
                buffer.WriteString(header.Value.author);                

                r.data = buffer.GetBytes();

                AddRecord(r);

            }
            else if (_mode == Mode.Append)
            {
                file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);

                string foundSignature = ReadSignature();
                if (signature != foundSignature) { throw new InvalidDataException(); }

                file.Seek(0, SeekOrigin.End);
            }
            else // _mode == Mode.Read
            {
                file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                file.Seek(signatureBytes.Length, SeekOrigin.Begin);

                string foundSignature = ReadSignature();
                if (signature != foundSignature) { throw new InvalidDataException(); }
            }

            Dump();
        }

        string ReadSignature()
        {
            byte[] bytes = new byte[signature.Length];
            Utils.ReadAllBytes(file, bytes);
            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            return encoding.GetString(bytes);
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

            record.type = RecordType.CreateDirectory;

            BinaryBufferWriter buffer = new();

            buffer.WriteString(name);

            record.data = buffer.GetBytes();

            AddRecord(record);

            OnCreateFolder?.Invoke(name);

        }

        void AddSignature()
        {
            file.Write(signatureBytes);
            file.Flush();
        }

        void AddRecord(Record record)
        {
            int bufferSize = sizeof(RecordType) +
                             sizeof(int) +
                             record.data.Length +
                             sizeof(RecordType) +
                             sizeof(int);


            int offset = 0;

            byte[] buffer = new byte[bufferSize];

            byte[] bytes = BitConverter.GetBytes((int)record.type);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            bytes = BitConverter.GetBytes(record.data.Length);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            record.data.CopyTo(buffer, offset);
            offset += record.data.Length;
            
            // Data length and type is added again to make
            // it easier to read the contents backwards

            bytes = BitConverter.GetBytes((int)record.type);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);

            bytes = BitConverter.GetBytes(record.data.Length);
            bytes.CopyTo(buffer, offset);
            offset += sizeof(int);



            file.Write(buffer);
            file.Flush();

        }

        public Record? ReadRecord(ReadDirection direction = ReadDirection.Forward)
        {
            int blockHeaderSize = 2 * sizeof(int);
            int minimumBlockSize = 2 * blockHeaderSize;

            if (direction == ReadDirection.Backwards && file.Position <= signatureBytes.Length)
            {
                // No block before current position

                return null;
            }
            else if (direction == ReadDirection.Backwards)
            {

                Debug.Assert(file.Position >= signatureBytes.Length + minimumBlockSize, "Story: found too small block reading backwards");

                Record r = new();

                file.Seek(-blockHeaderSize, SeekOrigin.Current);

                byte[] bytes = new byte[sizeof(int)];
                Utils.ReadAllBytes(file, bytes);

                r.type = (RecordType)BitConverter.ToInt32(bytes);

                int blockDataLength;

                bytes = new byte[sizeof(int)];
                Utils.ReadAllBytes(file, bytes);

                blockDataLength = BitConverter.ToInt32(bytes);

                Debug.Assert(file.Position >= signatureBytes.Length + blockHeaderSize + blockDataLength, "Story: Block data not found when reading backwards");

                // Place the file pointer at the beginning of the block data

                file.Seek(-(blockHeaderSize + blockDataLength), SeekOrigin.Current);

                bytes = new byte[blockDataLength];
                Utils.ReadAllBytes(file, bytes);

                r.data = bytes;

                // Place the file pointer at the beginning of the block we just read

                file.Seek(-(blockHeaderSize + blockDataLength), SeekOrigin.Current);

                return r;
            }
            else if (direction == ReadDirection.Forward && file.Position == file.Length)
            {
                // Already at end of file

                return null;
            }
            else // direction == ReadDirection.Forward
            {
                Debug.Assert(file.Position <= file.Length - minimumBlockSize, "Story: found too small block reading forwards");

                Record r = new();

                byte[] bytes = new byte[sizeof(int)];
                Utils.ReadAllBytes(file, bytes);

                r.type = (RecordType)BitConverter.ToInt32(bytes);

                int blockDataLength;

                bytes = new byte[sizeof(int)];
                Utils.ReadAllBytes(file, bytes);

                blockDataLength = BitConverter.ToInt32(bytes);

                Debug.Assert(file.Position <= file.Length - blockDataLength - blockHeaderSize, "Story: no room for block data when reading forward");

                bytes = new byte[blockDataLength];
                Utils.ReadAllBytes(file, bytes);

                r.data = bytes;

                // Place the file pointer at the end of the block

                file.Seek(blockHeaderSize, SeekOrigin.Current);

                return r;

            }
        }

        public void AddCreateFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.CreateFile;

            BinaryBufferWriter buffer = new();

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

            BinaryBufferWriter buffer = new();

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

            BinaryBufferWriter buffer = new();

            buffer.WriteString(name);

            record.data = buffer.GetBytes();

            AddRecord(record);

            OnDeleteFile?.Invoke(name);
        }

        public string AddLoadFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string? result = OnLoadFile?.Invoke(name);

            Debug.Assert(result != null);

            Record record = new();

            record.type = RecordType.LoadFile;

            BinaryBufferWriter buffer = new();

            buffer.WriteString(name);
            buffer.WriteString(result);

            record.data = buffer.GetBytes();

            AddRecord(record);


            return result;
        }

        public void AddSaveFile(string name, string text, FileChangeData[] changes)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.SaveFile;

            BinaryBufferWriter buffer = new();

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

        public string AddExecuteCommand(string command)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string result = OnExecuteCommand?.Invoke(command) ?? "";

            Record record = new();

            record.type = RecordType.ExecuteCommand;

            BinaryBufferWriter buffer = new();

            buffer.WriteString(command);
            buffer.WriteString(result);

            record.data = buffer.GetBytes();

            AddRecord(record);

            return result;


        }

        public void AddQuestion(QuestionData question)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            question = OnQuestion?.Invoke(question) ?? new QuestionData();

            Record record = new();

            record.type = RecordType.Question;

            BinaryBufferWriter buffer = new();

            buffer.WriteInt((int)question.type);
            buffer.WriteString(question.text);

            buffer.WriteString(question.context.fileName);

            buffer.WriteInt(question.context.lines.Length);
            for (int i = 0; i < question.context.lines.Length; i++)
            {
                Story.QuestionContextLineData line = question.context.lines[i];
                buffer.WriteInt(line.number);
                buffer.WriteString(line.text);
            }

            buffer.WriteInt(question.context.currentLine);
            buffer.WriteString(question.context.command);

            buffer.WriteString(question.answer);
            buffer.WriteInt(question.usedTime);
            buffer.WriteInt(question.timeLimit);

            record.data = buffer.GetBytes();

            AddRecord(record);


        }

        public void AddOpenStory()
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append);

            PersistentData p = new();

            long restore = file.Position;
            Record? previous = ReadRecord(ReadDirection.Backwards);
            file.Position = restore;

            if (previous.HasValue)
            {
                if (previous.Value.type == RecordType.CloseStory)
                {
                    BinaryBufferReader buffer = new(previous.Value.data);

                    p.instructorCharactersCounter = buffer.ReadInt();
                    p.instructorCharactersWait = buffer.ReadInt();
                    p.instructorCommandsCounter = buffer.ReadInt();
                    p.instructorCommandsWait = buffer.ReadInt();
                    p.instructorOpensCounter = buffer.ReadInt();
                    p.instructorOpensWait = buffer.ReadInt();
                    p.instructorSavesCounter = buffer.ReadInt();
                    p.instructorSavesWait = buffer.ReadInt();

                    Console.WriteLine("Recovered CharsCounter " + p.instructorCharactersCounter + " CharsWait " + p.instructorCharactersWait);
                    Console.WriteLine("Recovered CommandsCounter " + p.instructorCommandsCounter + " CommandsWait " + p.instructorCommandsWait);
                    Console.WriteLine("Recovered OpensCounter " + p.instructorOpensCounter + " OpensWait " + p.instructorOpensWait);
                    Console.WriteLine("Recovered SavesCounter " + p.instructorOpensCounter + " SavesWait " + p.instructorSavesWait);

                }
                else
                {
                    Console.WriteLine("No previous close block found");
                }
            }

            OnOpenStory?.Invoke(p);

            Record record = new();

            record.type = RecordType.OpenStory;
            record.data = Array.Empty<byte>();

            AddRecord(record);
        }

        public void AddCloseStory()
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            PersistentData result = OnCloseStory?.Invoke() ?? new PersistentData();

            Record record = new();

            record.type = RecordType.CloseStory;

            BinaryBufferWriter buffer = new();

            buffer.WriteInt(result.instructorCharactersCounter);
            buffer.WriteInt(result.instructorCharactersWait);
            buffer.WriteInt(result.instructorCommandsCounter);
            buffer.WriteInt(result.instructorCommandsWait);
            buffer.WriteInt(result.instructorOpensCounter);
            buffer.WriteInt(result.instructorOpensWait);
            buffer.WriteInt(result.instructorSavesCounter);
            buffer.WriteInt(result.instructorSavesWait);

            record.data = buffer.GetBytes();

            AddRecord(record);

        }

        public void Dump()
        {
            long previousPos = file.Position;

            file.Seek(0, SeekOrigin.Begin);

            byte[] bytes = new byte[signatureBytes.Length];
            Utils.ReadAllBytes(file, bytes);

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            string signature = encoding.GetString(bytes);

            Console.WriteLine("Signature: " + signature);

            int blockCounter = 0;

            Record? record;
            do
            {
                record = ReadRecord();

                if (record.HasValue)
                {
                    Console.WriteLine("Block " + blockCounter + ": type " + record.Value.type + " data size " + record.Value.data.Length);

                    if (record.Value.type == RecordType.Header)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        string guid = reader.ReadString();
                        string publicKey = reader.ReadString();
                        string author = reader.ReadString();

                        Console.WriteLine("    GUID       : " + guid);
                        Console.WriteLine("    Public key : " + publicKey);
                        Console.WriteLine("    Author     : " + author);
                    }
                    else if (record.Value.type == RecordType.LoadFile)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        string fileName = reader.ReadString();
                        string result = reader.ReadString();

                        Console.WriteLine("    File       : " + fileName);
                        Console.WriteLine("    Loaded     : " + result.Length + " chars");

                    }
                    else if (record.Value.type == RecordType.SaveFile)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        string fileName = reader.ReadString();
                        int changesCount = reader.ReadInt();

                        for (int i = 0; i < changesCount; i++)
                        {
                            reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadString();
                            reader.ReadInt();
                            reader.ReadLong();

                        }

                        string result = reader.ReadString();

                        Console.WriteLine("    File       : " + fileName);
                        Console.WriteLine("    Changes    : " + changesCount);
                        Console.WriteLine("    Saved      : " + result.Length + " chars");

                    }
                    else if (record.Value.type == RecordType.CreateFile ||
                            record.Value.type == RecordType.DeleteFile)
                    {
                        BinaryBufferReader reader = new(record.Value.data);
                        string fileName = reader.ReadString();
                        Console.WriteLine("    File       : " + fileName);
                    }
                    else if (record.Value.type == RecordType.CreateDirectory ||
                             record.Value.type == RecordType.DeleteDirectory)
                    {
                        BinaryBufferReader reader = new(record.Value.data);
                        string fileName = reader.ReadString();
                        Console.WriteLine("    Directory  : " + fileName);
                    }
                    else if (record.Value.type == RecordType.ExecuteCommand)
                    {
                        BinaryBufferReader reader = new(record.Value.data);
                        string command = reader.ReadString();
                        string result = reader.ReadString();
                        Console.WriteLine("    Command    : " + command);
                        Console.WriteLine("    Result     : " + result.Length + " chars");
                    }
                    else if (record.Value.type == RecordType.CloseStory)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        int instructorCharactersCounter = reader.ReadInt();
                        int instructorCharactersWait = reader.ReadInt();
                        int instructorCommandsCounter = reader.ReadInt();
                        int instructorCommandsWait = reader.ReadInt();
                        int instructorOpensCounter = reader.ReadInt();
                        int instructorOpensWait = reader.ReadInt();
                        int instructorSavesCounter = reader.ReadInt();
                        int instructorSavesWait = reader.ReadInt();

                        Console.WriteLine("    CharsCount : " + instructorCharactersCounter);
                        Console.WriteLine("    CharsWait  : " + instructorCharactersWait);
                        Console.WriteLine("    CommandCount : " + instructorCommandsCounter);
                        Console.WriteLine("    CommandWait  : " + instructorCommandsWait);
                        Console.WriteLine("    OpensCount : " + instructorOpensCounter);
                        Console.WriteLine("    OpensWait  : " + instructorOpensWait);
                        Console.WriteLine("    SavesCount : " + instructorSavesCounter);
                        Console.WriteLine("    SavesWait  : " + instructorSavesWait);

                    }
                    else if (record.Value.type == RecordType.OpenStory)
                    {
                        // Nothing
                    }
                    else if (record.Value.type == RecordType.Question)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        QuestionType type = (QuestionType)reader.ReadInt();
                        string text = reader.ReadString();

                        string fileName = reader.ReadString();

                        int linesCount = reader.ReadInt();
                        for (int i = 0; i < linesCount; i++)
                        {
                            reader.ReadInt();
                            reader.ReadString();
                        }

                        int currentLine = reader.ReadInt();
                        string command = reader.ReadString();

                        string answer = reader.ReadString();
                        int usedTime = reader.ReadInt();
                        int timeLimit = reader.ReadInt();

                        Console.WriteLine("    Type       : " + type);
                        Console.WriteLine("    Text       : " + text);
                        Console.WriteLine("    CtxFilename: " + fileName);
                        Console.WriteLine("    CtxLines   : " + linesCount);
                        Console.WriteLine("    CtxCurrentLine : " + currentLine);
                        Console.WriteLine("    CtxCommand : " + command);
                        Console.WriteLine("    Answer     : " + answer);
                        Console.WriteLine("    UsedTime   : " + usedTime);
                        Console.WriteLine("    TimeLimit  : " + timeLimit);

                    }



                    blockCounter++;
                }
            }
            while (record.HasValue);

            file.Position = previousPos;
        }

        public void Dispose()
        {
            file.Dispose();

            disposed = true;
        }



    }
}
