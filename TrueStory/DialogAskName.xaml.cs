using System.Windows;

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
