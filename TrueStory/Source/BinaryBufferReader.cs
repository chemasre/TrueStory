using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class BinaryBufferReader : IDisposable
    {
        BinaryReader reader;
        MemoryStream stream;

        public BinaryBufferReader(byte[] data)
        {
            byte[] streamData = new byte[data.Length];
            Array.Copy(data, streamData, data.Length);

            stream = new MemoryStream(streamData);
            reader = new BinaryReader(stream);
        }

        public int ReadInt()
        {
            return reader.ReadInt32();
        }

        public long ReadLong()
        {
            return reader.ReadInt64();
        }

        public String ReadString()
        {
            String result;

            int bytesLength = reader.ReadInt32();
            byte[] bytes = new byte[bytesLength];
            reader.Read(bytes);

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            result = encoding.GetString(bytes);

            return result;

        }

        public void Dispose()
        {
            reader.Dispose();
            stream.Dispose();
        }
    }
}
