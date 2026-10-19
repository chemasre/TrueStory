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
        ReadDirection readDirection;

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
                questionWindow.Visibility = Visibility.Visible;

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
                }
                else
                {
                    MessageBox.Show("No hay una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

        }

        private void OnHeaderReaded(Story.HeaderData data)
        {
            BlockInfoLabel.Content = "Creada actividad por " + data.author + " con código " + data.publicKey;
        }

        private void OnQuestionReaded(Story.QuestionData q)
        {
            BlockInfoLabel.Content = "Contestada pregunta en " + q.usedTime + " segundos de " + q.timeLimit;

            questionWindow.Question = q;
        }

        private void OnSaveFileReaded(SaveFileData data)
        {
            BlockInfoLabel.Content = "Guardado fichero " + data.fileName + " con " + data.text.Length + " carácteres y " + data.changes.Length + " cambios";

            files.Add(data.fileName);
            SelectFileInFolderView(data.fileName);

            FileEditorTextBox.Text = data.text;
        }

        private void OnLoadFileReaded(LoadFileData data)
        {
            BlockInfoLabel.Content = "Abierto fichero " + data.fileName + " con " + data.text.Length + " carácteres";

            files.Add(data.fileName);
            SelectFileInFolderView(data.fileName);

            FileEditorTextBox.Text = data.text;
        }

        private void OnExecuteCommandReaded(ExecuteCommandData data)
        {
            BlockInfoLabel.Content = "Ejecutado comando " + data.name;

            CommandConsoleTextBox.Text = "> " + data.name + "\n" + data.result + "\n";
        }

        private void OnDeleteFileReaded(DeleteFileData data)
        {
            BlockInfoLabel.Content = "Eliminado fichero " + data.fileName;

            if (readDirection == ReadDirection.Forward)
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
        }

        private void OnCreateFolderReaded(CreateFolderData data)
        {
            BlockInfoLabel.Content = "Creada carpeta " + data.folderName;

            if (readDirection == ReadDirection.Forward)
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
        }

        private void OnCreateFileReaded(CreateFileData data)
        {
            BlockInfoLabel.Content = "Creado fichero " + data.fileName;

            if (readDirection == ReadDirection.Forward)
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

        }

        void OnDeleteFolderReaded(DeleteFolderData data)
        {
            BlockInfoLabel.Content = "Eliminada carpeta " + data.folderName;

            if (readDirection == ReadDirection.Forward)
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
        }

        private void OnCloseStoryReaded(Story.PersistentData data)
        {
            BlockInfoLabel.Content = "Actividad cerrada";
        }

        private void OnOpenStoryReaded(Story.PersistentData data)
        {
            BlockInfoLabel.Content = "Actividad abierta";
        }

        private void OnMenuCloseActivity(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            FileEditorTextBox.Clear();
            FolderViewList.Items.Clear();
            CommandConsoleTextBox.Clear();

            story.Dispose();
            story = null;

            SetUIState(UIState.NoStory);
        }

        void OnGotoStart(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            readDirection = ReadDirection.Backwards;

            Record? record;

            do
            { record = story.ReadRecord(ReadDirection.Backwards);
            } while (record != null);

            BlockProgress.Value = story.RecordIndex;
        }

        void OnGotoEnd(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            readDirection = ReadDirection.Forward;

            Record? record;

            do
            { record = story.ReadRecord();
            } while (record != null);

            BlockProgress.Value = story.RecordIndex;
        }

        private void OnNextStep(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            readDirection = ReadDirection.Forward;

            Step();

            BlockProgress.Value = story.RecordIndex;
        }

        private void OnPreviousStep(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            readDirection = ReadDirection.Backwards;

            Step(Story.ReadDirection.Backwards);

            BlockProgress.Value = story.RecordIndex;
        }

        void Step(Story.ReadDirection direction = Story.ReadDirection.Forward)
        {
            bool commandStop = ConsoleCommandsStopCheckBox.IsChecked.GetValueOrDefault();
            bool questionStop = QuestionsStopCheckBox.IsChecked.GetValueOrDefault();
            bool fileOperationStop = FileSaveOrLoadStopCheckBox.IsChecked.GetValueOrDefault();

            Record? record = null;
            bool stop = false;

            do
            {
                record = story.ReadRecord(direction);

                if (record.HasValue)
                {
                    if (fileOperationStop && (record.Value.type == RecordType.CreateFile ||
                                              record.Value.type == RecordType.DeleteFile ||
                                              record.Value.type == RecordType.LoadFile ||
                                              record.Value.type == RecordType.SaveFile))
                    { stop = true; }
                    else if (commandStop && record.Value.type == RecordType.ExecuteCommand)
                    { stop = true; }
                    else if (questionStop && record.Value.type == RecordType.Question)
                    { stop = true; }

                }
                else
                {
                    stop = true;
                }

            } while (!stop);

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
    }
}