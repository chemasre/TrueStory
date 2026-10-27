using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace TrueStory.Source
{
    public class FolderView
    {
        public event OnFolderSelectedHandler? OnFolderSelected;
        public event OnFileSelectedHandler? OnFileSelected;
        public event OnDeselectedHandler? OnDeselected;

        public delegate void OnFolderSelectedHandler(string folder);
        public delegate void OnFileSelectedHandler(string file);
        public delegate void OnDeselectedHandler();

        public Story? Story { get { return story; } set { story = value; Update(); } }

        Story? story;

        ListBox listBox;

        public FolderView(ListBox _listBox)
        {
            listBox = _listBox;

            listBox.SelectionChanged += OnListBoxSelectionChanged;
        }

        public void Update()
        {
            Debug.Assert(story != null);

            listBox.Items.Clear();

            if (!Switches.NoFolders)
            {
                string[] directories = Directory.GetDirectories(story.RootPath);
                foreach (string d in directories)
                { listBox.Items.Add(Constants.folderPrefix + Path.GetFileName(d)); }
            }

            string[] files = Directory.GetFiles(story.RootPath);
            foreach (string f in files)
            {
                string n = Path.GetFileName(f);

                if (Utils.HasAllowedExtension(n, story.Header.editableExtensions) && n != Story.StoryFilename)
                {
                    listBox.Items.Add(n);
                }
            }
        }

        void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedIndex < 0)
            {
                OnDeselected?.Invoke();
            }
            else
            {
                string? item = (string?)listBox.SelectedItem;

                Debug.Assert(item != null);

                if (!IsFolderItem(item))
                {
                    OnFileSelected?.Invoke(item);
                }
                else
                {
                    OnFolderSelected?.Invoke(GetFolderName(item));
                }
            }

        }



        bool IsFolderItem(string item)
        {
            return (item.StartsWith(Constants.folderPrefix) || item == Constants.parentFolderName);
        }

        string GetFolderName(string item)
        {
            return item.Substring(Constants.folderPrefix.Length);
        }


    }
}
