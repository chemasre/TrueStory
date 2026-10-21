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
            public string guid;
            public string publicKey;
            public string author;
        }

        public struct LoadFileData
        {
            public string fileName;
            public string text;
        }

        public struct SaveFileData
        {
            public string fileName;
            public string text;
            public FileChangeData[] changes;
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

        public struct CreateFileData
        {
            public string fileName;
        }

        public struct DeleteFileData
        {
            public string fileName;
        }

        public struct CreateFolderData
        {
            public string folderName;
        }

        public struct DeleteFolderData
        {
            public string folderName;
        }

        public struct ExecuteCommandData
        {
            public string name;
            public string result;
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

        public struct CloseStoryData
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

        public event OpenStoryAddedHandler? OnAddedOpenStory;
        public event CreateFileAddedHandler? OnAddedCreateFile;
        public event CreateFolderAddedHandler? OnAddedCreateFolder;
        public event DeleteFileAddedHandler? OnAddedDeleteFile;
        public event DeleteFolderAddedHandler? OnAddedDeleteFolder;
        public event LoadFileAddedHandler? OnAddedLoadFile;
        public event SaveFileAddedHandler? OnAddedSaveFile;
        public event ExecuteCommandAddedHandler? OnAddedExecuteCommand;
        public event QuestionAddedHandler? OnAddedQuestion;
        public event CloseStoryAddedHandler? OnAddedCloseStory;


        public delegate void OpenStoryAddedHandler(CloseStoryData data);
        public delegate void CreateFileAddedHandler(string name);
        public delegate void CreateFolderAddedHandler(string name);
        public delegate void DeleteFileAddedHandler(string name);
        public delegate void DeleteFolderAddedHandler(string name);
        public delegate string LoadFileAddedHandler(string name);
        public delegate void SaveFileAddedHandler(string name, string text);
        public delegate string ExecuteCommandAddedHandler(string name);
        public delegate QuestionData QuestionAddedHandler(QuestionData q);
        public delegate CloseStoryData CloseStoryAddedHandler();

        public event HeaderReadedHandler? OnReadedHeader;
        public event OpenStoryReadedHandler? OnReadedOpenStory;
        public event CreateFileReadedHandler? OnReadedCreateFile;
        public event CreateFolderReadedHandler? OnReadedCreateFolder;
        public event DeleteFileReadedHandler? OnReadedDeleteFile;
        public event DeleteFolderReadedHandler? OnReadedDeleteFolder;
        public event LoadFileReadedHandler? OnReadedLoadFile;
        public event SaveFileReadedHandler? OnReadedSaveFile;
        public event ExecuteCommandReadedHandler? OnReadedExecuteCommand;
        public event QuestionReadedHandler? OnReadedQuestion;
        public event CloseStoryReadedHandler? OnReadedCloseStory;

        public delegate void HeaderReadedHandler(HeaderData data);
        public delegate void OpenStoryReadedHandler();
        public delegate void CreateFileReadedHandler(CreateFileData data);
        public delegate void CreateFolderReadedHandler(CreateFolderData data);
        public delegate void DeleteFileReadedHandler(DeleteFileData data);
        public delegate void DeleteFolderReadedHandler(DeleteFolderData data);
        public delegate void LoadFileReadedHandler(LoadFileData data);
        public delegate void SaveFileReadedHandler(SaveFileData data);
        public delegate void ExecuteCommandReadedHandler(ExecuteCommandData data);
        public delegate void QuestionReadedHandler(QuestionData q);
        public delegate void CloseStoryReadedHandler(CloseStoryData data);

        public const string StoryFilename = ".truestory";

        public string RootPath { get { return rootPath;  } }

        public long TotalRecords
        {
            get
            {
                Debug.Assert(mode == Mode.Read);

                return readTotalRecords;
            }
        }

        public long RecordIndex
        {
            get
            {
                Debug.Assert(mode == Mode.Read);

                return readRecordIndex;
            }
        }


        FileStream file;
        string rootPath;
        Mode mode;

        byte[] signatureBytes;

        bool internalCall;

        long readRecordIndex;
        long readTotalRecords;

        bool disposed;

        public Story(string _rootPath, Mode _mode, HeaderData? header = null)
        {
            rootPath = _rootPath;
            mode = _mode;

            string filePath = _rootPath + Path.DirectorySeparatorChar + StoryFilename;

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            signatureBytes = encoding.GetBytes(Story.signature);

            internalCall = false;

            Debug.Assert(!(_mode == Mode.Create && header == null), "Story: No header provided");

            if (_mode == Mode.Create)
            {
                Debug.Assert(header.HasValue);

                if (header.Value.guid == null)
                {
                    HeaderData h = header.Value;
                    h.guid = Guid.NewGuid().ToString();
                    header = h;
                }

                file = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite);

                AddSignature();

                Record r = new();
                r.type = RecordType.Header;

                BinaryBufferWriter buffer = new();

                buffer.WriteString(header.Value.guid);
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

                string foundSignature = ReadSignature();
                if (signature != foundSignature) { throw new InvalidDataException(); }

                readRecordIndex = 0;
                readTotalRecords = CountRecords();

            }

        }

        long CountRecords()
        {
            long count = 0;

            long previousPosition = file.Position;

            file.Seek(signature.Length, SeekOrigin.Begin);

            internalCall = true;

            Record? r;
            do
            {
                r = ReadRecord();
                if (r.HasValue) { count++; }
            } while (r.HasValue);

            internalCall = false;

            file.Position = previousPosition;

            return count;
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

            OnAddedCreateFolder?.Invoke(name);

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

        public HeaderData DecodeHeaderRecord(Record r)
        {
            HeaderData data = new();

            BinaryBufferReader buffer = new(r.data);

            data.guid = buffer.ReadString();
            data.publicKey = buffer.ReadString();
            data.author = buffer.ReadString();

            return data;
        }

        public LoadFileData DecodeLoadFileRecord(Record r)
        {
            LoadFileData data = new();

            BinaryBufferReader buffer = new(r.data);

            data.fileName = buffer.ReadString();
            data.text = buffer.ReadString();

            return data;
        }

        public SaveFileData DecodeSaveFileRecord(Record r)
        {
            SaveFileData data = new();

            BinaryBufferReader reader = new(r.data);

            data.fileName = reader.ReadString();
            int changesCount = reader.ReadInt();

            List<FileChangeData> changes = new();

            for (int i = 0; i < changesCount; i++)
            {
                FileChangeData changeData = new();

                changeData.Offset = reader.ReadInt();
                changeData.Added = reader.ReadInt();
                changeData.AddedText = reader.ReadString();
                changeData.Removed = reader.ReadInt();
                changeData.DeltaTime = reader.ReadLong();

                changes.Add(changeData);

            }

            data.changes = changes.ToArray();
            data.text = reader.ReadString();

            return data;

        }

        public CreateFileData DecodeCreateFileRecord(Record r)
        {
            CreateFileData data = new();

            BinaryBufferReader reader = new(r.data);
            data.fileName = reader.ReadString();

            return data;
        }

        public DeleteFileData DecodeDeleteFileRecord(Record r)
        {
            DeleteFileData data = new();

            BinaryBufferReader reader = new(r.data);
            data.fileName = reader.ReadString();

            return data;
        }

        public CreateFolderData DecodeCreateFolderRecord(Record r)
        {
            CreateFolderData data = new();

            BinaryBufferReader reader = new(r.data);
            data.folderName = reader.ReadString();

            return data;
        }

        public DeleteFolderData DecodeDeleteFolderRecord(Record r)
        {
            DeleteFolderData data = new();

            BinaryBufferReader reader = new(r.data);
            data.folderName = reader.ReadString();

            return data;
        }

        public ExecuteCommandData DecodeExecuteCommandRecord(Record r)
        {
            ExecuteCommandData data = new();

            BinaryBufferReader reader = new(r.data);
            data.name = reader.ReadString();
            data.result = reader.ReadString();

            return data;
        }

        public QuestionData DecodeQuestionRecord(Record r)
        {
            QuestionData data = new();

            BinaryBufferReader reader = new(r.data);

            data.type = (QuestionType)reader.ReadInt();
            data.text = reader.ReadString();

            QuestionContextData contextData = new();

            contextData.fileName = reader.ReadString();

            List<QuestionContextLineData> lines = new();

            int linesCount = reader.ReadInt();
            for (int i = 0; i < linesCount; i++)
            {
                QuestionContextLineData lineData = new();

                lineData.number = reader.ReadInt();
                lineData.text = reader.ReadString();

                lines.Add(lineData);
            }

            contextData.lines = lines.ToArray();

            contextData.currentLine = reader.ReadInt();
            contextData.command = reader.ReadString();

            data.context = contextData;

            data.answer = reader.ReadString();
            data.usedTime = reader.ReadInt();
            data.timeLimit = reader.ReadInt();


            return data;
        }

        public CloseStoryData DecodeCloseStoryRecord(Record r)
        {
            CloseStoryData data = new();

            BinaryBufferReader reader = new(r.data);

            data.instructorCharactersCounter = reader.ReadInt();
            data.instructorCharactersWait = reader.ReadInt();
            data.instructorCommandsCounter = reader.ReadInt();
            data.instructorCommandsWait = reader.ReadInt();
            data.instructorOpensCounter = reader.ReadInt();
            data.instructorOpensWait = reader.ReadInt();
            data.instructorSavesCounter = reader.ReadInt();
            data.instructorSavesWait = reader.ReadInt();


            return data;

        }

        public Record? ReadRecord(ReadDirection direction = ReadDirection.Forward)
        {
            int blockHeaderSize = 2 * sizeof(int);
            int minimumBlockSize = 2 * blockHeaderSize;

            Record? result;

            if (direction == ReadDirection.Backwards && file.Position <= signatureBytes.Length)
            {
                // No block before current position

                result = null;
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

                if (!internalCall)
                {
                    readRecordIndex--;
                }

                result = r;
            }
            else if (direction == ReadDirection.Forward && file.Position == file.Length)
            {
                // Already at end of file

                result = null;
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

                if (!internalCall)
                {
                    readRecordIndex++;
                }

                result = r;

            }

            if (!internalCall && result.HasValue)
            {
                if (result.Value.type == RecordType.Header)
                {
                    HeaderData d = DecodeHeaderRecord(result.Value);
                    OnReadedHeader?.Invoke(d);
                }
                else if (result.Value.type == RecordType.LoadFile)
                {
                    LoadFileData d = DecodeLoadFileRecord(result.Value);
                    OnReadedLoadFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.SaveFile)
                {
                    SaveFileData d = DecodeSaveFileRecord(result.Value);
                    OnReadedSaveFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.CreateFile)
                {
                    CreateFileData d = DecodeCreateFileRecord(result.Value);
                    OnReadedCreateFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.DeleteFile)
                {
                    DeleteFileData d = DecodeDeleteFileRecord(result.Value);
                    OnReadedDeleteFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.CreateDirectory)
                {
                    CreateFolderData d = DecodeCreateFolderRecord(result.Value);
                    OnReadedCreateFolder?.Invoke(d);
                }
                else if (result.Value.type == RecordType.DeleteDirectory)
                {
                    DeleteFolderData d = DecodeDeleteFolderRecord(result.Value);
                    OnReadedDeleteFolder?.Invoke(d);
                }
                else if (result.Value.type == RecordType.ExecuteCommand)
                {
                    ExecuteCommandData d = DecodeExecuteCommandRecord(result.Value);
                    OnReadedExecuteCommand?.Invoke(d);
                }
                else if (result.Value.type == RecordType.Question)
                {
                    QuestionData d = DecodeQuestionRecord(result.Value);
                    OnReadedQuestion?.Invoke(d);
                }
                else if (result.Value.type == RecordType.CloseStory)
                {
                    CloseStoryData d = DecodeCloseStoryRecord(result.Value);
                    OnReadedCloseStory?.Invoke(d);
                }
                else // result.Value.type == RecordType.OpenStory
                {
                    OnReadedOpenStory?.Invoke();
                }


                // CONTINUE
            }

            return result;
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

            OnAddedCreateFile?.Invoke(name);

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

            OnAddedDeleteFolder?.Invoke(name);
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

            OnAddedDeleteFile?.Invoke(name);
        }

        public string AddLoadFile(string name)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string? result = OnAddedLoadFile?.Invoke(name);

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

            OnAddedSaveFile?.Invoke(name, text);
        }

        public string AddExecuteCommand(string command)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            string result = OnAddedExecuteCommand?.Invoke(command) ?? "";

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

            question = OnAddedQuestion?.Invoke(question) ?? new QuestionData();

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

            CloseStoryData p = new();

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

                    Utils.Log("Recovered CharsCounter " + p.instructorCharactersCounter + " CharsWait " + p.instructorCharactersWait);
                    Utils.Log("Recovered CommandsCounter " + p.instructorCommandsCounter + " CommandsWait " + p.instructorCommandsWait);
                    Utils.Log("Recovered OpensCounter " + p.instructorOpensCounter + " OpensWait " + p.instructorOpensWait);
                    Utils.Log("Recovered SavesCounter " + p.instructorOpensCounter + " SavesWait " + p.instructorSavesWait);

                }
                else
                {
                    Utils.Log("No previous close block found");
                }
            }

            OnAddedOpenStory?.Invoke(p);

            Record record = new();

            record.type = RecordType.OpenStory;
            record.data = Array.Empty<byte>();

            AddRecord(record);
        }

        public void AddCloseStory()
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            CloseStoryData result = OnAddedCloseStory?.Invoke() ?? new CloseStoryData();

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

            internalCall = true;

            file.Seek(0, SeekOrigin.Begin);

            byte[] bytes = new byte[signatureBytes.Length];
            Utils.ReadAllBytes(file, bytes);

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            string signature = encoding.GetString(bytes);

            Utils.Log("Signature: " + signature);

            int blockCounter = 0;

            Record? record;
            do
            {
                record = ReadRecord();

                if (record.HasValue)
                {
                    Utils.Log("Block " + blockCounter + ": type " + record.Value.type + " data size " + record.Value.data.Length);

                    if (record.Value.type == RecordType.Header)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        string guid = reader.ReadString();
                        string publicKey = reader.ReadString();
                        string author = reader.ReadString();

                        Utils.Log("    GUID       : " + guid);
                        Utils.Log("    Public key : " + publicKey);
                        Utils.Log("    Author     : " + author);
                    }
                    else if (record.Value.type == RecordType.LoadFile)
                    {
                        BinaryBufferReader reader = new(record.Value.data);

                        string fileName = reader.ReadString();
                        string result = reader.ReadString();

                        Utils.Log("    File       : " + fileName);
                        Utils.Log("    Loaded     : " + result.Length + " chars");

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

                        Utils.Log("    File       : " + fileName);
                        Utils.Log("    Changes    : " + changesCount);
                        Utils.Log("    Saved      : " + result.Length + " chars");

                    }
                    else if (record.Value.type == RecordType.CreateFile ||
                            record.Value.type == RecordType.DeleteFile)
                    {
                        BinaryBufferReader reader = new(record.Value.data);
                        string fileName = reader.ReadString();
                        Utils.Log("    File       : " + fileName);
                    }
                    else if (record.Value.type == RecordType.CreateDirectory ||
                             record.Value.type == RecordType.DeleteDirectory)
                    {
                        BinaryBufferReader reader = new(record.Value.data);
                        string fileName = reader.ReadString();
                        Utils.Log("    Directory  : " + fileName);
                    }
                    else if (record.Value.type == RecordType.ExecuteCommand)
                    {
                        BinaryBufferReader reader = new(record.Value.data);
                        string command = reader.ReadString();
                        string result = reader.ReadString();
                        Utils.Log("    Command    : " + command);
                        Utils.Log("    Result     : " + result.Length + " chars");
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

                        Utils.Log("    CharsCount : " + instructorCharactersCounter);
                        Utils.Log("    CharsWait  : " + instructorCharactersWait);
                        Utils.Log("    CommandCount : " + instructorCommandsCounter);
                        Utils.Log("    CommandWait  : " + instructorCommandsWait);
                        Utils.Log("    OpensCount : " + instructorOpensCounter);
                        Utils.Log("    OpensWait  : " + instructorOpensWait);
                        Utils.Log("    SavesCount : " + instructorSavesCounter);
                        Utils.Log("    SavesWait  : " + instructorSavesWait);

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

                        Utils.Log("    Type       : " + type);
                        Utils.Log("    Text       : " + text);
                        Utils.Log("    CtxFilename: " + fileName);
                        Utils.Log("    CtxLines   : " + linesCount);
                        Utils.Log("    CtxCurrentLine : " + currentLine);
                        Utils.Log("    CtxCommand : " + command);
                        Utils.Log("    Answer     : " + answer);
                        Utils.Log("    UsedTime   : " + usedTime);
                        Utils.Log("    TimeLimit  : " + timeLimit);

                    }



                    blockCounter++;
                }
            }
            while (record.HasValue);

            internalCall = false;

            file.Position = previousPos;
        }

        public void Dispose()
        {
            file.Dispose();

            disposed = true;
        }



    }
}
