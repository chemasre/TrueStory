using Microsoft.Win32;
using System;
using System.ComponentModel;
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
        Story? story;
        StoryActions? performer;

        string? loadedFile;
        List<Story.FileChange> loadedFileChanges;
        bool loadedFileNeedsSave;

        string? selectedFolder;

        enum UIState
        {
            NoStory,
            StoryNoSelection,
            StoryFileLoadedNoNeedsSave,
            StoryFileSelectedNeedsSave,
            StoryFolderSelected
        }

        UIState uiState;

        FileEditor fileEditor;
        FolderView folderView;
        CommandConsole commandConsole;



        public MainWindow()
        {
            InitializeComponent();

            fileEditor = new(FileEditorTextBox, FileEditorInfoBar);
            fileEditor.OnContentsChanged += OnFileContentsChanged;

            folderView = new FolderView(FolderView);
            folderView.OnDeselected += OnFolderViewDeselected;
            folderView.OnFileSelected += OnFolderViewFileSelected;
            folderView.OnFolderSelected += OnFolderViewFolderSelected;

            commandConsole = new(CommandConsoleTextBox);
            commandConsole.OnExecuteCommand += OnExecuteCommand;

            loadedFileChanges = new();


            SetUIState(UIState.NoStory);

            UpdateTitle();

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

            fileEditor.Clear();

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
            UpdateTitle();

        }

        private void OnFolderViewDeselected()
        {
            selectedFolder = null;

            CheckFileNeedsSave();

            fileEditor.Clear();

            SetUIState(UIState.StoryNoSelection);
        }

        void SetUIState(UIState _uiState)
        {
            if (_uiState == UIState.NoStory)
            {
                NewActivityMenuAction.IsEnabled = true;
                OpenActivityMenuAction.IsEnabled = true;
                CloseActivityMenuAction.IsEnabled = false;
                FileEditorTextBox.IsEnabled = false;
                FolderView.IsEnabled = false;
                CommandConsoleTextBox.IsEnabled = false;
                FileMenu.IsEnabled = false;
            }
            else if (_uiState == UIState.StoryNoSelection)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                FileEditorTextBox.IsEnabled = false;
                FolderView.IsEnabled = true;
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
                FolderView.IsEnabled = true;
                CommandConsoleTextBox.IsEnabled = true;
                FileMenu.IsEnabled = true;
                NewMenu.IsEnabled = true;
                NewFileMenuAction.IsEnabled = true;
                NewFolderMenuAction.IsEnabled = true;
                SaveFileMenuAction.IsEnabled = false;
                DeleteFileOrFolderMenuAction.IsEnabled = true;
            }
            else if (_uiState == UIState.StoryFileSelectedNeedsSave)
            {
                NewActivityMenuAction.IsEnabled = false;
                OpenActivityMenuAction.IsEnabled = false;
                CloseActivityMenuAction.IsEnabled = true;
                FileEditorTextBox.IsEnabled = true;
                FolderView.IsEnabled = true;
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
                FolderView.IsEnabled = true;
                CommandConsoleTextBox.IsEnabled = true;
                FileMenu.IsEnabled = true;
                NewMenu.IsEnabled = true;
                NewFileMenuAction.IsEnabled = true;
                NewFolderMenuAction.IsEnabled = true;
                SaveFileMenuAction.IsEnabled = false;
                DeleteFileOrFolderMenuAction.IsEnabled = true;
            }


            uiState = _uiState;
        }

        void OnFileContentsChanged(FileEditor.ContentChange c)
        {
            Debug.Assert(story != null);

            if (!loadedFileNeedsSave)
            {
                loadedFileNeedsSave = true;
                SetUIState(UIState.StoryFileSelectedNeedsSave);
                UpdateTitle();
            }

            Story.FileChange fc = new();
            fc.Offset = c.Offset;
            fc.Added = c.Added;
            fc.AddedText = c.AddedText;
            fc.Removed = c.Removed;

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

                UpdateTitle();

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

                    UpdateTitle();

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
            FolderView.Items.Clear();
            commandConsole.Clear();

            Debug.Assert(performer != null);

            performer.Dispose();
            performer = null;

            story.Dispose();
            story = null;

            SetUIState(UIState.NoStory);
            UpdateTitle();

        }



        void UpdateTitle()
        {
            string caption;

            if (story == null) { caption = ""; }
            else if (loadedFile != null) { caption = " [" + (loadedFileNeedsSave ? "* ":"") + loadedFile + "]"; }
            else { caption = " " + story.RootPath; }

            Title = Version.FullName() + " " + caption;
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

            if(FolderView.SelectedIndex < 0)
            {
                validSelection = false;
            }
            else if(FolderView.SelectedItem as string == Constants.parentFolderName)
            {
                validSelection = false;
            }

            if(!validSelection)
            {
                MessageBox.Show("Selecciona primero el fichero o carpeta que quieres eliminar", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                string name = ((string?)FolderView.SelectedItem ?? "");

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
                    UpdateTitle();

                    SetUIState(UIState.StoryFileLoadedNoNeedsSave);
                }
            }
        }

        void OnMenuSaveFile(object sender, RoutedEventArgs e)
        {
            Debug.Assert(story != null);

            string? item = (string?)FolderView.SelectedItem;

            Debug.Assert(item != null);

            Debug.Assert(!item.StartsWith(Constants.folderPrefix) && item != Constants.parentFolderName);

            story.SaveFile(item, FileEditorTextBox.Text, loadedFileChanges.ToArray());
            loadedFileChanges.Clear();

            if(loadedFileNeedsSave)
            {
                loadedFileNeedsSave = false;

                SetUIState(UIState.StoryFileLoadedNoNeedsSave);
                UpdateTitle();
            }
        }

        void OnMenuAboutApp(object sender, RoutedEventArgs e)
        {
            DialogAbout d = new();

            d.ShowDialog();
        }
    }
}