using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class BinaryBuffer : IDisposable
    {
        BinaryWriter writer;
        MemoryStream stream;

        public BinaryBuffer()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
        }

        public void WriteInt(int i)
        {
            writer.Write(i);
        }

        public void WriteString(String s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            byte[] bytesLength = BitConverter.GetBytes(bytes.Length);
            writer.Write(bytesLength);
            writer.Write(bytes);
        }

        public byte[] GetBytes()
        {
            writer.Flush();

            return stream.ToArray();
        }

        public void Dispose()
        {
            writer.Dispose();
            stream.Dispose();
        }
    }
}
