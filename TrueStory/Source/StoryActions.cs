using System;
using System.Collections.Generic;
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

        public StoryActions(Story _story)
        {
            story = _story;

            story.OnCreateFolder += OnCreateFolder;
            story.OnCreateFile += OnCreateFile;
            story.OnDeleteFile += OnDeleteFile;
            story.OnDeleteFolder += OnDeleteFolder;
            story.OnLoadFile += OnLoadFile;
            story.OnSaveFile += OnSaveFile;
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
            File.WriteAllText(story.RootPath + Path.DirectorySeparatorChar + name, text, Encoding.UTF8);
        }

        string OnLoadFile(string name)
        {
            string text = File.ReadAllText(story.RootPath + Path.DirectorySeparatorChar + name, Encoding.UTF8);

            return text;
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
