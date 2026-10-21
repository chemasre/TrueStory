using System.Diagnostics;
using System.IO;

namespace TrueStory
{
    public class Log : IDisposable
    {
        StreamWriter? writer;

        public Log(string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Append, FileAccess.Write);
            writer = new StreamWriter(stream);
        }

        public void Add(string message)
        {

            Debug.Assert(writer != null);

            writer.WriteLine(message);
            writer.Flush();
        }

        public void Dispose()
        {

            Debug.Assert(writer != null);

            writer.Dispose();
        }
    }
}
