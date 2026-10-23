using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using TrueStory;

namespace StoryPlayer
{
    /// <summary>
    /// Lógica de interacción para QuestionWindow.xaml
    /// </summary>
    public partial class QuestionWindow : Window
    {
        public RecordData.QuestionData Question
        {
            set
            {
                FlowDocument doc = QuestionDocument.Document;

                doc.Blocks.Clear();

                Paragraph? p = null;

                if (value.type == RecordData.QuestionType.input)
                {

                    Table table = new();
                    TableColumn column = new();
                    table.Columns.Add(column);
                    TableRowGroup rowGroup = new();
                    TableRow row = new();
                    TableCell cell = new();

                    p = new Paragraph(new Run(value.context.fileName));
                    p.FontSize *= 1.2f;
                    doc.Blocks.Add(p);

                    foreach (RecordData.QuestionContextLineData l in value.context.lines)
                    {
                        p = new Paragraph(new Run((l.number + 1) + ": " + l.text));

                        p.Margin = new Thickness(0);
                        cell.Blocks.Add(p);
                    }

                    table.BorderBrush = new SolidColorBrush(Colors.LightGray);
                    table.Foreground= (Brush)App.Current.Resources["StoryPlayerEditableText"];
                    table.Background= (Brush)App.Current.Resources["StoryPlayerEditableTextBackground"];;
                    table.BorderThickness = new Thickness(1);
                    table.Padding = new Thickness(5);


                    row.Cells.Add(cell);
                    rowGroup.Rows.Add(row);
                    table.RowGroups.Add(rowGroup);

                    doc.Blocks.Add(table);
                }
                else if (value.type == RecordData.QuestionType.command)
                {
                    p = new Paragraph(new Run(Constants.consolePrompt + value.context.command));
                    p.TextAlignment = TextAlignment.Center;
                    p.FontSize *= 1.2f;
                    p.Margin = new Thickness(0);
                    p.Foreground= (Brush)App.Current.Resources["StoryPlayerEditableText"];;
                    p.Background= (Brush)App.Current.Resources["StoryPlayerEditableTextBackground"];;

                    doc.Blocks.Add(p);
                }

                p = new Paragraph(new Run(value.text));
                p.Margin = new Thickness(0, 20, 0, 0);
                p.FontSize *= 1.2f;
                doc.Blocks.Add(p);

                TimeBar.Maximum = value.timeLimit;
                TimeBar.Minimum = 0;
                TimeBar.Value = value.timeLimit - value.usedTime;

                AnswerTextBox.Text = value.answer;


            }
        }

        // This hides the close button
        const int GWL_STYLE = -16;
        const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public QuestionWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            nint hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
