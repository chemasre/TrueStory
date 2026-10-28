using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;

namespace TrueStory
{
    /// <summary>
    /// Lógica de interacción para DialogActivityProperties.xaml
    /// </summary>
    public partial class DialogActivityProperties : Window
    {
        public RecordData.NewHeaderData NewHeader { get { return newHeader; } }

        RecordData.NewHeaderData newHeader;

        HashSet<Control> activityFileControls;
        HashSet<Control> noActivityFileControls;

        bool disableUIEvents;


        public DialogActivityProperties()
        {
            disableUIEvents = true;
            InitializeComponent();
            disableUIEvents = false;

            ActivityTitleText.Text = "Título de la actividad";
            ActivityExtensionsText.Text = "h,c,hpp,cpp,txt,html,html,css,js,php,cs,tsx,json";
            StudentNameText.Text = "Nombre Apellido";
            ActivityFolderText.Text = "";

            activityFileControls = new();
            noActivityFileControls = new();

            activityFileControls.Add(ActivityFileText);
            activityFileControls.Add(ActivityFileLabel);
            activityFileControls.Add(PickActivityFileButton);

            noActivityFileControls.Add(ActivityTitleText);
            noActivityFileControls.Add(ActivityTitleLabel);
            noActivityFileControls.Add(ActivityExtensionsLabel);
            noActivityFileControls.Add(ActivityExtensionsText);

            newHeader = new();

            UpdateUI();


        }

        private void UpdateUI()
        {
            if (UseActivityFileRadio.IsChecked.GetValueOrDefault())
            {
                foreach (Control c in activityFileControls) { c.IsEnabled = true; }
                foreach (Control c in noActivityFileControls) { c.IsEnabled = false; }
            }
            else
            {
                foreach (Control c in activityFileControls) { c.IsEnabled = false; }
                foreach (Control c in noActivityFileControls) { c.IsEnabled = true; }
            }
        }

        private void PickActivityFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new();

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FolderName;

                if (Story.ExistsStory(path))
                {
                    MessageBox.Show("Ya hay una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    ActivityFolderText.Text = path;
                }
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            bool passes = true;

            if (UseActivityFileRadio.IsChecked.GetValueOrDefault())
            {
                if (ActivityFileText.Text.Trim().Length <= 0)
                { MessageBox.Show("Debes elegir un fichero de configuración para la actividad.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

                if (passes && !File.Exists(ActivityFileText.Text.Trim()))
                { MessageBox.Show("El fichero de configuración para la actividad no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }
            }
            else
            {
                if (ActivityTitleText.Text.Trim().Length <= 0)
                { MessageBox.Show("Debes escribir un título para la actividad.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

                string[] extensions = ActivityExtensionsText.Text.Split(Constants.extensionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (extensions.Length <= 0)
                { MessageBox.Show("Debes incluir al menos una extensión editable.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }
            }

            if (ActivityFolderText.Text.Trim().Length <= 0)
            { MessageBox.Show("Debes elegir una carpeta para la actividad.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

            if (passes && !Directory.Exists(ActivityFolderText.Text))
            { MessageBox.Show("La carpeta de actividad no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

            if (passes && Story.ExistsStory(ActivityFolderText.Text))
            { MessageBox.Show("Ya existe una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }


            if (passes && StudentNameText.Text.Trim().Length <= 0)
            { MessageBox.Show("Debes introducir tu nombre y apellido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

            if (passes)
            {
                if (DontUseActivityFileRadio.IsChecked.GetValueOrDefault())
                {
                    newHeader = new RecordData.NewHeaderData()
                    {
                        title = ActivityTitleText.Text.Trim(),
                        student = StudentNameText.Text.Trim(),
                        editableExtensions = ActivityExtensionsText.Text.Split(Constants.extensionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    };

                    newHeader.instructor.enabled = false;
                }
                else
                {
                    Story.Record record = new Story.Record();
                    record.type = Story.RecordType.Header;
                    record.data = File.ReadAllBytes(ActivityFileText.Text.Trim());

                    newHeader = RecordData.DecodeNewHeaderRecord(record);
                }

                newHeader.guid = Guid.NewGuid().ToString();

                DialogResult = true;

                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!DialogResult.HasValue) { DialogResult = false; }
        }

        private void PickActivityFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "activity files (*."+ Constants.activitySettingsExtension + ")|*." + Constants.activitySettingsExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                ActivityFileText.Text = dialog.FileName;
            }
        }

        private void DontUseActivityFileRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return;  }

            UpdateUI();
        }

        private void DontUseActivityFileRadio_Unchecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            UpdateUI();
        }

        private void UseActivityFileRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            UpdateUI();
        }

        private void UseActivityFileRadio_Unchecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            UpdateUI();
        }

        private void OnExtensionsChanged(object sender, TextChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            int previousCaret = ActivityExtensionsText.CaretIndex;
            int removed = ActivityExtensionsText.Text.Count(c => c == ' ');
            ActivityExtensionsText.Text = ActivityExtensionsText.Text.Replace(' '.ToString(), "");
            foreach (char c in Constants.forbiddenExtensionChars)
            {
                removed += ActivityExtensionsText.Text.Count(cc => cc == c);
                ActivityExtensionsText.Text = ActivityExtensionsText.Text.Replace(c.ToString(), "");
            }

            if (previousCaret - removed >= 0 && previousCaret - removed <= ActivityExtensionsText.Text.Length)
            {
                ActivityExtensionsText.CaretIndex = previousCaret - removed;
            }


        }
    }
}
