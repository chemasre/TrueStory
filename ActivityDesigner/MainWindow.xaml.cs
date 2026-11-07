using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
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
        enum DataChangeType
        {
            All,
            QuestionsEnabled,
            TemplateListModified,
            ExtensionSettingsModified,
            QuestionChances
        }

        RecordData.NewHeaderData data;

        HashSet<Control> questionControls;
        HashSet<Control> templateControls;
        HashSet<Control> templateTypeTextControls;
        HashSet<Control> wordSeparatorsControls;
        HashSet<Control> wordSeparatorsExtensionControls;

        bool disableUIEvents;

        int selectedTemplate;
        int selectedExtension;

        string? savePath;
        bool needsSave;

        //////////////////////
        /// INITIALIZATION ///
        //////////////////////

        public MainWindow()
        {
            disableUIEvents = true;
            InitializeComponent();
            disableUIEvents = false;

            data = new();

            questionControls = new()
            {
                QuestionsWaitLabel,
                QuestionsWaitProfileLoad,
                QuestionsWaitProfileSave,
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

            savePath = null;
            needsSave = false;

            UpdateTitle();


            Loaded += OnLoaded;

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }

        /////////////////////////////////
        /// NEW, SAVE, SAVE AS, CLOSE ///
        /////////////////////////////////

        private void OnMenuNewActivity(object sender, RoutedEventArgs e)
        {
            bool cancel = CheckNeedsSave();

            if (cancel) { return; }

            data = new();
            savePath = null;
            needsSave = false;

            UpdateView();

        }

        void OnMenuOpenActivity(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "activity files (*." + Constants.activitySettingsExtension + ")|*." + Constants.activitySettingsExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                Story.Record record;
                record.type = Story.RecordType.Header;
                record.data = File.ReadAllBytes(path);

                data = RecordData.DecodeNewHeaderRecord(record);

                savePath = path;
                needsSave = false;
                UpdateTitle();

                UpdateView();


            }

        }

        private void OnMenuSaveActivityAs(object sender, RoutedEventArgs e)
        {
            SaveActivityAs();
        }

        private void OnMenuSaveActivity(object sender, RoutedEventArgs e)
        {
            SaveActivity();
        }

        private void OnExitMenuAction(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel = CheckNeedsSave();

            e.Cancel = cancel;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        bool CheckNeedsSave()
        {
            bool cancelled = false;

            if (needsSave)
            {
                MessageBoxResult result = MessageBox.Show("¿Quieres guardar la actividad?", "Pregunta", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveActivity();
                }
                else if (result == MessageBoxResult.No)
                {
                    // Nothing
                }
                else
                {
                    cancelled = true;
                }

            }

            return cancelled;

        }

        void SaveActivity()
        {
            if (savePath == null)
            {
                SaveActivityAs();
            }
            else
            {
                Story.Record record = RecordData.EncodeNewHeaderRecord(data);
                File.WriteAllBytes(savePath, record.data);

                needsSave = false;
                UpdateTitle();
            }
        }

        void SaveActivityAs()
        {
            SaveFileDialog dialog = new();

            dialog.Filter = "activity files (*." + Constants.activitySettingsExtension + ")|*." + Constants.activitySettingsExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                Story.Record record = RecordData.EncodeNewHeaderRecord(data);
                File.WriteAllBytes(path, record.data);

                savePath = path;
                needsSave = false;
                UpdateTitle();

            }
        }

        ///////////////////////
        /// MODIFY ACTIVITY ///
        ///////////////////////

        private void UpdateTitle()
        {
            Title = (needsSave ? "[*] " : "") + (savePath == null ? "Nueva actividad" : savePath) + " - " + Version.GetFullName();
        }

        private void UpdateView(DataChangeType changeType = DataChangeType.All)
        {
            disableUIEvents = true;

            if (changeType == DataChangeType.All)
            {
                TitleText.Text = data.title;

                UpdateTitle();
            }

            if (changeType == DataChangeType.All || changeType == DataChangeType.ExtensionSettingsModified)
            {
                EditableExtensionsText.Text = String.Join(Constants.extensionSeparator, data.editableExtensions);
                SanitizeExtensions(EditableExtensionsText);
            }

            ConsoleEnabledCheckbox.IsChecked = data.commandConsoleEnabled;

            if (changeType == DataChangeType.All || changeType == DataChangeType.QuestionsEnabled)
            {
                QuestionsEnabledCheckbox.IsChecked = data.instructor.enabled;

                foreach (Control c in questionControls)
                { c.Visibility = data.instructor.enabled ? Visibility.Visible : Visibility.Hidden; }

                if (!data.instructor.enabled) { Tabs.SelectedIndex = 0; }
            }

            if (data.instructor.enabled)
            {
                if (changeType == DataChangeType.All || changeType == DataChangeType.QuestionsEnabled)
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
                }

                if (changeType == DataChangeType.All || changeType == DataChangeType.TemplateListModified || changeType == DataChangeType.QuestionsEnabled)
                {
                    TemplatesList.Items.Clear();

                    int index = 0;
                    foreach (RecordData.QuestionTemplateData template in data.instructor.ask.templates)
                    {
                        string label = Constants.questionTypeNames[(int)template.type];

                        string title = template.question.Trim().Length > 0 ? template.question : Constants.templateDefaultTitle;

                        TemplatesList.Items.Add((index + 1) + ": [" + label + "] " + title);
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
                }

                if (changeType == DataChangeType.All || changeType == DataChangeType.QuestionChances || changeType == DataChangeType.QuestionsEnabled)
                {
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

                    if (changeType == DataChangeType.All || changeType == DataChangeType.QuestionsEnabled || changeType == DataChangeType.ExtensionSettingsModified)
                    {

                        WordSeparatorsExtensionList.Items.Clear();

                        int extIndex = 0;
                        foreach (RecordData.InstructorObserveWordSeparators wordSeparators in data.instructor.observe.wordSeparators)
                        {
                            WordSeparatorsExtensionList.Items.Add((extIndex + 1) + ": " + wordSeparators.extension);
                            extIndex++;
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
                            SanitizeWordSeparators(WordSeparatorsText);
                        }

                        WordSeparatorsExtensionList.SelectedIndex = selectedExtension;
                    }
                }
            }


            disableUIEvents = false;
        }

        private void OnConsoleChecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.commandConsoleEnabled = true;

            needsSave = true;

        }

        private void OnConsoleUnchecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.commandConsoleEnabled = false;

            needsSave = true;

        }

        private void OnQuestionsChecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.instructor.enabled = true;
            UpdateView(DataChangeType.QuestionsEnabled);

            needsSave = true;
            UpdateTitle();

        }

        private void OnQuestionsUnchecked(object sender, RoutedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.instructor.enabled = false;
            UpdateView(DataChangeType.QuestionsEnabled);

            needsSave = true;
            UpdateTitle();
        }


        private void OnSelectedTemplateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            selectedTemplate = TemplatesList.SelectedIndex;
            UpdateView();

        }

        private void OnSelectedExtensionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            selectedExtension = WordSeparatorsExtensionList.SelectedIndex;

            UpdateView();

        }

        private void OnTitleChanged(object sender, TextChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            data.title = TitleText.Text;

            needsSave = true;
            UpdateTitle();
        }

        private void OnQuestionParamChanged(object sender, TextChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            SanitizeInt(QuestionsTimeMinText);
            SanitizeInt(QuestionsTimeMaxText);
            SanitizeInt(QuestionsTextMinText);
            SanitizeInt(QuestionsTextMaxText);
            SanitizeInt(QuestionsSavesMinText);
            SanitizeInt(QuestionsSavesMaxText);
            SanitizeInt(QuestionsOpensMinText);
            SanitizeInt(QuestionsOpensMaxText);
            SanitizeInt(QuestionsCommandsMinText);
            SanitizeInt(QuestionsCommandsMaxText);
            SanitizeInt(QuestionsObserveMinText);
            SanitizeInt(QuestionsObserveMaxText);


            data.instructor.wait.time.min = Int32.Parse(QuestionsTimeMinText.Text);
            data.instructor.wait.time.max = Int32.Parse(QuestionsTimeMaxText.Text);
            data.instructor.wait.characters.min = Int32.Parse(QuestionsTextMinText.Text);
            data.instructor.wait.characters.max = Int32.Parse(QuestionsTextMaxText.Text);
            data.instructor.wait.fileSaves.min = Int32.Parse(QuestionsSavesMinText.Text);
            data.instructor.wait.fileSaves.max = Int32.Parse(QuestionsSavesMaxText.Text);
            data.instructor.wait.fileOpens.min = Int32.Parse(QuestionsOpensMinText.Text);
            data.instructor.wait.fileOpens.max = Int32.Parse(QuestionsOpensMaxText.Text);
            data.instructor.wait.commands.min = Int32.Parse(QuestionsCommandsMinText.Text);
            data.instructor.wait.commands.max = Int32.Parse(QuestionsCommandsMaxText.Text);
            data.instructor.observe.time.min = Int32.Parse(QuestionsObserveMinText.Text);
            data.instructor.observe.time.max = Int32.Parse(QuestionsObserveMaxText.Text);

            needsSave = true;
            UpdateTitle();

        }

        private void OnTemplateParamChanged(object sender, TextChangedEventArgs e)
        {
            Debug.Assert(selectedTemplate >= 0 && selectedTemplate < data.instructor.ask.templates.Length, "Selected template out of bounds");

            if (disableUIEvents) { return; }

            OnTemplateParamChanged();

            needsSave = true;
            UpdateTitle();

        }

        private void OnTemplateParamChanged(object sender, SelectionChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            OnTemplateParamChanged();

            needsSave = true;
            UpdateTitle();
        }

        private void OnTemplateParamChanged()
        {
            Debug.Assert(selectedTemplate >= 0 && selectedTemplate < data.instructor.ask.templates.Length, "Selected template out of bounds");

            SanitizeInt(QuestionTimeText);

            data.instructor.ask.templates[selectedTemplate].type = (RecordData.QuestionType)QuestionTypeList.SelectedIndex;
            data.instructor.ask.templates[selectedTemplate].question = QuestionTextText.Text;
            data.instructor.ask.templates[selectedTemplate].timeLimit = Int32.Parse(QuestionTimeText.Text);

            RecordData.QuestionType questionType = (RecordData.QuestionType)QuestionTypeList.SelectedIndex;

            if (questionType == RecordData.QuestionType.input)
            {
                foreach (Control c in templateTypeTextControls) { c.Visibility = Visibility.Visible; }

                SanitizeInt(QuestionLinesText);
                SanitizeInt(QuestionWordsText);

                data.instructor.ask.templates[selectedTemplate].contextLines = Int32.Parse(QuestionLinesText.Text);
                data.instructor.ask.templates[selectedTemplate].contextWords = Int32.Parse(QuestionWordsText.Text);
            }
            else
            {
                foreach (Control c in templateTypeTextControls) { c.Visibility = Visibility.Hidden; }
            }

            needsSave = true;
            UpdateTitle();

        }

        private void OnProbabilityParamsChanged(object sender, TextChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            SanitizeInt(ProbabilityTextText);
            SanitizeInt(ProbabilityGeneralText);
            SanitizeInt(ProbabilitySavesText);
            SanitizeInt(ProbabilityOpensText);
            SanitizeInt(ProbabilityCommandsText);

            data.instructor.ask.chances = new RecordData.QuestionChanceData[]
            {
                new() { type = RecordData.QuestionType.input, weight = Int32.Parse(ProbabilityTextText.Text) },
                new() { type = RecordData.QuestionType.time, weight = Int32.Parse(ProbabilityGeneralText.Text) },
                new() { type = RecordData.QuestionType.fileSave, weight = Int32.Parse(ProbabilitySavesText.Text) },
                new() { type = RecordData.QuestionType.fileOpen, weight = Int32.Parse(ProbabilityOpensText.Text) },
                new() { type = RecordData.QuestionType.command, weight = Int32.Parse(ProbabilityCommandsText.Text) }

            };

            needsSave = true;
            UpdateTitle();

        }

        private void OnSeparatorParamsChanged(object sender, TextChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            Debug.Assert(selectedExtension >= 0 && selectedExtension < data.instructor.observe.wordSeparators.Length, "Selected template out of bounds");

            SanitizeWordSeparators(WordSeparatorsText);

            data.instructor.observe.wordSeparators[selectedExtension].separators = WordSeparatorsText.Text.ToCharArray();

            needsSave = true;
            UpdateTitle();

        }

        private void OnCreateTemplate(object sender, RoutedEventArgs e)
        {
            List<RecordData.QuestionTemplateData> templates = data.instructor.ask.templates.ToList();
            templates.Add(new RecordData.QuestionTemplateData());
            data.instructor.ask.templates = templates.ToArray();

            selectedTemplate = data.instructor.ask.templates.Length - 1;

            UpdateView(DataChangeType.TemplateListModified);

            needsSave = true;
            UpdateTitle();
        }

        private void OnDeleteTemplate(object sender, RoutedEventArgs e)
        {
            if (selectedTemplate >= 0)
            {
                List<RecordData.QuestionTemplateData> templates = data.instructor.ask.templates.ToList();
                templates.RemoveAt(selectedTemplate);
                data.instructor.ask.templates = templates.ToArray();

                selectedTemplate = selectedTemplate - 1;

                UpdateView(DataChangeType.TemplateListModified);
            }

            needsSave = true;
            UpdateTitle();
        }

        private void OnEditableExtensionsChanged(object sender, TextChangedEventArgs e)
        {
            if (disableUIEvents) { return; }

            SanitizeExtensions(EditableExtensionsText);

            data.editableExtensions = EditableExtensionsText.Text.Split(Constants.extensionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Remove orphan extension separators
            List<string> editableExtensions = data.editableExtensions.ToList();
            List<RecordData.InstructorObserveWordSeparators> newExtensionSeparators = new();
            List<RecordData.InstructorObserveWordSeparators> extensionSeparators = data.instructor.observe.wordSeparators.ToList();
            List<string> removedExtensions = new();
            foreach (RecordData.InstructorObserveWordSeparators extensionSeparator in extensionSeparators)
            {
                if (editableExtensions.Contains(extensionSeparator.extension)) { newExtensionSeparators.Add(extensionSeparator); }
                else { removedExtensions.Add(extensionSeparator.extension);  }
            }

            // Create new extension separators

            List<string> addedExtensions = new();
            foreach (string extension in editableExtensions)
            {
                if (!newExtensionSeparators.Exists(e => e.extension == extension))
                {
                    RecordData.InstructorObserveWordSeparators extensionSeparator = new();
                    extensionSeparator.extension = extension;
                    newExtensionSeparators.Add(extensionSeparator);
                    addedExtensions.Add(extension);
                }
            }

            data.instructor.observe.wordSeparators = newExtensionSeparators.ToArray();

            //if (removedExtensions.Count > 0)
            //{
            //    MessageBox.Show("Se han eliminado los separadores para " + (removedExtensions.Count == 1 ? "la extensión " : "las extensiones ") +
            //        String.Join(Constants.extensionSeparator, removedExtensions), "Advertencia", MessageBoxButton.OK, MessageBoxImage.Information);
            //}

            //if (addedExtensions.Count > 0)
            //{
            //    MessageBox.Show("Se han añadido separadores por defecto para " + (addedExtensions.Count == 1 ? "la extensión " : "las extensiones ") +
            //        String.Join(Constants.extensionSeparator, addedExtensions), "Advertencia", MessageBoxButton.OK, MessageBoxImage.Information);
            //}

            if (removedExtensions.Count > 0 || addedExtensions.Count > 0)
            {
                UpdateView(DataChangeType.ExtensionSettingsModified);
            }

            needsSave = true;
            UpdateTitle();

        }

        void SanitizeInt(TextBox textBox)
        {
            string text = textBox.Text.Trim();

            if (text.Length == 0) { textBox.Text = "0"; }
            if (!text.All(c => Constants.numbers.Contains(c))) { textBox.Text = "0"; }

            int number;
            if (!Int32.TryParse(text, out number))
            {
                textBox.Text = "0";
            }
        }

        void SanitizeWordSeparators(TextBox textBox)
        {
            int previousCaretIndex = textBox.CaretIndex;

            List<char> newChars = new();
            List<char> previousChars = new(textBox.Text);
            int removed = 0;

            foreach (char c in previousChars)
            {
                if (!newChars.Contains(c)) { newChars.Add(c); }
                else { removed ++;  }
            }

            textBox.Text = new string(newChars.ToArray());

            if (previousCaretIndex - removed >= 0 && previousCaretIndex - removed <= newChars.Count)
            {
                textBox.CaretIndex = previousCaretIndex - removed;
            }
        }

        void SanitizeExtensions(TextBox textBox)
        {
            int previousCaretIndex = textBox.CaretIndex;

            List<string> newExtensions = new();
            List<string> previousExtensions = textBox.Text.Split(Constants.extensionSeparator).ToList();
            int removed = 0;

            foreach (string extension in previousExtensions)
            {
                if (!newExtensions.Contains(extension.ToLower())) { newExtensions.Add(extension.ToLower()); }
                else { removed += extension.Length; }
            }

            textBox.Text = new string(String.Join(Constants.extensionSeparator, newExtensions.ToArray()));

            if (previousCaretIndex - removed >= 0 && previousCaretIndex - removed <= textBox.Text.Length)
            {
                textBox.CaretIndex = previousCaretIndex - removed;
            }
        }

        private void OnQuestionWaitProfileLoad(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "question frequency files (*." + Constants.questionFrequencyProfileExtension + ")|*." + Constants.questionFrequencyProfileExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                Profiles.QuestionFrequency? profile = Profiles.LoadQuestionFrequency(path);

                if (profile != null)
                {
                    disableUIEvents = true;

                    QuestionsTimeMinText.Text = profile.WaitTime.Min.ToString();
                    QuestionsTimeMaxText.Text = profile.WaitTime.Max.ToString();
                    QuestionsTextMinText.Text = profile.WaitCharacters.Min.ToString();
                    QuestionsTextMaxText.Text = profile.WaitCharacters.Max.ToString();
                    QuestionsSavesMinText.Text = profile.WaitFileSaves.Min.ToString();
                    QuestionsSavesMaxText.Text = profile.WaitFileSaves.Max.ToString();
                    QuestionsOpensMinText.Text = profile.WaitFileOpens.Min.ToString();
                    QuestionsOpensMaxText.Text = profile.WaitFileOpens.Max.ToString();
                    QuestionsCommandsMinText.Text = profile.WaitCommands.Min.ToString();
                    QuestionsCommandsMaxText.Text = profile.WaitCommands.Max.ToString();
                    QuestionsObserveMinText.Text = profile.ObserveTime.Min.ToString();
                    QuestionsObserveMaxText.Text = profile.ObserveTime.Max.ToString();

                    data.instructor.wait.time = profile.WaitTime.ToData();
                    data.instructor.wait.characters = profile.WaitCharacters.ToData();
                    data.instructor.wait.fileSaves = profile.WaitFileSaves.ToData();
                    data.instructor.wait.fileOpens = profile.WaitFileOpens.ToData();
                    data.instructor.wait.commands = profile.WaitCommands.ToData();
                    data.instructor.observe.time = profile.ObserveTime.ToData();

                    disableUIEvents = false;

                    needsSave = true;

                }
                else
                {
                    MessageBox.Show("No se han podido cargar las esperas del fichero", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
        }

        private void OnQuestionWaitProfileSave(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new();

            dialog.Filter = "question frequency files (*." + Constants.questionFrequencyProfileExtension + ")|*." + Constants.questionFrequencyProfileExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                Profiles.QuestionFrequency profile = new();

                profile.WaitTime = new(data.instructor.wait.time);
                profile.WaitCharacters = new(data.instructor.wait.characters);
                profile.WaitFileSaves = new(data.instructor.wait.fileSaves);
                profile.WaitFileOpens = new(data.instructor.wait.fileOpens);
                profile.WaitCommands = new(data.instructor.wait.commands);
                profile.ObserveTime = new(data.instructor.observe.time);

                Profiles.SaveQuestionFrequency(profile, path);
            }
        }

        private void OnQuestionTemplatesProfileLoad(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "question templates files (*." + Constants.questionTemplatesProfileExtension+ ")|*." + Constants.questionTemplatesProfileExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                Profiles.QuestionTemplates? profile = Profiles.LoadQuestionTemplates(path);

                if (profile != null)
                {
                    data.instructor.ask.templates = profile.Templates;
                    UpdateView(DataChangeType.TemplateListModified);
                    needsSave = true;

                }
                else
                {
                    MessageBox.Show("No se han podido cargar las plantillas del fichero", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }

        }

        private void OnQuestionTemplatesProfileSave(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new();

            dialog.Filter = "question templates files (*." + Constants.questionTemplatesProfileExtension + ")|*." + Constants.questionTemplatesProfileExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;
                Profiles.QuestionTemplates profile = new();
                profile.Templates = data.instructor.ask.templates;
                Profiles.SaveQuestionTemplates(profile, path);
            }

        }

        private void OnQuestionChancesProfileSave(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new();

            dialog.Filter = "question chances files (*." + Constants.questionChancesProfileExtension + ")|*." + Constants.questionChancesProfileExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;
                Profiles.QuestionChances profile = new();
                profile.Chances = data.instructor.ask.chances;
                Profiles.SaveQuestionChances(profile, path);
            }
        }

        private void OnQuestionChancesProfileLoad(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "question chances files (*." + Constants.questionChancesProfileExtension + ")|*." + Constants.questionChancesProfileExtension;
            dialog.FilterIndex = 1;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                string path = dialog.FileName;

                Profiles.QuestionChances? profile = Profiles.LoadQuestionChances(path);

                if (profile != null)
                {
                    data.instructor.ask.chances = profile.Chances;
                    UpdateView(DataChangeType.QuestionChances);
                    needsSave = true;

                }
                else
                {
                    MessageBox.Show("No se han podido cargar las probabilidades del fichero", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }

        }

        private void OnExtensionSettingsLoad(object sender, RoutedEventArgs e)
        {
            bool isSeparators = (sender == WordSeparatorsProfileLoad);

            string infoText = isSeparators ? "Esto también reemplazará las extensiones de archivo editables" : "Esto también reemplazará los separadores de palabras de las extensiones";

            MessageBoxResult confirmResult = MessageBox.Show(infoText, "Atención", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (confirmResult == MessageBoxResult.OK)
            {
                OpenFileDialog dialog = new();

                dialog.Filter = "extension settings files (*." + Constants.extensionSettingsExtension + ")|*." + Constants.extensionSettingsExtension;
                dialog.FilterIndex = 1;

                bool? result = dialog.ShowDialog();

                if (result.GetValueOrDefault())
                {
                    string path = dialog.FileName;

                    Profiles.ExtensionSettings? profile = Profiles.LoadExtensionSettings(path);

                    if (profile != null)
                    {
                        data.editableExtensions = profile.EditableExtensions;
                        data.instructor.observe.wordSeparators = profile.WordSeparators;
                        UpdateView(DataChangeType.ExtensionSettingsModified);
                        needsSave = true;

                    }
                    else
                    {
                        MessageBox.Show("No se han podido cargar la configuración de extensiones del fichero", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                }
            }

        }

        private void OnExtensionSettingsSave(object sender, RoutedEventArgs e)
        {
            bool isSeparators = (sender == WordSeparatorsProfileSave);

            string infoText = isSeparators ? "Esto también guardará las extensiones de archivo editables" : "Esto también guardará los separadores de palabras de las extensiones";

            MessageBoxResult confirmResult = MessageBox.Show(infoText, "Atención", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (confirmResult == MessageBoxResult.OK)
            {
                SaveFileDialog dialog = new();

                dialog.Filter = "extension settings files (*." + Constants.extensionSettingsExtension + ")|*." + Constants.extensionSettingsExtension;
                dialog.FilterIndex = 1;

                bool? result = dialog.ShowDialog();

                if (result.GetValueOrDefault())
                {
                    string path = dialog.FileName;
                    Profiles.ExtensionSettings profile = new();

                    profile.EditableExtensions = data.editableExtensions;
                    profile.WordSeparators = data.instructor.observe.wordSeparators;

                    Profiles.SaveExtensionSettings(profile, path);
                }
            }

        }
    }
}