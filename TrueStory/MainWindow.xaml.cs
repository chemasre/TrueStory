using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace TrueStory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string parentFolderName = "..";
        const string folderPrefix = "[Carpeta] ";

        const string tabAsSpaces = "    ";
        char[] tabEquivalents = new char[] { ' ', '\t' };

        string? clipboard;

        Story? story;
        public MainWindow()
        {
            InitializeComponent();

            DataObject.AddPastingHandler(FileContents, FileContents_OnPaste);
            DataObject.AddCopyingHandler(FileContents, FileContents_OnCopy);

            FileContents.ContextMenu = null;
        }

        private void FileContents_OnCopy(object sender, DataObjectCopyingEventArgs e)
        {
            bool isText = e.DataObject.GetDataPresent(DataFormats.UnicodeText, true);

            if (isText)
            {
                string text = (string)e.DataObject.GetData(DataFormats.UnicodeText);

                Console.WriteLine("Detected copy: " + text);

                clipboard = text;

                if(Keyboard.IsKeyDown(Key.X))
                {
                    if (FileContents.SelectionLength > 0) { FileContents.SelectedText = "";  }
                }

            }
            else
            {
                Console.WriteLine("Unrecognized copy");
            }

            e.CancelCommand();
            Console.WriteLine("Copy cancelled");

        }

        private void FileContents_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            bool isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);

            if(isText)
            {
                string text = (string)e.SourceDataObject.GetData(DataFormats.UnicodeText);
                e.FormatToApply = DataFormats.Text;

                Console.WriteLine("Detected paste: " + text);


                if(clipboard != null)
                {
                    if (FileContents.SelectionLength > 0)
                    {
                        FileContents.SelectedText = text;
                    }
                    else
                    {
                        int p = FileContents.CaretIndex;
                        FileContents.Text = FileContents.Text.Insert(FileContents.CaretIndex, clipboard);
                        FileContents.CaretIndex = p + clipboard.Length;
                    }
                }



            }
            else
            {
                Console.WriteLine("Unrecognized paste");
            }

            e.CancelCommand();
            Console.WriteLine("Paste cancelled");
        }

        private void FileContents_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Console.WriteLine("Detected input: " + e.Text);
        }

        private void NewStoryMenuAction_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new ();

            bool? result = dialog.ShowDialog();

            if(result.GetValueOrDefault())
            {
                string path = dialog.FolderName;

                if (Story.ExistsStory(path))
                {
                    MessageBox.Show("Ya hay una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    story = Story.CreateStory(path);

                    UpdateTitle();
                    UpdateFolderView();
                }
            }
        }

        private void UpdateFolderView()
        {
            if (story == null) { return;  }


            FolderView.Items.Clear();
            
            string[] directories = Directory.GetDirectories(story.RootPath);
            foreach(string d in directories)
            { FolderView.Items.Add(folderPrefix + Path.GetFileName(d));  }

            string[] files = Directory.GetFiles(story.RootPath);
            foreach(string f in files)
            {
                string n = Path.GetFileName(f);
                if(n != Story.storyFilename) { FolderView.Items.Add(n); }
            }


        }

        private void OpenStoryMenuAction_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new();

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FolderName;

                if (Story.ExistsStory(path))
                {
                    story = Story.OpenStory(path);
                    UpdateTitle();
                    UpdateFolderView();

                }
                else
                {
                    MessageBox.Show("No hay una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void CloseStoryMenuAction_Click(object sender, RoutedEventArgs e)
        {
            if(story == null)
            {
                MessageBox.Show("No hay una historia abierta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                story.Close();
                story = null;
                UpdateTitle();
            }
        }



        private void UpdateTitle()
        {
            Title = "TrueStory 0.1.0" + (story == null ? "" : story.RootPath);
        }

        private void NewFolderMenuAction_Click(object sender, RoutedEventArgs e)
        {
            if (story == null) { return;  }

            DialogAskName dialog = new();

            dialog.Title = "Crear nueva carpeta";
            dialog.Message.Text = "Introduce un nombre para la nueva carpeta.";
            dialog.Input.Text = "NuevaCarpeta";

            bool? result = dialog.ShowDialog();

            if(result.HasValue && result.Value)
            {
                string name = dialog.Input.Text;

                if (!IsValidFolderName(name))
                {
                    MessageBox.Show("El nombre de la carpeta no puede estar vacío o contener carácteres especiales", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if(Directory.Exists(story.RootPath + Path.DirectorySeparatorChar + name))
                {
                    MessageBox.Show("Ya existe una carpeta con ese nombre", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    story.CreateFolder(name);
                    UpdateFolderView();
                }

            }

        }
        private void NewFileMenuAction_Click(object sender, RoutedEventArgs e)
        {
            if (story == null) { return; }

            DialogAskName dialog = new();

            dialog.Title = "Crear nuevo fichero";
            dialog.Message.Text = "Introduce un nombre para el fichero.";
            dialog.Input.Text = "NuevoFichero.txt";

            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string name = dialog.Input.Text;


                if(!IsValidFileName(name))
                {
                    MessageBox.Show("El nombre del fichero no puede estar vacío o contener carácteres especiales", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if (File.Exists(story.RootPath + Path.DirectorySeparatorChar + name))
                {
                    MessageBox.Show("Ya existe un fichero con ese nombre", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    story.CreateFile(name);
                    UpdateFolderView();
                }

            }
        }
        private void DeleteFileOrFolderMenuAction_Click(object sender, RoutedEventArgs e)
        {
            if (story == null) { return; }

            bool validSelection = true;

            if(FolderView.SelectedIndex < 0)
            {
                validSelection = false;
            }
            else if(FolderView.SelectedItem as string == parentFolderName)
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

                bool isDirectory = name.StartsWith(folderPrefix);

                if (isDirectory) { name = name.Remove(0, folderPrefix.Length); }

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
                    if (isDirectory) { story.DeleteDirectory(name); }
                    else { story.DeleteFile(name);  }

                    UpdateFolderView();
                }
            }

        }

        private bool IsValidFolderName(string text)
        {
            if (text.Length == 0) { return false;  }

            char[] invalid = Path.GetInvalidPathChars();
            bool valid = true;
            int i = 0;
            while (valid && i < invalid.Length)
            {
                valid = valid && !text.Contains(invalid[i]);

                if (valid) { i++;  }
            }
            
            return valid;
        }

        private bool IsValidFileName(string text)
        {
            if (text.Length == 0) { return false; }

            char[] invalid = Path.GetInvalidFileNameChars();
            bool valid = true;
            int i = 0;
            while (valid && i < invalid.Length)
            {
                valid = valid && !text.Contains(invalid[i]);

                if (valid) { i++; }
            }

            return valid;
        }

        private void FileContents_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (TextChange c in e.Changes)
            {
                Console.WriteLine("Text changed at " + c.Offset + ": added length " + c.AddedLength + ", removed length " + c.RemovedLength);
            }

            if(e.Changes.Count == 1)
            {
                TextChange c = e.Changes.First();
                if(c.AddedLength > 0 && c.RemovedLength == 0)
                {
                    Console.WriteLine("Added text " + FileContents.Text.Substring(c.Offset, c.AddedLength));
                }
            }

        }

        private void FileContents_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Tab)
            {
                int p = FileContents.CaretIndex;
                FileContents.Text = FileContents.Text.Insert(p, tabAsSpaces);
                FileContents.CaretIndex = p + tabAsSpaces.Length;

                e.Handled = true;
            }
            if (e.Key == Key.Return)
            {
                Console.WriteLine("Detected return");

                int lineIndex = FileContents.GetLineIndexFromCharacterIndex(FileContents.CaretIndex);
                string lineText = FileContents.GetLineText(lineIndex);

                int lastTabEquivalent = FindLastTabEquivalent(lineText);

                string? tabPrefix = null;
                if (lastTabEquivalent >= 0) { tabPrefix = lineText.Substring(0, lastTabEquivalent);  }

                int p = FileContents.CaretIndex;
                string newLineText = Environment.NewLine + (tabPrefix ?? "");
                FileContents.Text = FileContents.Text.Insert(p, newLineText);
                FileContents.CaretIndex = p + newLineText.Length;

                e.Handled = true;
            }
        }

        int FindLastTabEquivalent(string lineText)
        {
            int i = 0;
            bool found = false;
            int lastTabEquivalent = -1;

            while (i < lineText.Length && !found)
            {
                int j = 0;
                bool isTabEquivalent = false;
                while (j < tabEquivalents.Length && !isTabEquivalent)
                {
                    if (lineText[i] == tabEquivalents[j]) { isTabEquivalent = true; }
                    else { j++; }
                }

                if (!isTabEquivalent) { found = true; lastTabEquivalent = i; }
                else
                {
                    i++;
                }
            }

            return lastTabEquivalent;
        }
    }
}