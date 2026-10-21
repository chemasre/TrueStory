using System.Windows;
using System.Windows.Input;

namespace TrueStory
{
    /// <summary>
    /// Lógica de interacción para DialogGotoLine.xaml
    /// </summary>
    public partial class DialogGotoLine : Window
    {
        public int Line { get { return line;  }  }
        public int MaxLines { get; set; }

        int line;

        string validChars = "0123456789";

        public DialogGotoLine()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            line = Int32.Parse(Input.Text) - 1;

            Close();
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

        private void Input_LostFocus(object sender, RoutedEventArgs e)
        {
            int n;
            if (!Int32.TryParse(Input.Text, out n)) { Input.Text = "1"; }
            else if (n == 0) { Input.Text = "1"; }
            else if (n > MaxLines) { Input.Text = MaxLines.ToString();  }
        }

        private void Input_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool allValid = true;
            foreach (char c in e.Text)
            {
                if (!validChars.Contains(c)) { allValid = false;  }
            }

            if(!allValid) { e.Handled = true; }
        }
    }
}
