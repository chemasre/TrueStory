using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Text;

namespace TrueStory
{
    public class StoryActions : IDisposable
    {
        Story story;

        Encoding encoding;

        public StoryActions(Story _story)
        {
            story = _story;

            story.OnCreateFolder += OnCreateFolder;
            story.OnCreateFile += OnCreateFile;
            story.OnDeleteFile += OnDeleteFile;
            story.OnDeleteFolder += OnDeleteFolder;
            story.OnLoadFile += OnLoadFile;
            story.OnSaveFile += OnSaveFile;
            story.OnExecuteCommand += OnExecuteCommand;

            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }



        void OnDeleteFolder(string name)
        {
            Directory.Delete(story.RootPath + Path.DirectorySeparatorChar + name, true);
        }

        void OnDeleteFile(string name)
        {
            File.Delete(story.RootPath + Path.DirectorySeparatorChar + name);
        }

        void OnCreateFile(string name)
        {
            FileStream f = File.Create(story.RootPath + Path.DirectorySeparatorChar + name);
            f.Dispose();
        }

        void OnCreateFolder(string name)
        {
            Directory.CreateDirectory(story.RootPath + Path.DirectorySeparatorChar + name);

        }

        void OnSaveFile(string name, string text)
        {
            File.WriteAllText(story.RootPath + Path.DirectorySeparatorChar + name, text, encoding);
        }

        string OnLoadFile(string name)
        {
            string text = File.ReadAllText(story.RootPath + Path.DirectorySeparatorChar + name, encoding);

            return text;
        }

        string OnExecuteCommand(string command)
        {
            string output;

            Process process = new();
            ProcessStartInfo startInfo = new();
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = story.RootPath;
            startInfo.Arguments = "/c " + command;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            process.Start();

            output = process.StandardOutput.ReadToEnd() + Environment.NewLine + process.StandardError.ReadToEnd();

            process.WaitForExit();

            return output;
        }

        public void Dispose()
        {
            story.OnCreateFolder -= OnCreateFolder;
            story.OnCreateFile -= OnCreateFile;
            story.OnDeleteFile -= OnDeleteFile;
            story.OnDeleteFolder -= OnDeleteFolder;
            story.OnLoadFile -= OnLoadFile;
            story.OnSaveFile -= OnSaveFile;
        }
    }
}
