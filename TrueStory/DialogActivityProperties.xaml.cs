using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TrueStory
{
    /// <summary>
    /// Lógica de interacción para DialogActivityProperties.xaml
    /// </summary>
    public partial class DialogActivityProperties : Window
    {
        public Story.Header Header { get { return header;  }  }

        Story.Header header;

        public DialogActivityProperties()
        {
            InitializeComponent();

            ActivityCodeText.Text = "00000000";
            StudentNameText.Text = "";
            ActivityFolderText.Text = "";

            header = new();

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


            if (ActivityFolderText.Text.Trim().Length <= 0)
            { MessageBox.Show("Debes elegir una etiqueta para la actividad.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

            if (passes && !Directory.Exists(ActivityFolderText.Text))
            { MessageBox.Show("La carpeta de actividad no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

            if (passes && Story.ExistsStory(ActivityFolderText.Text))
            { MessageBox.Show("Ya existe una historia en esa carpeta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }


            if (passes && StudentNameText.Text.Trim().Length <= 0)
            { MessageBox.Show("Debes introducir tu nombre y apellido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation); passes = false; }

            if (passes)
            {
                header = new Story.Header() { author = StudentNameText.Text.Trim() , publicKey = ActivityCodeText.Text.Trim() };


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
    }
}
