using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TrueStory
{
    public class Instructor
    {
        public struct Question
        {
            public string text;
            public Context context;
            public string answer;
            public long usedTime;
            public long timeLimit;
        }

        public struct Context
        {
            public string fileName;
            public ContextLine[] lines;
            public int currentLine;
        }

        public struct ContextLine
        {
            public int number;
            public string text;
        }

        enum TriggerType
        {
            fileChanges,
            fileSave,
            fileOpen,
            command,
            time

        }

        struct QuestionTemplate
        {
            public TriggerType trigger;
            public float frequency;
            public string question;
            public int contextLineCount;
            public int contextLineSize;
            public int timeLimit;
        }

        QuestionTemplate[] questionTemplates = 
        {
            new() { trigger = TriggerType.fileChanges, question = "¿Para qué sirve '[lineWord]' en la línea [lineIndex]?", contextLineCount = 10, contextLineSize = 10, timeLimit = 20 },
            new() { trigger = TriggerType.fileChanges, question = "¿Qué es la palabra '[lineWord]' de la línea [lineIndex]?", contextLineCount = 10, contextLineSize = 10, timeLimit = 20 },
            new() { trigger = TriggerType.fileChanges, question = "¿Para qué sirve la línea [lineIndex]?", contextLineCount = 10, contextLineSize = 10, timeLimit = 20 },
            new() { trigger = TriggerType.fileChanges, question = "¿Qué estás cambiando en la línea [lineIndex]?", contextLineCount = 10, contextLineSize = 10, timeLimit = 20 },
            new() { trigger = TriggerType.fileSave, question = "¿Qué has cambiado en el fichero [fileName] que has guardado recientemente?", contextLineCount = -1, timeLimit = 15  },
            new() { trigger = TriggerType.fileOpen, question = "Has abierto hace poco el fichero [fileName] ¿Por qué?", contextLineCount = -1, timeLimit = 15  },
            new() { trigger = TriggerType.command, question = "¿Por qué has ejecutado el comando [commandName] hace un momento?", contextLineCount = 0, timeLimit = 10  },
            new() { trigger = TriggerType.command, question = "¿Qué significa la línea [lineIndex] en el resultado del comando [commandName]?", contextLineCount = 0, timeLimit = 10  },
            new() { trigger = TriggerType.time, question = "¿Para qué sirve el fichero [fileName] que tienes abierto?", contextLineCount = 0, timeLimit = 10  },
            new() { trigger = TriggerType.time, question = "¿Qué estás haciendo ahora mismo en el proyecto?", contextLineCount = 0, timeLimit = 10  },
            new() { trigger = TriggerType.time, question = "¿Qué has hecho antes que esto?", contextLineCount = 0, timeLimit = 10  },
            new() { trigger = TriggerType.time, question = "¿Cuál es el último problema que has tenido?", contextLineCount = 0, timeLimit = 20  },
            new() { trigger = TriggerType.time, question = "¿Cuál es el problema más importante que has tenido hasta ahora?", contextLineCount = 0, timeLimit = 20  },
            new() { trigger = TriggerType.time, question = "¿Qué te preocupa ahora mismo que crees que no sabrás resolver?", contextLineCount = 0, timeLimit = 20  },
            new() { trigger = TriggerType.time, question = "Explica algo que estés particularmente feliz de haber solucionado", contextLineCount = 0, timeLimit = 20  },
        };

        enum State
        {
            idle,
            fetching,
            warning
        };

        State state;

        Question? questionChanges;

        long questionIdleWait;
        long questionWarningWait;

        Random random;

        MainWindow window;
        FileEditor fileEditor;

        DispatcherTimer timer;

        TextBlock instructorTextBox;
        

        public Instructor(MainWindow _window)
        {
            _window.FileEditor.OnContentsChanged += OnFileEditorContentsChanged;

            fileEditor = _window.FileEditor;
            instructorTextBox = _window.InstructorTextBox;

            window = _window;

            random = new Random((int)(DateTimeOffset.Now.ToUnixTimeSeconds() % Int32.MaxValue));

            questionIdleWait = random.NextInt64(Constants.minQuestionWait, Constants.maxQuestionWait);
            instructorTextBox.Text = "";
            instructorTextBox.Background = new SolidColorBrush(Colors.Transparent);

            state = State.idle;


            timer = new();
            timer.Tick += OnTimerTick;
            timer.Interval = new TimeSpan(0, 0, 1);

            if (!Switches.NoQuestions)
            {
                timer.Start();
            }

        }

        void TryFetchChangesQuestion()
        {
            List<QuestionTemplate> templates = new(questionTemplates);
            templates.RemoveAll((q) => q.trigger != TriggerType.fileChanges);

            if (templates.Count > 0)
            {
                int index = random.Next(templates.Count);

                QuestionTemplate template = templates[index];

                Console.WriteLine("Trying to apply template " + index);

                if (template.contextLineCount > 0)
                {
                    int currentLine = fileEditor.TextBox.GetLineIndexFromCharacterIndex(fileEditor.TextBox.CaretIndex);
                    int numLines = fileEditor.TextBox.LineCount;
                    int contextStart = currentLine - template.contextLineCount / 2;
                    int contextEnd = currentLine + template.contextLineCount / 2;

                    if (contextStart < 0) { contextEnd += -contextStart; contextStart = 0; }
                    else if (contextEnd >= numLines) { contextStart += -(contextEnd - (numLines - 1)); contextEnd = numLines - 1; }

                    string lineText = fileEditor.TextBox.GetLineText(currentLine).Replace("\r\n", "");

                    int lineSize = lineText.Count((c) => !(Constants.wordSeparators.Contains(c) || Constants.tabEquivalents.Contains(c)));

                    if (contextStart >= 0 && contextEnd < numLines &&
                        (contextEnd - contextStart + 1) > template.contextLineCount &&
                        lineSize >= template.contextLineSize)
                    {
                        Context context = new();

                        string? fileName = window.LoadedFile;

                        Debug.Assert(fileName != null);

                        context.fileName = fileName;
                        context.currentLine = currentLine;

                        List<ContextLine> lines = new();
                        for (int i = contextStart; i <= contextEnd; i++)
                        {
                            ContextLine line = new();
                            line.number = i;
                            line.text = fileEditor.TextBox.GetLineText(i).Replace("\r\n", "");
                            lines.Add(line);
                        }

                        context.lines = lines.ToArray();

                        Question q = new Question();


                        List<char> separators = new();
                        separators.AddRange(Constants.tabEquivalents);
                        separators.AddRange(Constants.wordSeparators);

                        string[] words = lineText.Split(separators.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        string anyWord = words[random.Next(words.Length)];

                        string text = template.question;
                        text = text.Replace("[fileName]", context.fileName);
                        text = text.Replace("[lineIndex]", (context.currentLine + 1).ToString());
                        text = text.Replace("[lineWord]", anyWord);

                        q.text = text;
                        q.context = context;
                        q.answer = "";
                        q.usedTime = 0;
                        q.timeLimit = template.timeLimit;

                        questionChanges = q;
                    }
                    else
                    {
                        if (contextStart < 0 || contextEnd > numLines || (contextEnd - contextStart + 1) < template.contextLineCount)
                        {
                            Console.WriteLine("Not enough context size");
                        }
                        else
                        {
                            Console.WriteLine("Not enough words size");
                        }

                    }


                }

            }
        }

        void OnTimerTick(object? sender, EventArgs e)
        {
            if (Switches.NoQuestions) { return; }


            if (state == State.idle)
            {
                questionIdleWait -= 1;

                if (questionIdleWait <= 0)
                {
                    state = State.fetching;
                }
            }
            else if (state == State.fetching)
            {
                if (questionChanges.HasValue)
                {
                    questionWarningWait = Constants.questionWarningTime;
                    instructorTextBox.Text = "Pregunta en " + questionWarningWait;
                    instructorTextBox.Background = new SolidColorBrush(Colors.Yellow);

                    state = State.warning;
                }
            }
            else if (state == State.warning)
            {
                questionWarningWait -= 1;

                if (questionWarningWait > 0)
                {
                    instructorTextBox.Text = "Pregunta en " + questionWarningWait;
                    instructorTextBox.Background = new SolidColorBrush(Colors.Yellow);
                }
                else
                {
                    instructorTextBox.Text = "";
                    instructorTextBox.Background = new SolidColorBrush(Colors.Transparent);

                    Debug.Assert(questionChanges.HasValue);

                    DialogQuestion d = new();
                    d.Question = questionChanges.Value;
                    d.Show();

                    questionChanges = null;
                    questionIdleWait = random.NextInt64(Constants.minQuestionWait, Constants.maxQuestionWait);

                    state = State.idle;

                }
            }

        }

        void OnFileEditorContentsChanged(FileEditor.ContentChange c)
        {
            if (Switches.NoQuestions) { return;  }

            if (questionWarningWait >= 0)
            {
                TryFetchChangesQuestion();
            }

        }
    }
}