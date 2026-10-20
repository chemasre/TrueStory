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
using TrueStory;

namespace StoryPlayer
{
    /// <summary>
    /// Lógica de interacción para QuestionWindow.xaml
    /// </summary>
    public partial class QuestionWindow : Window
    {
        public Story.QuestionData Question
        {
            set
            {
                FlowDocument doc = QuestionDocument.Document;

                doc.Blocks.Clear();

                Paragraph? p = null;

                if (value.type == Story.QuestionType.input)
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

                    foreach (Story.QuestionContextLineData l in value.context.lines)
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
                else if (value.type == Story.QuestionType.command)
                {
                    p = new Paragraph(new Run(Constants.consolePrompt + value.context.command));
                    p.TextAlignment = TextAlignment.Center;
                    p.FontSize *= 1.2f;
                    p.Margin = new Thickness(0);
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



        public QuestionWindow()
        {
            InitializeComponent();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
