using System.Diagnostics;
using System.IO;
using System.Text;

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


        public delegate void OpenStoryAddedHandler(RecordData.CloseStoryData data);
        public delegate void CreateFileAddedHandler(string name);
        public delegate void CreateFolderAddedHandler(string name);
        public delegate void DeleteFileAddedHandler(string name);
        public delegate void DeleteFolderAddedHandler(string name);
        public delegate string LoadFileAddedHandler(string name);
        public delegate void SaveFileAddedHandler(string name, string text);
        public delegate string ExecuteCommandAddedHandler(string name);
        public delegate RecordData.QuestionData QuestionAddedHandler(RecordData.QuestionData q);
        public delegate RecordData.CloseStoryData CloseStoryAddedHandler();

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

        public delegate void HeaderReadedHandler(RecordData.NewHeaderData data);
        public delegate void OpenStoryReadedHandler();
        public delegate void CreateFileReadedHandler(RecordData.CreateFileData data);
        public delegate void CreateFolderReadedHandler(RecordData.CreateFolderData data);
        public delegate void DeleteFileReadedHandler(RecordData.DeleteFileData data);
        public delegate void DeleteFolderReadedHandler(RecordData.DeleteFolderData data);
        public delegate void LoadFileReadedHandler(RecordData.LoadFileData data);
        public delegate void SaveFileReadedHandler(RecordData.SaveFileData data);
        public delegate void ExecuteCommandReadedHandler(RecordData.ExecuteCommandData data);
        public delegate void QuestionReadedHandler(RecordData.QuestionData q);
        public delegate void CloseStoryReadedHandler(RecordData.CloseStoryData data);

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

        public ref readonly RecordData.NewHeaderData Header { get { return ref header;  } }

        FileStream file;
        string rootPath;
        Mode mode;

        byte[] signatureBytes;

        bool internalCall;

        long readRecordIndex;
        long readTotalRecords;

        RecordData.NewHeaderData header;

        bool disposed;

        public Story(string _rootPath, Mode _mode, RecordData.NewHeaderData? _header = null)
        {
            rootPath = _rootPath;
            mode = _mode;

            string filePath = _rootPath + Path.DirectorySeparatorChar + StoryFilename;

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            signatureBytes = encoding.GetBytes(Story.signature);

            internalCall = false;

            Debug.Assert(!(_mode == Mode.Create && _header == null), "Story: No header provided");

            if (_mode == Mode.Create)
            {
                file = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite);

                AddSignature();

                Debug.Assert(_header != null);

                Record r = RecordData.EncodeNewHeaderRecord(_header.Value);

                AddRecord(r);

                header = _header.Value;

            }
            else if (_mode == Mode.Append)
            {
                file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);

                string foundSignature = ReadSignature();
                if (signature != foundSignature) { throw new InvalidDataException(); }

                Record? r = ReadRecord();

                if (r == null) { throw new InvalidDataException(); }

                header = RecordData.DecodeNewHeaderRecord(r.Value);

                file.Seek(0, SeekOrigin.End);

            }
            else // _mode == Mode.Read
            {
                file = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                string foundSignature = ReadSignature();
                if (signature != foundSignature) { throw new InvalidDataException(); }

                Record? r = ReadRecord();

                if (r == null) { throw new InvalidDataException(); }

                header = RecordData.DecodeNewHeaderRecord(r.Value);

                file.Seek(signatureBytes.Length, SeekOrigin.Begin);

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
                    RecordData.NewHeaderData d = RecordData.DecodeNewHeaderRecord(result.Value);
                    OnReadedHeader?.Invoke(d);
                }
                else if (result.Value.type == RecordType.LoadFile)
                {
                    RecordData.LoadFileData d = RecordData.DecodeLoadFileRecord(result.Value);
                    OnReadedLoadFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.SaveFile)
                {
                    RecordData.SaveFileData d = RecordData.DecodeSaveFileRecord(result.Value);
                    OnReadedSaveFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.CreateFile)
                {
                    RecordData.CreateFileData d = RecordData.DecodeCreateFileRecord(result.Value);
                    OnReadedCreateFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.DeleteFile)
                {
                    RecordData.DeleteFileData d = RecordData.DecodeDeleteFileRecord(result.Value);
                    OnReadedDeleteFile?.Invoke(d);
                }
                else if (result.Value.type == RecordType.CreateDirectory)
                {
                    RecordData.CreateFolderData d = RecordData.DecodeCreateFolderRecord(result.Value);
                    OnReadedCreateFolder?.Invoke(d);
                }
                else if (result.Value.type == RecordType.DeleteDirectory)
                {
                    RecordData.DeleteFolderData d = RecordData.DecodeDeleteFolderRecord(result.Value);
                    OnReadedDeleteFolder?.Invoke(d);
                }
                else if (result.Value.type == RecordType.ExecuteCommand)
                {
                    RecordData.ExecuteCommandData d = RecordData.DecodeExecuteCommandRecord(result.Value);
                    OnReadedExecuteCommand?.Invoke(d);
                }
                else if (result.Value.type == RecordType.Question)
                {
                    RecordData.QuestionData d = RecordData.DecodeQuestionRecord(result.Value);
                    OnReadedQuestion?.Invoke(d);
                }
                else if (result.Value.type == RecordType.CloseStory)
                {
                    RecordData.CloseStoryData d = RecordData.DecodeCloseStoryRecord(result.Value);
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

        public void AddSaveFile(string name, string text, RecordData.FileChangeData[] changes)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            Record record = new();

            record.type = RecordType.SaveFile;

            BinaryBufferWriter buffer = new();

            buffer.WriteString(name);
            buffer.WriteInt(changes.Length);

            foreach (RecordData.FileChangeData c in changes)
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

        public void AddQuestion(RecordData.QuestionData question)
        {
            Debug.Assert(!disposed);
            Debug.Assert(mode == Mode.Append || mode == Mode.Create);

            question = OnAddedQuestion?.Invoke(question) ?? new RecordData.QuestionData();

            Record record = new();

            record.type = RecordType.Question;

            BinaryBufferWriter buffer = new();

            buffer.WriteInt((int)question.type);
            buffer.WriteString(question.text);

            buffer.WriteString(question.context.fileName);

            buffer.WriteInt(question.context.lines.Length);
            for (int i = 0; i < question.context.lines.Length; i++)
            {
                RecordData.QuestionContextLineData line = question.context.lines[i];
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

            RecordData.CloseStoryData p = new();

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

            RecordData.CloseStoryData result = OnAddedCloseStory?.Invoke() ?? new RecordData.CloseStoryData();

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

                        RecordData.QuestionType type = (RecordData.QuestionType)reader.ReadInt();
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
