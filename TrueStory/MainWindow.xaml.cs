using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace TrueStory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string? LoadedFile { get { return loadedFile; } }
        public FileEditor FileEditor { get { return fileEditor; } }
        public CommandConsole CommandConsole { get { return commandConsole; } }
        public Instructor Instructor { get { return instructor; } }
        public Story? Story { get { return story; } }
        public Settings Settings { get { return settings;  } }

        public static Log Log { get { return log;  } }

        Story? story;
        StoryActions? storyActions;

        string? loadedFile;
        List<RecordData.FileChangeData> loadedFileChanges;
        bool loadedFileNeedsSave;

        string? selectedFolder;

        enum UIState
        {
            NoStory,
            StoryNoSelection,
            StoryFileLoadedNoNeedsSave,
            StoryFileLoadedNeedsSave,
            StoryFolderSelected,
            Question
        }

        UIState uiState;

        FileEditor fileEditor;
        FolderView folderView;
        CommandConsole commandConsole;
        Instructor instructor;

        Themes themes;

        Settings settings;

        public static RoutedCommand ShortcutNewFile = new RoutedCommand();
        public static RoutedCommand ShortcutSaveFile = new RoutedCommand();
        public static RoutedCommand ShortcutEnlargeText = new RoutedCommand();
        public static RoutedCommand ShortcutReduceText = new RoutedCommand();
        public static RoutedCommand ShortcutCopy = new RoutedCommand();
        public static RoutedCommand ShortcutCut = new RoutedCommand();
        public static RoutedCommand ShortcutPaste = new RoutedCommand();
        public static RoutedCommand ShortcutGoto = new RoutedCommand();
        public static RoutedCommand ShortcutFind = new RoutedCommand();

        UIState questionPreviousUIState;

        bool ignoreUIevents;

        static Log log;

        public MainWindow()
        {
            InitializeComponent();

            log = new Log(Constants.logFileName);
            settings = new(Constants.settingsFile);

            Closing += OnClosing;

            fileEditor = new(this);
            fileEditor.OnContentsChanged += OnFileContentsChanged;

            folderView = new FolderView(FolderViewList);
            folderView.OnDeselected += OnFolderViewDeselected;
            folderView.OnFileSelected += OnFolderViewFileSelected;
            folderView.OnFolderSelected += OnFolderViewFolderSelected;

            commandConsole = new(CommandConsoleTextBox, CommandConsoleTitleLabel);
            commandConsole.OnExecuteCommand += OnExecuteCommand;

            instructor = new(this);

            instructor.OnQuestionStarts += OnInstructorQuestionStarts;
            instructor.OnQuestionEnds += OnInstructorQuestionEnds;

            ignoreUIevents = true;
            DarkModeCheckableAction.IsChecked = (settings.GetInt("DarkMode") != 0);
            ignoreUIevents = false;

            themes = new(this);


            ShortcutSaveFile.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            ShortcutNewFile.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            ShortcutEnlargeText.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control));
            ShortcutEnlargeText.InputGestures.Add(new KeyGesture(Key.Add, ModifierKeys.Control));
            ShortcutReduceText.InputGestures.Add(new KeyGesture(Key.OemMinus, ModifierKeys.Control));
            ShortcutReduceText.InputGestures.Add(new KeyGesture(Key.Subtract, ModifierKeys.Control));
            ShortcutCopy.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
            ShortcutCut.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));
            ShortcutPaste.InputGestures.Add(new KeyGesture(Key.V, ModifierKeys.Control));
            ShortcutGoto.InputGestures.Add(new KeyGesture(Key.G, ModifierKeys.Control));
            ShortcutFind.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));

            loadedFileChanges = new();

            if (Switches.NoFolders)
            {
                NewFolderMenuAction.Visibility = Visibility.Collapsed;
            }

            SetUIState(UIState.NoStory);

            UpdateFileTitle();
            UpdateWindowTitle();

        }

        private void OnInstructorQuestionStarts()
        {
            Utils.Log("Blocking UI");
            questionPreviousUIState = uiState;
            SetUIState(UIState.Question);
        }

        private void OnInstructorQuestionEnds()
        {
            Utils.Log("Unblocking UI");
            SetUIState(questionPreviousUIState);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (story == null) { return;  }
            if (uiState == UIState.Question) { e.Cancel = true; return;  }

            CheckFileNeedsSave();

            log.Dispose();
            log = null;
        }

        private string OnExecuteCommand(string command)
        {
            Debug.Assert(story != null);

            return story.AddExecuteCommand(command);
        }

        private void OnFolderViewFolderSelected(string _folder)
        {
            CheckFileNeedsSave();

            selectedFolder = _folder;

            fileEditor.Unload();

            SetUIState(UIState.StoryFolderSelected);
        }

        private void OnFolderViewFileSelected(string file)
        {
            Debug.Assert(story != null);

            selectedFolder = null;

            CheckFileNeedsSave();

            SetUIState(UIState.StoryFileLoadedNoNeedsSave);

            fileEditor.Load(file, story.AddLoadFile(file));
            loadedFile = file;
            loadedFileNeedsSave = false;
            UpdateFileTitle();

        }

        private void OnFolderViewDeselected()
        {
            selectedFolder = null;

            CheckFileNeedsSave();

            fileEditor.Unload();

            SetUIState(UIState.StoryNoSelection);
        }

        private void OnShortcutSaveFile(object sender, ExecutedRoutedEventArgs e)
        {
            if(FileMenu.IsEnabled && SaveFileMenuAction.IsEnabled)
            {
                OnMenuSaveFile(sender, e);
            }

        }

        private void OnShortcutNewFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (FileMenu.IsEnabled && NewFileMenuAction.IsEnabled)
            {
                OnMenuNewFile(sender, e);
            }
        }

        private void OnShortcutEnlargeText(object sender, ExecutedRoutedEventArgs e)
        {
            fileEditor.EnlargeText();
        }

        private void OnShortcutReduceText(object sender, ExecutedRoutedEventArgs e)
        {
            fileEditor.ReduceText();
        }

        void SetUIState(UIState _uiState)
        {
            if (_uiState == UIState.NoStory)
            {
                NewActivityMenuAction.IsEnabled = true;
                OpenActivityMenuAction.IsEnabled = true;
                CloseActivityMenuAction.IsEnabled = false;
                FileEditorTextBox.IsEnabled = false;
                FolderViewList.IsEnabled = false;
                CommandConsoleTextBox.IsEnabled = false;
                FileMenu.IsEnabled = false;
                ViewMenu.IsEnabled = true;
                EnlargeTextAction.IsEnabled = false;
                ReduceTextAction.IsEnabled = false;
                ResetTextAction.IsEnabled = false;
                FileEditorTextBox.IsReadOnly = false;
                FolderViewList.Visibility = Visibility.Hidden;
            }
            else if (_uiState == UIState.StoryNoSelection)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                FileEditorTextBox.IsEnabled = false;
                FolderViewList.IsEnabled = true;
                CommandConsoleTextBox.IsEnabled = true;
                FileMenu.IsEnabled = true;
                NewMenu.IsEnabled = true;
                NewFileMenuAction.IsEnabled = true;
                NewFolderMenuAction.IsEnabled = true;
                SaveFileMenuAction.IsEnabled = false;
                DeleteFileOrFolderMenuAction.IsEnabled = false;
                RenameFileOrFolderMenuAction.IsEnabled = false;
                ViewMenu.IsEnabled = true;
                EnlargeTextAction.IsEnabled = false;
                ReduceTextAction.IsEnabled = false;
                ResetTextAction.IsEnabled = false;
                FileEditorTextBox.IsReadOnly = false;
                FolderViewList.Visibility = Visibility.Visible;
            }
            else if (_uiState == UIState.StoryFileLoadedNoNeedsSave)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                FileEditorTextBox.IsEnabled = true;
                FolderViewList.IsEnabled = true;
                CommandConsoleTextBox.IsEnabled = true;
                FileMenu.IsEnabled = true;
                NewMenu.IsEnabled = true;
                NewFileMenuAction.IsEnabled = true;
                NewFolderMenuAction.IsEnabled = true;
                SaveFileMenuAction.IsEnabled = false;
                DeleteFileOrFolderMenuAction.IsEnabled = true;
                RenameFileOrFolderMenuAction.IsEnabled = true;
                ViewMenu.IsEnabled = true;
                EnlargeTextAction.IsEnabled = true;
                ReduceTextAction.IsEnabled = true;
                ResetTextAction.IsEnabled = true;
                FileEditorTextBox.IsReadOnly = false;
                FolderViewList.Visibility = Visibility.Visible;
            }
            else if (_uiState == UIState.StoryFileLoadedNeedsSave)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                FileEditorTextBox.IsEnabled = true;
                FolderViewList.IsEnabled = true;
                CommandConsoleTextBox.IsEnabled = true;
                FileMenu.IsEnabled = true;
                NewMenu.IsEnabled = true;
                NewFileMenuAction.IsEnabled = true;
                NewFolderMenuAction.IsEnabled = true;
                SaveFileMenuAction.IsEnabled = true;
                DeleteFileOrFolderMenuAction.IsEnabled = true;
                RenameFileOrFolderMenuAction.IsEnabled = true;
                ViewMenu.IsEnabled = true;
                EnlargeTextAction.IsEnabled = true;
                ReduceTextAction.IsEnabled = true;
                ResetTextAction.IsEnabled = true;
                FileEditorTextBox.IsReadOnly = false;
                FolderViewList.Visibility = Visibility.Visible;
            }
            else if (_uiState == UIState.StoryFolderSelected)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                FileEditorTextBox.IsEnabled = false;
                FolderViewList.IsEnabled = true;
                CommandConsoleTextBox.IsEnabled = true;
                FileMenu.IsEnabled = true;
                NewMenu.IsEnabled = true;
                NewFileMenuAction.IsEnabled = true;
                NewFolderMenuAction.IsEnabled = true;
                SaveFileMenuAction.IsEnabled = false;
                DeleteFileOrFolderMenuAction.IsEnabled = true;
                RenameFileOrFolderMenuAction.IsEnabled = true;
                EditMenu.IsEnabled = false;
                ViewMenu.IsEnabled = true;
                EnlargeTextAction.IsEnabled = false;
                ReduceTextAction.IsEnabled = false;
                ResetTextAction.IsEnabled = false;
                FileEditorTextBox.IsReadOnly = false;
                FolderViewList.Visibility = Visibility.Visible;
            }
            else if (_uiState == UIState.Question)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = false;
                FileEditorTextBox.IsEnabled = false;
                FolderViewList.IsEnabled = false;
                CommandConsoleTextBox.IsEnabled = false;
                FileMenu.IsEnabled = false;
                NewMenu.IsEnabled = false;
                NewFileMenuAction.IsEnabled = false;
                NewFolderMenuAction.IsEnabled = false;
                SaveFileMenuAction.IsEnabled = false;
                DeleteFileOrFolderMenuAction.IsEnabled = false;
                RenameFileOrFolderMenuAction.IsEnabled = false;
                EditMenu.IsEnabled = false;
                ViewMenu.IsEnabled = false;
                EnlargeTextAction.IsEnabled = false;
                ReduceTextAction.IsEnabled = false;
                ResetTextAction.IsEnabled = false;
                FileEditorTextBox.IsReadOnly = true;
                FolderViewList.Visibility = Visibility.Hidden;
            }


            uiState = _uiState;
        }

        void OnFileContentsChanged(FileEditor.ContentChange c)
        {
            Debug.Assert(story != null);

            if (!loadedFileNeedsSave)
            {
                loadedFileNeedsSave = true;
                SetUIState(UIState.StoryFileLoadedNeedsSave);
                UpdateFileTitle();
            }

            RecordData.FileChangeData fc = new();
            fc.Offset = c.Offset;
            fc.Added = c.Added;
            fc.AddedText = c.AddedText;
            fc.Removed = c.Removed;
            fc.DeltaTime = Switches.NoFileChangesDeltaTime ? Constants.defaultFileChangeTime : c.DeltaTime;

            loadedFileChanges.Add(fc);
        }

        void OnNewActivity(object sender, RoutedEventArgs e)
        {
            DialogActivityProperties dialog = new();

            bool ? result = dialog.ShowDialog();


            if(result.GetValueOrDefault())
            {
                story = new Story(dialog.ActivityFolderText.Text.Trim(), Story.Mode.Create, dialog.NewHeader);
                storyActions = new StoryActions(story, this);

                SetUIState(UIState.StoryNoSelection);

                UpdateFileTitle();
                UpdateWindowTitle();


                commandConsole.Story = story;
                folderView.Story = story;
                instructor.Story = story;
            }
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
                    story = new Story(path, Story.Mode.Append);
                    storyActions = new StoryActions(story, this);

                    SetUIState(UIState.StoryNoSelection);

                    UpdateFileTitle();
                    UpdateWindowTitle();


                    commandConsole.Story = story;
                    folderView.Story = story;
                    instructor.Story = story;

                    story.AddOpenStory();


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
            
            CheckFileNeedsSave();

            FileEditor.Unload();
            loadedFile = null;

            FolderViewList.Items.Clear();
            commandConsole.Clear();

            Debug.Assert(storyActions != null);

            story.AddCloseStory();

            storyActions.Dispose();
            storyActions = null;

            commandConsole.Story = null;
            folderView.Story = null;
            instructor.Story = null;
            story.Dispose();
            story = null;

            SetUIState(UIState.NoStory);
            UpdateFileTitle();
            UpdateWindowTitle();
        }

        void UpdateFileTitle()
        {
            if (story != null && loadedFile != null) { FileEditorTitleLabel.Content = (loadedFileNeedsSave ? "[*]":"") + loadedFile + ""; }
            else { FileEditorTitleLabel.Content = "Editor de ficheros"; }

        }

        void UpdateWindowTitle()
        {
            if (story != null) { Title = story.Header.title + " - " + story.Header.student;  }
            else { Title = Version.GetFullName(); }

        }

        void OnMenuNewFolder(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            DialogAskName dialog = new();

            dialog.Title = "Crear nueva carpeta";
            dialog.Message.Text = "Introduce un nombre para la nueva carpeta.";
            dialog.Input.Text = "NuevaCarpeta";

            bool? result = dialog.ShowDialog();

            if(result.HasValue && result.Value)
            {
                string name = dialog.Input.Text;

                if (!Utils.IsValidFolderName(name))
                {
                    MessageBox.Show("El nombre de la carpeta no puede estar vacío o contener carácteres especiales", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if(Directory.Exists(story.RootPath + Path.DirectorySeparatorChar + name))
                {
                    MessageBox.Show("Ya existe una carpeta con ese nombre", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    story.AddCreateFolder(name);
                    folderView.Update();
                }

            }

        }
        void OnMenuNewFile(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            DialogAskName dialog = new();

            dialog.Title = "Crear nuevo fichero";
            dialog.Message.Text = "Introduce un nombre de fichero con alguna de estas extensiones:\n\n" + String.Join(Constants.extensionSeparator, story.Header.editableExtensions);
            dialog.Input.Text = "NuevoFichero.txt";

            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string name = dialog.Input.Text;


                if (!Utils.IsValidFileName(name))
                {
                    MessageBox.Show("El nombre del fichero no puede estar vacío o contener carácteres especiales", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if (!Utils.HasAllowedExtension(name, story.Header.editableExtensions))
                {
                    string extensions = string.Join(';', story.Header.editableExtensions);
                    MessageBox.Show("Sólo se permite crear ficheros que tengan alguna de estas extensiones: " + extensions, "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if (File.Exists(story.RootPath + Path.DirectorySeparatorChar + name))
                {
                    MessageBox.Show("Ya existe un fichero con ese nombre", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    story.AddCreateFile(name);
                    folderView.Update();
                }

            }
        }
        void OnMenuDeleteFileOrFolder(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            bool validSelection = true;

            if(FolderViewList.SelectedIndex < 0)
            {
                validSelection = false;
            }
            else if(FolderViewList.SelectedItem as string == Constants.parentFolderName)
            {
                validSelection = false;
            }

            if(!validSelection)
            {
                MessageBox.Show("Selecciona primero el fichero o carpeta que quieres eliminar", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                string name = ((string?)FolderViewList.SelectedItem ?? "");

                bool isDirectory = name.StartsWith(Constants.folderPrefix);

                if (isDirectory) { name = name.Remove(0, Constants.folderPrefix.Length); }

                MessageBoxResult result;

                if(isDirectory)
                {
                    result = MessageBox.Show("Vas a eliminar la carpeta '" + name + "' estás seguro/a?", "Confirmar eliminación", MessageBoxButton.YesNo);
                }
                else
                {
                    result = MessageBox.Show("Vas a eliminar el fichero '" + name + "' estás seguro/a?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);
                }

                if (result == MessageBoxResult.Yes)
                {
                    if (isDirectory) { story.AddDeleteDirectory(name); }
                    else { story.AddDeleteFile(name);  }

                    folderView.Update();
                }
            }

        }

        void OnMenuRenameFileOrFolder(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            bool validSelection = true;

            if (FolderViewList.SelectedIndex < 0)
            {
                validSelection = false;
            }
            else if (FolderViewList.SelectedItem as string == Constants.parentFolderName)
            {
                validSelection = false;
            }

            if (!validSelection)
            {
                MessageBox.Show("Selecciona primero el fichero que quieres renombrar", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                string name = ((string?)FolderViewList.SelectedItem ?? "");

                bool isDirectory = name.StartsWith(Constants.folderPrefix);

                if (isDirectory) { name = name.Remove(0, Constants.folderPrefix.Length); }

                MessageBoxResult result;

                if (isDirectory)
                {
                    MessageBox.Show("No se soporta todavía el renombrado de carpetas", "Aviso", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }
                else
                {
                    Debug.Assert(loadedFile != null);



                    bool saveConfirmed = true;

                    if(loadedFileNeedsSave)
                    {
                        result = MessageBox.Show("Debes guardar el fichero '" + loadedFile + "' antes de renombrarlo", "Confirmar guardado", MessageBoxButton.OKCancel);

                        if (result == MessageBoxResult.OK)
                        {
                            story.AddSaveFile(loadedFile, FileEditorTextBox.Text, loadedFileChanges.ToArray());

                            loadedFileNeedsSave = false;
                            loadedFileChanges.Clear();
                            UpdateFileTitle();
                        }
                        else
                        {
                            saveConfirmed = false;
                        }
                    }

                    if (saveConfirmed)
                    {
                        SetUIState(UIState.StoryFileLoadedNoNeedsSave);

                        string oldName = loadedFile;

                        DialogAskName askName = new();
                        askName.Title = "Renombrar fichero";
                        askName.Message.Text = "Introduce el nuevo nombre para el fichero '" + oldName + "'";
                        askName.Input.Text = loadedFile;
                        askName.Input.SelectAll();

                        bool? askResult = askName.ShowDialog();

                        if (askResult.GetValueOrDefault())
                        {
                            string newName = askName.Input.Text?.Trim() ?? "";

                            if (newName.Trim().ToLower() == oldName.Trim().ToLower())
                            {
                                MessageBox.Show("El nuevo nombre no puede ser igual al anterior", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                            else if (!Utils.IsValidFileName(newName))
                            {
                                MessageBox.Show("El nuevo nombre no puede estar vacío o contener carácteres especiales", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                            else if (!Utils.HasAllowedExtension(newName, story.Header.editableExtensions))
                            {
                                string extensions = string.Join(Constants.extensionSeparator, story.Header.editableExtensions);
                                MessageBox.Show("El nuevo fichero debe tener alguna de estas extensiones: " + extensions, "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                            else if (File.Exists(story.RootPath + Path.DirectorySeparatorChar + newName))
                            {
                                MessageBox.Show("Ya existe un fichero con ese nombre", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                            else
                            {
                                story.AddRenameFile(oldName, newName);

                                fileEditor.Load(newName, story.AddLoadFile(newName));
                                loadedFile = newName;
                                loadedFileNeedsSave = false;
                                UpdateFileTitle();

                                folderView.Update(true);
                                folderView.SelectFileByName(newName, true);

                                SetUIState(UIState.StoryFileLoadedNoNeedsSave);

                            }


                        }
                    }
                }

            }

        }



        void CheckFileNeedsSave()
        {
            Debug.Assert(story != null);

            if (loadedFile != null && loadedFileNeedsSave)
            {
                MessageBoxResult result = MessageBox.Show("El fichero '" + loadedFile + "' se ha modificado ¿quieres guardarlo?", "Confirmar guardado", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    story.AddSaveFile(loadedFile, FileEditorTextBox.Text, loadedFileChanges.ToArray());

                    loadedFile = null;
                    loadedFileNeedsSave = false;
                    loadedFileChanges.Clear();
                    UpdateFileTitle();

                    SetUIState(UIState.StoryFileLoadedNoNeedsSave);
                }
            }
        }

        void OnMenuSaveFile(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            string? item = (string?)FolderViewList.SelectedItem;

            Debug.Assert(item != null);

            Debug.Assert(!item.StartsWith(Constants.folderPrefix) && item != Constants.parentFolderName);

            story.AddSaveFile(item, FileEditorTextBox.Text, loadedFileChanges.ToArray());
            loadedFileChanges.Clear();

            if(loadedFileNeedsSave)
            {
                loadedFileNeedsSave = false;

                SetUIState(UIState.StoryFileLoadedNoNeedsSave);
                UpdateFileTitle();
            }
        }

        void OnMenuAboutApp(object sender, RoutedEventArgs e)
        {
            DialogAbout d = new();

            d.ShowDialog();
        }

        private void OnMenuDarkModeChecked(object sender, RoutedEventArgs e)
        {
            if (ignoreUIevents) { return;  }

            themes.DarkMode = true;
        }

        private void OnMenuDarkModeUnchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreUIevents) { return; }

            themes.DarkMode = false;
        }

        private void OnShortcutCopy(object sender, RoutedEventArgs e)
        {
            fileEditor.OnShortcutCopy();
        }

        private void OnShortcutCut(object sender, RoutedEventArgs e)
        {
            fileEditor.OnShortcutCut();
        }

        private void OnShortcutPaste(object sender, RoutedEventArgs e)
        {
            fileEditor.OnShortcutPaste();
        }

        private void OnShortcutGoto(object sender, RoutedEventArgs e)
        {
            fileEditor.OnShortcutGoto();
        }

        private void OnShortcutFind(object sender, RoutedEventArgs e)
        {
            fileEditor.OnShortcutFind();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Settings.Save();
        }

    }
}