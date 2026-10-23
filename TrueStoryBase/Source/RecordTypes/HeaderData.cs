using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public partial class  RecordData
    {
        public struct HeaderData
        {
            public string guid;
            public string publicKey;
            public string author;
        }

        public static HeaderData DecodeHeaderRecord(Story.Record r)
        {
            HeaderData data = new();

            BinaryBufferReader buffer = new(r.data);

            data.guid = buffer.ReadString();
            data.publicKey = buffer.ReadString();
            data.author = buffer.ReadString();

            return data;
        }


    }
}
