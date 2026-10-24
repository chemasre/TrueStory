using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TrueStory;

namespace ActivityDesigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RecordData.NewHeaderData data = new();

        public MainWindow()
        {
            InitializeComponent();

            Title = Version.GetFullName();
        }


        void OnMenuOpenActivity(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "activity files (*.activity)|*.activity";
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                MessageBox.Show("Carpeta " + path , "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        void OnMenuCloseActivity(object sender, RoutedEventArgs e)
        {

        }


        private void OnClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnMenuNewActivity(object sender, RoutedEventArgs e)
        {
            data = new();
        }

        private void OnMenuSaveActivityAs(object sender, RoutedEventArgs e)
        {

        }

        private void OnMenuSaveActivity(object sender, RoutedEventArgs e)
        {

        }
    }
}