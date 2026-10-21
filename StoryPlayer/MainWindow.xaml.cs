using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TrueStory;
using static TrueStory.Story;

namespace StoryPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Story? story;

        enum State
        {
            NoStory,
            StoryManualPlay,
            StoryAutoPlay
        }

        State state;

        QuestionWindow questionWindow;

        HashSet<string> files;
        HashSet<string> folders;

        ReadDirection? lastReadDirection;
        bool lastReadedIsQuestion;
        bool lastReadedIsFileSave;
        SaveFileData? lastReadedSaveFileData;
        int lastReadedChangeIndex;

        bool ignoreReadEvent;

        DispatcherTimer playTimer;

        public MainWindow()
        {
            InitializeComponent();

            questionWindow = new();

            files = new();
            folders = new();

            playTimer = new();
            playTimer.Interval = TimeSpan.FromMilliseconds(Constants.playTimerInterval);
            playTimer.Tick += OnPlayTimerTick;

            Title = Version.GetFullName();

            SetState(State.NoStory);


        }

        void SetState(State _uiState)
        {
            if (_uiState == State.NoStory)
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
            else if (_uiState == State.StoryManualPlay)
            {
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                StoryControls.Visibility = Visibility.Visible;
                FileEditorTextBox.Visibility = Visibility.Visible;
                CommandConsoleTextBox.Visibility = Visibility.Visible;
                FolderViewList.Visibility = Visibility.Visible;
                questionWindow.Visibility = Visibility.Hidden;
                GotoStartButton.IsEnabled = true;
                PreviousChangeButton.IsEnabled = true;
                PlayButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                NextChangeButton.IsEnabled = true;
                GotoEndButton.IsEnabled = true;

                ClearFolderView();
            }
            else // _uiState == UIState.StoryAutoPlay
            {
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = false;
                StoryControls.Visibility = Visibility.Visible;
                FileEditorTextBox.Visibility = Visibility.Visible;
                CommandConsoleTextBox.Visibility = Visibility.Visible;
                FolderViewList.Visibility = Visibility.Visible;
                questionWindow.Visibility = Visibility.Hidden;
                GotoStartButton.IsEnabled = false;
                PreviousChangeButton.IsEnabled = false;
                PlayButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                NextChangeButton.IsEnabled = false;
                GotoEndButton.IsEnabled = false;

            }

            state = _uiState;
        }

        void OnMenuOpenActivity(object sender, RoutedEventArgs e)
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

                    SetState(State.StoryManualPlay);

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
                    ignoreReadEvent = false;

                }
                else
                {
                    MessageBox.Show("No hay una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

        }

        void OnMenuCloseActivity(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            files.Clear();
            folders.Clear();
            lastReadDirection = null;
            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
            lastReadedSaveFileData = null;
            lastReadedChangeIndex = 0;
            ignoreReadEvent = false;


            FileEditorTextBox.Clear();
            FolderViewList.Items.Clear();
            CommandConsoleTextBox.Clear();

            story.Dispose();
            story = null;

            SetState(State.NoStory);
        }

        void OnHeaderReaded(Story.HeaderData data)
        {
            if (ignoreReadEvent) { return;  }

            BlockInfoLabel.Content = GetCursorText() + " Creada actividad por " + data.author + " con código " + data.publicKey;

            FileEditorTextBox.Text = "";

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
        }

        void OnQuestionReaded(Story.QuestionData q)
        {
            if (ignoreReadEvent) { return; }

            BlockInfoLabel.Content = GetCursorText() + " Contestada pregunta en " + q.usedTime + " segundos de " + q.timeLimit;

            questionWindow.Question = q;
            lastReadedIsQuestion = true;
            lastReadedIsFileSave = false;
        }

        void OnSaveFileReaded(SaveFileData data)
        {
            if (ignoreReadEvent) { return; }

            BlockInfoLabel.Content = GetCursorText() + " Guardado fichero " + data.fileName + " con " + data.text.Length + " carácteres y " + data.changes.Length + " cambios";

            files.Add(data.fileName);
            SelectFileInFolderView(data.fileName);

            if (!ContentEventsCheckBox.IsChecked.GetValueOrDefault())
            {
                FileEditorTextBox.Text = data.text;
            }
            else
            {
                if (lastReadDirection == ReadDirection.Backwards)
                {
                    FileEditorTextBox.Text = data.text;
                    lastReadedChangeIndex = data.changes.Length;
                }
                else // lastReadDirection == ReadDirection.Forward
                {
                    lastReadedChangeIndex = 0;
                }

            }

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = true;

            lastReadedSaveFileData = data;
        }

        void OnLoadFileReaded(LoadFileData data)
        {
            if (ignoreReadEvent) { return; }

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

        void OnExecuteCommandReaded(ExecuteCommandData data)
        {
            if (ignoreReadEvent) { return; }

            BlockInfoLabel.Content = GetCursorText() + " Ejecutado comando " + data.name;

            CommandConsoleTextBox.Text = "> " + data.name + "\n" + data.result + "\n";

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;
        }

        void OnDeleteFileReaded(DeleteFileData data)
        {
            if (ignoreReadEvent) { return; }

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

        void OnCreateFolderReaded(CreateFolderData data)
        {
            if (ignoreReadEvent) { return; }

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

        void OnCreateFileReaded(CreateFileData data)
        {
            if (ignoreReadEvent) { return; }

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
            if (ignoreReadEvent) { return; }

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

        void OnCloseStoryReaded(Story.CloseStoryData data)
        {
            if (ignoreReadEvent) { return; }

            BlockInfoLabel.Content = GetCursorText() + " Actividad cerrada";

            FileEditorTextBox.Text = "";

            lastReadedIsQuestion = false;
            lastReadedIsFileSave = false;

        }

        void OnOpenStoryReaded()
        {
            if (ignoreReadEvent) { return; }

            BlockInfoLabel.Content = GetCursorText() + " Actividad abierta";

            FileEditorTextBox.Text = "";

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

        void OnNextStep(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            Step();
        }

        void OnPreviousStep(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);
            Step(Story.ReadDirection.Backwards);
        }


        void Step(Story.ReadDirection direction = Story.ReadDirection.Forward)
        {
            Debug.Assert(story != null);

            lastReadDirection = direction;

            bool consoleReadStop = ConsoleEventsCheckBox.IsChecked.GetValueOrDefault();
            bool questionReadStop = QuestionEventsCheckBox.IsChecked.GetValueOrDefault();
            bool fileReadStop = FileEventsCheckBox.IsChecked.GetValueOrDefault();
            bool activityReadStop = ActivityEventsCheckbox.IsChecked.GetValueOrDefault();
            bool contentReadStop = ContentEventsCheckBox.IsChecked.GetValueOrDefault();

            bool stepDone = false;

            Record? record;

            do
            {

                lastReadDirection = direction;

                if (contentReadStop && lastReadedIsFileSave)
                {
                    Debug.Assert(lastReadedSaveFileData != null);
                    if (lastReadDirection == ReadDirection.Forward && lastReadedChangeIndex < lastReadedSaveFileData.Value.changes.Length)
                    {
                        string text = FileEditorTextBox.Text;
                        FileChangeData change = lastReadedSaveFileData.Value.changes[lastReadedChangeIndex];

                        // Console.WriteLine("Do change " + lastReadedChangeIndex + "Offset: " + change.Offset + " Added: " + change.Added + " Removed: " + change.Removed + " AddedText: " + change.AddedText);
                        FileEditorTextBox.Text = ApplyFileChange(text, change, ReadDirection.Forward);

                        BlockInfoLabel.Content = GetCursorText() + ":" + GetChangeText() + " Aplicado cambio";

                        lastReadedChangeIndex++;



                        stepDone = true;



                    }
                    else if (lastReadDirection == ReadDirection.Backwards && lastReadedChangeIndex > 0)
                    {
                        lastReadedChangeIndex--;

                        string text = FileEditorTextBox.Text;
                        FileChangeData change = lastReadedSaveFileData.Value.changes[lastReadedChangeIndex];

                        //Console.WriteLine("Undo change " + lastReadedChangeIndex + "Backwards " + "Offset: " + change.Offset + " Added: " + change.Added + " Removed: " + change.Removed + " AddedText: " + change.AddedText);
                        FileEditorTextBox.Text = ApplyFileChange(text, change, ReadDirection.Backwards);

                        BlockInfoLabel.Content = GetCursorText() + ":" + GetChangeText() + " Revertido cambio";

                        stepDone = true;

                    }
                    else if (lastReadDirection == ReadDirection.Backwards && lastReadedChangeIndex == 0)
                    {
                        ignoreReadEvent = true;
                        story.ReadRecord(ReadDirection.Backwards);
                        ignoreReadEvent = false;

                        lastReadedIsFileSave = false;
                        lastReadedSaveFileData = null;
                        lastReadedChangeIndex = 0;
                    }
                    else if (lastReadDirection == ReadDirection.Forward && lastReadedChangeIndex == lastReadedSaveFileData.Value.changes.Length)
                    {
                        lastReadedIsFileSave = false;
                        lastReadedSaveFileData = null;
                        lastReadedChangeIndex = 0;
                    }

                }

                // Read a block
                
                if(!stepDone)
                {

                    record = story.ReadRecord(direction);

                    if (record.HasValue)
                    {

                        if (fileReadStop && (record.Value.type == RecordType.CreateFile ||
                                                  record.Value.type == RecordType.DeleteFile ||
                                                  record.Value.type == RecordType.LoadFile ||
                                                  record.Value.type == RecordType.SaveFile))
                        { stepDone = true; }
                        else if (consoleReadStop && record.Value.type == RecordType.ExecuteCommand)
                        { stepDone = true; }
                        else if (questionReadStop && record.Value.type == RecordType.Question)
                        { stepDone = true; }
                        else if (consoleReadStop && record.Value.type == RecordType.ExecuteCommand)
                        { stepDone = true; }
                        else if (activityReadStop && (record.Value.type == RecordType.OpenStory ||
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

            BlockProgress.Value = story.RecordIndex;

            if (lastReadedIsQuestion) { questionWindow.Show(); }
            else { questionWindow.Hide(); }

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

        string ApplyFileChange(string text, FileChangeData change, ReadDirection direction)
        {
            string result = text;

            if (change.Offset <= result.Length)
            {
                if (direction == ReadDirection.Forward)
                {
                    if (change.Removed > 0 && change.Offset + change.Removed <= result.Length) { result = result.Remove(change.Offset, change.Removed); }
                    if (change.Added > 0) { result = result.Insert(change.Offset, change.AddedText); }
                }
                else
                {
                    if (change.Added > 0 && change.Offset + change.Added <= result.Length) { result = result.Remove(change.Offset, change.Added); }
                    if (change.Removed > 0) { result = result.Insert(change.Offset, GenerateUnknownText(change.Removed)); }
                }
            }

            return result;
        }
            

        string GetCursorText()
        {
            Debug.Assert(story != null);

            return String.Format("<{0:0}/{1:0}>", story.RecordIndex, story.TotalRecords);
        }

        string GetChangeText()
        {
            Debug.Assert(lastReadedSaveFileData != null);

            return String.Format("<{0:0}/{1:0}>", lastReadedChangeIndex, lastReadedSaveFileData.Value.changes.Length);
        }

        string GenerateUnknownText(int length)
        {
            StringBuilder builder = new();

            for (int i = 0; i < length; i++)
            {
                builder.Append(Constants.unknowCharacter);
            }

            return builder.ToString();
        }

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (story == null) { return; }

            if (e.Key == Key.Right)
            {
                Step();

                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                Step(ReadDirection.Backwards);

                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (state == State.StoryManualPlay)
                {
                    SetState(State.StoryAutoPlay);
                    playTimer.Start();
                }
                else
                {
                    playTimer.Stop();
                    SetState(State.StoryManualPlay);
                }

                e.Handled = true;
            }
        }

        void OnPlayTimerTick(object? sender, EventArgs e)
        {
            if (story == null) { return; }

            Step();
        }

        void OnPlay(object sender, RoutedEventArgs e)
        {
            if (story == null) { return; }

            SetState(State.StoryAutoPlay);
            playTimer.Start();
        }

        private void OnStop(object sender, RoutedEventArgs e)
        {
            if (story == null) { return; }

            playTimer.Stop();
            SetState(State.StoryManualPlay);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if(story != null)
            {
                story.Dispose();
                story = null;
            }

            Application.Current.Shutdown();
        }
    }
}