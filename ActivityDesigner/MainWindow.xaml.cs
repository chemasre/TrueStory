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
        RecordData.NewHeaderData data;

        HashSet<Control> questionControls;
        HashSet<Control> templateControls;
        HashSet<Control> templateTypeTextControls;
        HashSet<Control> wordSeparatorsControls;
        HashSet<Control> wordSeparatorsExtensionControls;

        bool disableUIEvents;

        int selectedTemplate;
        int selectedExtension;

        public MainWindow()
        {
            disableUIEvents = true;
            InitializeComponent();
            disableUIEvents = false;

            Title = Version.GetFullName();

            data = new();

            questionControls = new()
            {
                QuestionsTimeMinText,
                QuestionsTimeMaxText,
                QuestionsTextMinText,
                QuestionsTextMaxText,
                QuestionsSavesMinText,
                QuestionsSavesMaxText,
                QuestionsOpensMinText,
                QuestionsOpensMaxText,
                QuestionsCommandsMinText,
                QuestionsCommandsMaxText,
                QuestionsObserveMinText,
                QuestionsObserveMaxText,
                QuestionsTimeMinLabel,
                QuestionsTextMinLabel,
                QuestionsSavesMinLabel,
                QuestionsOpensMinLabel,
                QuestionsCommandsMinLabel,
                QuestionsObserveLabel,
                QuestionsCommandsMinLabel,
                QuestionsTab
            };

            templateControls = new()
            {
                QuestionTypeLabel,
                QuestionTypeList,
                QuestionTextLabel,
                QuestionTextText,
                QuestionLinesLabel,
                QuestionLinesText,
                QuestionWordsLabel,
                QuestionWordsText,
                QuestionTimeLabel,
                QuestionTimeText

            };

            templateTypeTextControls = new()
            {
                QuestionLinesLabel,
                QuestionLinesText,
                QuestionWordsLabel,
                QuestionWordsText
            };

            wordSeparatorsControls = new()
            {
                WordSeparatorsTitleLabel,
                WordSeparatorsLabel,
                WordSeparatorsText,
                WordSeparatorsExtensionList,
                WordSeparatorsExtensionTextLabel
            };

            wordSeparatorsExtensionControls = new()
            {
                WordSeparatorsLabel,
                WordSeparatorsText
            };

            selectedTemplate = -1;
            selectedExtension = -1;


            Loaded += OnLoaded;

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
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

        private void UpdateView()
        {
            disableUIEvents = true;

            TitleText.Text = data.title;

            EditableExtensionsText.Text = String.Join(Constants.extensionSeparator, data.editableExtensions);
            QuestionsEnabledCheckbox.IsChecked = data.instructor.enabled;

            bool questionsEnabled = QuestionsEnabledCheckbox.IsChecked.GetValueOrDefault();

            foreach (Control c in questionControls)
            { c.Visibility = questionsEnabled ? Visibility.Visible : Visibility.Hidden; }

            if (questionsEnabled)
            {
                QuestionsTimeMinText.Text = data.instructor.wait.time.min.ToString();
                QuestionsTimeMaxText.Text = data.instructor.wait.time.max.ToString();
                QuestionsTextMinText.Text = data.instructor.wait.characters.min.ToString();
                QuestionsTextMaxText.Text = data.instructor.wait.characters.max.ToString();
                QuestionsSavesMinText.Text = data.instructor.wait.fileSaves.min.ToString();
                QuestionsSavesMaxText.Text = data.instructor.wait.fileSaves.max.ToString();
                QuestionsOpensMinText.Text = data.instructor.wait.fileOpens.min.ToString();
                QuestionsOpensMaxText.Text = data.instructor.wait.fileOpens.max.ToString();
                QuestionsCommandsMinText.Text = data.instructor.wait.commands.min.ToString();
                QuestionsCommandsMaxText.Text = data.instructor.wait.commands.max.ToString();
                QuestionsObserveMinText.Text = data.instructor.observe.time.min.ToString();
                QuestionsObserveMaxText.Text = data.instructor.observe.time.max.ToString();

                TemplatesList.Items.Clear();

                int index = 0;
                foreach (RecordData.QuestionTemplateData template in data.instructor.ask.templates)
                {
                    string label = Constants.questionTypeNames[(int)template.type];

                    TemplatesList.Items.Add((index + 1) + ": [" + label + "] " + template.question);
                    index++;
                }

                if (selectedTemplate < 0 && data.instructor.ask.templates.Length > 0)
                {
                    selectedTemplate = 0;
                }
                else if (selectedTemplate >= data.instructor.ask.templates.Length)
                {
                    selectedTemplate = data.instructor.ask.templates.Length - 1;
                }

                if (selectedTemplate < 0)
                {
                    foreach (Control c in templateControls)
                    { c.Visibility = Visibility.Hidden; }
                }
                else
                {
                    foreach (Control c in templateControls)
                    { c.Visibility = Visibility.Visible; }

                    RecordData.QuestionTemplateData template = data.instructor.ask.templates[selectedTemplate];

                    QuestionTypeList.SelectedIndex = (int)template.type;
                    QuestionTextText.Text = template.question;
                    QuestionTimeText.Text = template.timeLimit.ToString();

                    if (template.type == RecordData.QuestionType.input)
                    {
                        foreach (Control c in templateTypeTextControls)
                        { c.Visibility = Visibility.Visible; }

                        QuestionWordsText.Text = template.contextWords.ToString();
                        QuestionLinesText.Text = template.contextLines.ToString();
                    }
                    else
                    {
                        foreach (Control c in templateTypeTextControls)
                        { c.Visibility = Visibility.Hidden; }
                    }

                    TemplatesList.SelectedIndex = selectedTemplate;
                }

                foreach (RecordData.QuestionChanceData chance in data.instructor.ask.chances)
                {
                    if (chance.type == RecordData.QuestionType.input)
                    {
                        ProbabilityTextText.Text = chance.weight.ToString();
                    }
                    else if (chance.type == RecordData.QuestionType.time)
                    {
                        ProbabilityGeneralText.Text = chance.weight.ToString();
                    }
                    else if (chance.type == RecordData.QuestionType.fileSave)
                    {
                        ProbabilitySavesText.Text = chance.weight.ToString();
                    }
                    else if (chance.type == RecordData.QuestionType.fileOpen)
                    {
                        ProbabilityOpensText.Text = chance.weight.ToString();
                    }
                    else // chance.type == RecordData.QuestionType.commands
                    {
                        ProbabilityCommandsText.Text = chance.weight.ToString();
                    }
                }

                if (data.editableExtensions.Length <= 0)
                {
                    foreach (Control c in wordSeparatorsControls)
                    {
                        c.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    foreach (Control c in wordSeparatorsControls)
                    {
                        c.Visibility = Visibility.Visible;
                    }

                    WordSeparatorsExtensionList.Items.Clear();

                    int extIndex = 0;
                    foreach (RecordData.InstructorObserveWordSeparators wordSeparators in data.instructor.observe.wordSeparators)
                    {
                        WordSeparatorsExtensionList.Items.Add((extIndex + 1) + ": " +wordSeparators.extension);
                        extIndex ++;
                    }

                    if (selectedExtension < 0 && data.instructor.observe.wordSeparators.Length > 0)
                    {
                        selectedExtension = 0;
                    }
                    else if (selectedExtension >= data.instructor.observe.wordSeparators.Length)
                    {
                        selectedExtension = data.instructor.observe.wordSeparators.Length - 1;
                    }

                    if (selectedExtension < 0)
                    {
                        foreach (Control c in wordSeparatorsExtensionControls)
                        {
                            c.Visibility = Visibility.Hidden;
                        }
                    }
                    else
                    {
                        foreach (Control c in wordSeparatorsExtensionControls)
                        {
                            c.Visibility = Visibility.Visible;
                        }

                        RecordData.InstructorObserveWordSeparators wordSeparators = data.instructor.observe.wordSeparators[selectedExtension];

                        WordSeparatorsText.Text = new String(wordSeparators.separators);
                    }
                }
            }

            disableUIEvents = false;
        }

        private void OnQuestionsChecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.instructor.enabled = true;
            UpdateView();
        }

        private void OnQuestionsUnchecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.instructor.enabled = false;
            UpdateView();
        }


        private void OnSelectedTemplateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            selectedTemplate = TemplatesList.SelectedIndex;
            UpdateView();

        }
    }
}