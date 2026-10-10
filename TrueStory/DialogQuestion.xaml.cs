using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TrueStory
{
    /// <summary>
    /// Lógica de interacción para DialogQuestion.xaml
    /// </summary>
    public partial class DialogQuestion : Window
    {
        [DllImport("user32.dll")]
        public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        public Instructor.Question Question { get; set; }

        long usedTime;
        DispatcherTimer timer;

        public DialogQuestion()
        {
            InitializeComponent();

            DataObject.AddPastingHandler(AnswerTextBox, OnFileContentsPaste);
            DataObject.AddCopyingHandler(AnswerTextBox, OnFileContentsCopy);

            AnswerTextBox.ContextMenu = null;

            timer = new();

            Loaded += OnLoaded;

            SourceInitialized += OnSourceInitialized;

        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            // Excluye la ventana de las capturas de pantalla
            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity

            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowDisplayAffinity(helper.EnsureHandle(), 0x01);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FlowDocument doc = QuestionDocument.Document;

            doc.Blocks.Clear();

            Paragraph? p = null;

            if (Question.type == Instructor.QuestionType.input)
            {

                Table table = new ();
                TableColumn column = new();
                table.Columns.Add(column);
                TableRowGroup rowGroup = new();
                TableRow row = new();
                TableCell cell = new();

                p = new Paragraph(new Run(Question.context.fileName));
                p.FontSize *= 1.2f;
                doc.Blocks.Add(p);

                foreach (Instructor.ContextLine l in Question.context.lines)
                {
                    p = new Paragraph(new Run((l.number + 1) + ": " + l.text));

                    p.Margin = new Thickness(0);
                    cell.Blocks.Add(p);
                }

                table.BorderBrush = new SolidColorBrush(Colors.LightGray);
                table.BorderThickness = new Thickness(1);
                table.Padding = new Thickness(5);

                row.Cells.Add(cell);
                rowGroup.Rows.Add(row);
                table.RowGroups.Add(rowGroup);

                doc.Blocks.Add(table);
            }
            else if(Question.type == Instructor.QuestionType.command)
            {
                p = new Paragraph(new Run(Constants.consolePrompt + Question.context.command));
                p.TextAlignment = TextAlignment.Center;
                p.FontSize *= 1.2f;
                p.Margin = new Thickness(0);
                doc.Blocks.Add(p);
            }


            p = new Paragraph(new Run(Question.text));
            p.Margin = new Thickness(0, 20, 0, 0);
            p.FontSize *= 1.2f;
            doc.Blocks.Add(p);

            AnswerTextBox.Focus();

            TimeBar.Maximum = Question.timeLimit;
            TimeBar.Minimum = 0;
            TimeBar.Value = Question.timeLimit;
            TimeBar.Foreground = new SolidColorBrush(Colors.Green);

            usedTime = 0;


            timer.Tick += OnTick;
            timer.Interval = TimeSpan.FromSeconds(1);

            timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            usedTime += 1;

            if (usedTime > (Question.timeLimit * Constants.questionDialogHurryUp2))
            {
                TimeBar.Foreground = new SolidColorBrush(Colors.DeepPink);
            }
            else if (usedTime > (Question.timeLimit * Constants.questionDialogHurryUp1))
            {
                TimeBar.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                TimeBar.Foreground = new SolidColorBrush(Colors.Green);
            }


            if (usedTime >= Question.timeLimit)
            {
                usedTime = Question.timeLimit;
                TimeBar.Value = 0;
                AnswerTextBox.IsEnabled = false;
                timer.Stop();
            }
            else
            {
                TimeBar.Value = Question.timeLimit - usedTime;
            }
        }

        void OnFileContentsCopy(object sender, DataObjectCopyingEventArgs e)
        {
            e.CancelCommand();
        }

        void OnFileContentsPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        void Accept_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            timer.Stop();
        }
    }
}
