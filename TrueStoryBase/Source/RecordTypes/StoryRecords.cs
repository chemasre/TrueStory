using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public partial class RecordData
    {
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

        public static CloseStoryData DecodeCloseStoryRecord(Story.Record r)
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

    }
}
