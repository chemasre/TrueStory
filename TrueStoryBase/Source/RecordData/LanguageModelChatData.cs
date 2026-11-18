using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public partial class  RecordData
    {
        public struct LanguageModelChatLineData
        {
            public bool isUser;
            public string text;
        }

        public struct LanguageModelChatData
        {
            public LanguageModelChatLineData[] lines;

            public LanguageModelChatData()
            {
                lines = new LanguageModelChatLineData[0];
            }

        }

        public static LanguageModelChatData DecodeLanguageModelChatRecord(Story.Record r)
        {
            LanguageModelChatData data = new();

            BinaryBufferReader reader = new(r.data);

            int linesCount = reader.ReadInt();

            List<LanguageModelChatLineData> lines = new();

            for (int i = 0; i < linesCount; i++)
            {
                LanguageModelChatLineData lineData = new();

                lineData.isUser = (reader.ReadByte() != 0);
                lineData.text = reader.ReadString();

                lines.Add(lineData);
            }

            data.lines = lines.ToArray();

            return data;
        }

    }
}
