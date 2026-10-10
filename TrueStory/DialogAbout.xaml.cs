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
    /// Lógica de interacción para DialogAbout.xaml
    /// </summary>
    public partial class DialogAbout : Window
    {
        public DialogAbout()
        {
            InitializeComponent();

            FlowDocument doc = Document.Document;
            Paragraph p;

            p = new Paragraph(new Run(Version.GetVersion(false)));
            p.TextAlignment = TextAlignment.Center;
            p.Margin = new Thickness(0, 5, 0, 10);

            doc.Blocks.InsertAfter(doc.Blocks.FirstBlock, p);

            p = new Paragraph(new Bold(new Run(Version.AppName)));
            p.FontSize *= 1.2f;
            p.TextAlignment = TextAlignment.Center;
            p.Margin = new Thickness(0, 0, 0, 5);

            doc.Blocks.InsertBefore(doc.Blocks.FirstBlock.NextBlock, p);

            Title += Version.AppName;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
