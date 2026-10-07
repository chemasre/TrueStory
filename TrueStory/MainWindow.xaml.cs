using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrueStory.Source;


namespace TrueStory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string? LoadedFile { get { return loadedFile;  } }
        public FileEditor FileEditor { get { return fileEditor; } }

        Story? story;
        StoryActions? performer;

        string? loadedFile;
        List<Story.FileChangeData> loadedFileChanges;
        bool loadedFileNeedsSave;

        string? selectedFolder;

        enum UIState
        {
            NoStory,
            StoryNoSelection,
            StoryFileLoadedNoNeedsSave,
            StoryFileLoadedNeedsSave,
            StoryFolderSelected
        }

        UIState uiState;

        FileEditor fileEditor;
        FolderView folderView;
        CommandConsole commandConsole;
        Instructor instructor;

        public static RoutedCommand ShortcutNewFile = new RoutedCommand();
        public static RoutedCommand ShortcutSaveFile = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            Closing += OnClosing;

            fileEditor = new(this);
            fileEditor.OnContentsChanged += OnFileContentsChanged;

            folderView = new FolderView(FolderViewList);
            folderView.OnDeselected += OnFolderViewDeselected;
            folderView.OnFileSelected += OnFolderViewFileSelected;
            folderView.OnFolderSelected += OnFolderViewFolderSelected;

            commandConsole = new(CommandConsoleTextBox);
            commandConsole.OnExecuteCommand += OnExecuteCommand;

            instructor = new(this);

            ShortcutSaveFile.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            ShortcutNewFile.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));

            loadedFileChanges = new();

            if (Switches.NoFolders)
            {
                NewFolderMenuAction.Visibility = Visibility.Collapsed;
            }

            SetUIState(UIState.NoStory);

            UpdateFileTitle();

            Title = Version.FullName();
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (story == null) { return;  }

            CheckFileNeedsSave();
        }

        private string OnExecuteCommand(string command)
        {
            Debug.Assert(story != null);

            return story.ExecuteCommand(command);
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

            fileEditor.Load(story.LoadFile(file));
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
            }
            else // uiState == UIState.StoryFolderSelected
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
                EditMenu.IsEnabled = false;
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

            Story.FileChangeData fc = new();
            fc.Offset = c.Offset;
            fc.Added = c.Added;
            fc.AddedText = c.AddedText;
            fc.Removed = c.Removed;
            fc.DeltaTime = c.DeltaTime;

            loadedFileChanges.Add(fc);
        }

        void OnNewActivity(object sender, RoutedEventArgs e)
        {
            DialogActivityProperties dialog = new();

            bool ? result = dialog.ShowDialog();


            if(result.GetValueOrDefault())
            {
                story = new Story(dialog.ActivityFolderText.Text.Trim(), Story.Mode.Create, dialog.Header);
                performer = new StoryActions(story);

                SetUIState(UIState.StoryNoSelection);

                UpdateFileTitle();

                folderView.RootPath = story.RootPath;
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
                    performer = new StoryActions(story);

                    SetUIState(UIState.StoryNoSelection);

                    UpdateFileTitle();

                    folderView.RootPath = story.RootPath;


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

            FileEditorTextBox.Clear();
            loadedFile = null;

            FolderViewList.Items.Clear();
            commandConsole.Clear();

            Debug.Assert(performer != null);

            performer.Dispose();
            performer = null;

            story.Dispose();
            story = null;

            SetUIState(UIState.NoStory);
            UpdateFileTitle();

        }

        void UpdateFileTitle()
        {
            if (story != null && loadedFile != null) { FileEditorTitleLabel.Content = (loadedFileNeedsSave ? "[*]":"") + loadedFile + ""; }
            else { FileEditorTitleLabel.Content = "Editor de ficheros"; }

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
            dialog.Message.Text = "Introduce un nombre para el fichero.";
            dialog.Input.Text = "NuevoFichero.txt";

            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string name = dialog.Input.Text;


                if (!Utils.IsValidFileName(name))
                {
                    MessageBox.Show("El nombre del fichero no puede estar vacío o contener carácteres especiales", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if (!Utils.HasAllowedExtension(name))
                {
                    string extensions = string.Join(';', Constants.allowedExtensions);
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
                    result = MessageBox.Show("Vas a eliminar el fichero '" + name + "' estás seguro/a?", "Confirmar eliminación", MessageBoxButton.YesNo);
                }

                if (result == MessageBoxResult.Yes)
                {
                    if (isDirectory) { story.AddDeleteDirectory(name); }
                    else { story.AddDeleteFile(name);  }

                    folderView.Update();
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
                    story.SaveFile(loadedFile, FileEditorTextBox.Text, loadedFileChanges.ToArray());

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

            story.SaveFile(item, FileEditorTextBox.Text, loadedFileChanges.ToArray());
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

    }
}