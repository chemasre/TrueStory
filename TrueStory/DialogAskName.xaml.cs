using System.Windows;
using System.Windows.Input;

namespace TrueStory
{
    /// <summary>
    /// Lógica de interacción para DialogAskName.xaml
    /// </summary>
    public partial class DialogAskName : Window
    {
        public DialogAskName()
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
