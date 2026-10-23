using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public partial class  RecordData
    {

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

        public static QuestionData DecodeQuestionRecord(Story.Record r)
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


    }
}
