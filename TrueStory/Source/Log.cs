using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public class Log : IDisposable
    {
        StreamWriter? writer;

        public Log(string filename)
        {
            if (Switches.NoLog) { return;  }

            FileStream stream = new FileStream(filename, FileMode.Append, FileAccess.Write);
            writer = new StreamWriter(stream);
        }

        public void Add(string message)
        {
            if (Switches.NoLog) { return; }

            Debug.Assert(writer != null);

            writer.WriteLine(message);
            writer.Flush();
        }

        public void Dispose()
        {
            if (Switches.NoLog) { return;  }

            Debug.Assert(writer != null);

            writer.Dispose();
        }
    }
}
