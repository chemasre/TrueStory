using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public partial class  RecordData
    {
        public struct LoadFileData
        {
            public string fileName;
            public string text;
        }

        public struct FileChangeData
        {
            public int Offset;
            public int Added;
            public string AddedText;
            public int Removed;
            public long DeltaTime;
        }

        public struct SaveFileData
        {
            public string fileName;
            public string text;
            public FileChangeData[] changes;
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


        public static LoadFileData DecodeLoadFileRecord(Story.Record r)
        {
            LoadFileData data = new();

            BinaryBufferReader buffer = new(r.data);

            data.fileName = buffer.ReadString();
            data.text = buffer.ReadString();

            return data;
        }

        public static SaveFileData DecodeSaveFileRecord(Story.Record r)
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

        public static CreateFileData DecodeCreateFileRecord(Story.Record r)
        {
            CreateFileData data = new();

            BinaryBufferReader reader = new(r.data);
            data.fileName = reader.ReadString();

            return data;
        }

        public static DeleteFileData DecodeDeleteFileRecord(Story.Record r)
        {
            DeleteFileData data = new();

            BinaryBufferReader reader = new(r.data);
            data.fileName = reader.ReadString();

            return data;
        }

        public static CreateFolderData DecodeCreateFolderRecord(Story.Record r)
        {
            CreateFolderData data = new();

            BinaryBufferReader reader = new(r.data);
            data.folderName = reader.ReadString();

            return data;
        }

        public static DeleteFolderData DecodeDeleteFolderRecord(Story.Record r)
        {
            DeleteFolderData data = new();

            BinaryBufferReader reader = new(r.data);
            data.folderName = reader.ReadString();

            return data;
        }

    }

}
