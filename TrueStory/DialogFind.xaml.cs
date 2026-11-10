using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Lógica de interacción para DialogFind.xaml
    /// </summary>
    public partial class DialogFind : Window
    {
        public DialogFind()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Input.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                DialogResult = true;
                Close();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Input.Focus();
            Input.SelectAll();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!DialogResult.HasValue) { DialogResult = false; }
        }
    }
}
