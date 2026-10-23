using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public partial class  RecordData
    {
        public struct ExecuteCommandData
        {
            public string name;
            public string result;
        }

        public struct ExecutedCommandData
        {
            public string command;
            public string output;
        }

        public static ExecuteCommandData DecodeExecuteCommandRecord(Story.Record r)
        {
            ExecuteCommandData data = new();

            BinaryBufferReader reader = new(r.data);
            data.name = reader.ReadString();
            data.result = reader.ReadString();

            return data;
        }

    }
}
