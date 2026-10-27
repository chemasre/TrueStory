using System.IO;
using System.Text;

namespace TrueStory
{
    public class BinaryBufferWriter : IDisposable
    {
        BinaryWriter writer;
        MemoryStream stream;

        public BinaryBufferWriter()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
        }

        public void WriteInt(int n)
        {
            writer.Write(n);
        }

        public void WriteLong(long n)
        {
            writer.Write(n);
        }

        public void WriteString(String s)
        {
            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            byte[] bytes = encoding.GetBytes(s);
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
