using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using TrueStory;
using static System.Net.Mime.MediaTypeNames;
using static TrueStory.Story;

namespace StoryPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Story? story;

        enum UIState
        {
            NoStory,
            Story
        }

        UIState uiState;

        QuestionWindow questionWindow;

        HashSet<string> files;
        HashSet<string> folders;

        ReadDirection? lastReadDirection;
        bool lastReadedIsQuestion;
        bool lastReadedIsFileSave;
        SaveFileData? lastReadedSaveFileData;
        int lastReadedChangeIndex;

        public MainWindow()
        {
            InitializeComponent();

            questionWindow = new();

            files = new();
            folders = new();

            Title = Version.GetFullName();

            SetUIState(UIState.NoStory);


        }

        void SetUIState(UIState _uiState)
        {
            if (_uiState == UIState.NoStory)
            {
                OpenActivityMenuAction.IsEnabled = true;
                CloseActivityMenuAction.IsEnabled = false;
                StoryControls.Visibility = Visibility.Hidden;
                FileEditorTextBox.Visibility = Visibility.Hidden;
                CommandConsoleTextBox.Visibility = Visibility.Hidden;
                FolderViewList.Visibility = Visibility.Hidden;
                questionWindow.Visibility = Visibility.Hidden;

                ClearFolderView();
            }
            else // _uiState == UIState.Story
            {
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                StoryControls.Visibility = Visibility.Visible;
                FileEditorTextBox.Visibility = Visibility.Visible;
                CommandConsoleTextBox.Visibility = Visibility.Visible;
                FolderViewList.Visibility = Visibility.Visible;
                questionWindow.Visibility = Visibility.Hidden;

                ClearFolderView();
            }

            uiState = _uiState;
        }

        private void OnMenuOpenActivity(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new();

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FolderName;

                if (Story.ExistsStory(path))
                {
                    story = new Story(path, Story.Mode.Read);

                    story.OnReadedHeader += OnHeaderReaded;
                    story.OnReadedOpenStory += OnOpenStoryReaded;
                    story.OnReadedCloseStory += OnCloseStoryReaded;
                    story.OnReadedCreateFile += OnCreateFileReaded;
                    story.OnReadedCreateFolder += OnCreateFolderReaded;
                    story.OnReadedDeleteFile += OnDeleteFileReaded;
                    story.OnReadedDeleteFolder += OnDeleteFolderReaded;
                    story.OnReadedExecuteCommand += OnExecuteCommandReaded;
                    story.OnReadedQuestion += OnQuestionReaded;
                    story.OnReadedLoadFile += OnLoadFileReaded;
                    story.OnReadedSaveFile += OnSaveFileReaded;

                    SetUIState(UIState.Story);

                    BlockProgress.Maximum = story.TotalRecords;
                    BlockProgress.Minimum = 0;
                    BlockProgress.Value = story.RecordIndex;

                    files.Clear();
                    folders.Clear();
                    lastReadDirection = null;
                    lastReadedIsQuestion = false;
                    lastReadedIsFileSave = false;
                    lastReadedSaveFileData = null;
                    lastReadedChangeIndex = 0;

                }
                else
                {
                    MessageBox.Show("No hay una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

        }

        private void OnMenuCloseActivity(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            files.Clear();
            folders.Clear();
            lastReadDirection = null;
            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
            lastReadedSaveFileData = null;
            lastReadedChangeIndex = 0;


            FileEditorTextBox.Clear();
            FolderViewList.Items.Clear();
            CommandConsoleTextBox.Clear();

            story.Dispose();
            story = null;

            SetUIState(UIState.NoStory);
        }

        private void OnHeaderReaded(Story.HeaderData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Creada actividad por " + data.author + " con código " + data.publicKey;

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
        }

        private void OnQuestionReaded(Story.QuestionData q)
        {
            BlockInfoLabel.Content = GetCursorText() + " Contestada pregunta en " + q.usedTime + " segundos de " + q.timeLimit;

            questionWindow.Question = q;
            lastReadedIsQuestion = true;
            lastReadedIsFileSave = false;
        }

        private void OnSaveFileReaded(SaveFileData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Guardado fichero " + data.fileName + " con " + data.text.Length + " carácteres y " + data.changes.Length + " cambios";

            files.Add(data.fileName);
            SelectFileInFolderView(data.fileName);

            if(lastReadDirection == ReadDirection.Backwards)
            {
                FileEditorTextBox.Text = data.text;
            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = true;

            lastReadedSaveFileData = data;
        }

        private void OnLoadFileReaded(LoadFileData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Abierto fichero " + data.fileName + " con " + data.text.Length + " carácteres";

            files.Add(data.fileName);
            SelectFileInFolderView(data.fileName);

            if (lastReadDirection == ReadDirection.Forward)
            {
                FileEditorTextBox.Text = data.text;
            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
        }

        private void OnExecuteCommandReaded(ExecuteCommandData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Ejecutado comando " + data.name;

            CommandConsoleTextBox.Text = "> " + data.name + "\n" + data.result + "\n";

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
        }

        private void OnDeleteFileReaded(DeleteFileData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Eliminado fichero " + data.fileName;

            if (lastReadDirection == ReadDirection.Forward)
            {
                if (files.Contains(data.fileName))
                {
                    files.Remove(data.fileName);
                    UpdateFolderView();
                }
            }
            else
            {
                // Don't try to understand it: feel it

                files.Add(data.fileName);
                UpdateFolderView();
            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }

        private void OnCreateFolderReaded(CreateFolderData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Creada carpeta " + data.folderName;

            if (lastReadDirection == ReadDirection.Forward)
            {
                folders.Add(data.folderName);
                UpdateFolderView();
            }
            else
            {
                if (folders.Contains(data.folderName))
                {
                    folders.Remove(data.folderName);
                    UpdateFolderView();
                }
            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }

        private void OnCreateFileReaded(CreateFileData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Creado fichero " + data.fileName;

            if (lastReadDirection == ReadDirection.Forward)
            {
                files.Add(data.fileName);
                UpdateFolderView();
            }
            else
            {
                if (files.Contains(data.fileName))
                {
                    files.Remove(data.fileName);
                    UpdateFolderView();
                }
            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }

        void OnDeleteFolderReaded(DeleteFolderData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Eliminada carpeta " + data.folderName;

            if (lastReadDirection == ReadDirection.Forward)
            {
                if (folders.Contains(data.folderName))
                {
                    folders.Remove(data.folderName);
                    UpdateFolderView();
                }
            }
            else
            {
                // Don't try to understand it: feel it

                folders.Add(data.folderName);
                UpdateFolderView();
            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }

        private void OnCloseStoryReaded(Story.CloseStoryData data)
        {
            BlockInfoLabel.Content = GetCursorText() + " Actividad cerrada";

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }

        private void OnOpenStoryReaded()
        {
            BlockInfoLabel.Content = GetCursorText() + " Actividad abierta";

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }


        void OnGotoStart(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            lastReadDirection = ReadDirection.Backwards;

            Record? record;

            do
            {
                lastReadDirection = ReadDirection.Forward;
                record = story.ReadRecord(ReadDirection.Backwards);

            } while (record != null);

            BlockProgress.Value = story.RecordIndex;

            if (lastReadedIsQuestion) { questionWindow.Show(); }
            else { questionWindow.Hide(); }

        }

        void OnGotoEnd(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            lastReadDirection = ReadDirection.Forward;

            Record? record;

            do
            {
                lastReadDirection = ReadDirection.Forward;
                record = story.ReadRecord();

            } while (record != null);

            BlockProgress.Value = story.RecordIndex;

            if (lastReadedIsQuestion) { questionWindow.Show(); }
            else { questionWindow.Hide(); }

        }

        private void OnNextStep(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            lastReadDirection = ReadDirection.Forward;

            Step();

            BlockProgress.Value = story.RecordIndex;

            if (lastReadedIsQuestion) { questionWindow.Show(); }
            else { questionWindow.Hide(); }
        }

        private void OnPreviousStep(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            lastReadDirection = ReadDirection.Backwards;

            Step(Story.ReadDirection.Backwards);

            BlockProgress.Value = story.RecordIndex;

            if (lastReadedIsQuestion) { questionWindow.Show(); }
            else { questionWindow.Hide(); }
        }

        void Step(Story.ReadDirection direction = Story.ReadDirection.Forward)
        {
            Debug.Assert(story != null);

            bool consoleEvents = ConsoleEventsCheckBox.IsChecked.GetValueOrDefault();
            bool questionEvents = QuestionEventsCheckBox.IsChecked.GetValueOrDefault();
            bool fileEvents = FileEventsCheckBox.IsChecked.GetValueOrDefault();
            bool activityEvents = ActivityEventsCheckbox.IsChecked.GetValueOrDefault();
            bool contentEvents = ContentEventsCheckBox.IsChecked.GetValueOrDefault();

            bool stepDone = false;

            Record? record;

            do
            {

                lastReadDirection = direction;

                if (contentEvents && lastReadedIsFileSave)
                {
                    Debug.Assert(lastReadedSaveFileData != null);
                    if (lastReadedChangeIndex < lastReadedSaveFileData.Value.changes.Length)
                    {
                        string text = FileEditorTextBox.Text;
                        FileChangeData change = lastReadedSaveFileData.Value.changes[lastReadedChangeIndex];
                        if (lastReadDirection == ReadDirection.Forward)
                        {
                            Console.WriteLine("Offset: " + change.Offset + " Added: " + change.Added + " Removed: " +  change.Removed + " AddedText: " + change.AddedText);

                            if (change.Offset <= text.Length)
                            {
                                if (change.Removed > 0 && change.Offset + change.Removed <= text.Length) { text = text.Remove(change.Offset, change.Removed); }
                                if (change.Added > 0) { text = text.Insert(change.Offset, change.AddedText); }

                                FileEditorTextBox.Text = text;
                            }


                            lastReadedChangeIndex++;
                            stepDone = true;
                        }
                    }
                    else
                    {
                        lastReadedIsFileSave = false;
                        lastReadedSaveFileData = null;
                        lastReadedChangeIndex = 0;
                    }

                }
                
                if(!stepDone)
                {

                    record = story.ReadRecord(direction);

                    if (record.HasValue)
                    {

                        if (fileEvents && (record.Value.type == RecordType.CreateFile ||
                                                  record.Value.type == RecordType.DeleteFile ||
                                                  record.Value.type == RecordType.LoadFile ||
                                                  record.Value.type == RecordType.SaveFile))
                        { stepDone = true; }
                        else if (consoleEvents && record.Value.type == RecordType.ExecuteCommand)
                        { stepDone = true; }
                        else if (questionEvents && record.Value.type == RecordType.Question)
                        { stepDone = true; }
                        else if (consoleEvents && record.Value.type == RecordType.ExecuteCommand)
                        { stepDone = true; }
                        else if (activityEvents && (record.Value.type == RecordType.OpenStory ||
                                                    record.Value.type == RecordType.CloseStory ||
                                                    record.Value.type == RecordType.Header))
                        { stepDone = true; }

                    }
                    else
                    {
                        stepDone = true;
                    }
                }

            } while (!stepDone);

        }

        void UpdateFolderView()
        {
            FolderViewList.Items.Clear();

            List<string> list = folders.ToList();
            list.Sort();

            foreach (string f in list)
            { FolderViewList.Items.Add(Constants.folderPrefix + f); }

            list = files.ToList();
            list.Sort();

            foreach (string f in list)
            { FolderViewList.Items.Add(f); }
        }

        void ClearFolderView()
        {
            FolderViewList.Items.Clear();
        }

        void SelectFileInFolderView(string filename)
        {
            UpdateFolderView();

            List<string> list = files.ToList();
            list.Sort();

            int pos = list.IndexOf(filename);
            if (pos >= 0)
            { FolderViewList.SelectedIndex = folders.Count + pos; }
            
        }

        string GetCursorText()
        {
            Debug.Assert(story != null);

            return String.Format("<{0:0}/{1:0}>", story.RecordIndex, story.TotalRecords);
        }

    }
}