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
using static TrueStory.Instructor;

namespace TrueStory
{
    public class Instructor
    {
        public struct Question
        {
            public QuestionType type;
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
            public string command;
        }

        public struct ContextLine
        {
            public int number;
            public string text;
        }

        public enum QuestionType
        {
            input,
            time,
            fileSave,
            fileOpen,
            command,

        }

        struct QuestionTemplate
        {
            public QuestionType type;
            public float frequency;
            public string question;
            public int contextLines;
            public int contextWords;
            public int timeLimit;
        }

        QuestionTemplate[] questionTemplates = 
        {
            new() { type = QuestionType.input, question = "¿Para qué sirve '[lineWord]' en la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = QuestionType.input, question = "¿Qué es la palabra '[lineWord]' de la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = QuestionType.input, question = "¿Para qué sirve la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = QuestionType.input, question = "¿Qué cambiaste en la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = QuestionType.fileSave, question = "¿Qué has cambiado en el fichero [fileName] que has guardado recientemente?", contextLines = -1, timeLimit = 15  },
            new() { type = QuestionType.fileOpen, question = "¿Por qué has abierto recientemente el fichero [fileName]?", contextLines = -1, timeLimit = 15  },
            new() { type = QuestionType.fileOpen, question = "¿Para qué sirve el fichero [fileName] que has abierto recientemente?", contextLines = 0, timeLimit = 10  },
            new() { type = QuestionType.command, question = "¿Por qué ejecutaste este comando recientemente?", contextLines = 0, timeLimit = 10  },
            new() { type = QuestionType.command, question = "¿Tuviste algún problema cuando ejecutaste el comando '[commandName]'?", contextLines = 0, timeLimit = 10  },
            new() { type = QuestionType.time, question = "¿Qué estás haciendo ahora mismo en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = QuestionType.time, question = "¿Qué has hecho antes que lo que estás haciendo ahora en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = QuestionType.time, question = "¿Cuál es el último problema que has tenido en esta actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = QuestionType.time, question = "¿Cuál es el problema más importante que has tenido hasta ahora en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = QuestionType.time, question = "¿Qué te preocupa ahora mismo que crees que no sabrás resolver en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = QuestionType.time, question = "Explica algo que estés particularmente feliz de haber solucionado en la actividad", contextLines = 0, timeLimit = 20  },
        };

        enum State
        {
            idle,
            fetching,
            warning
        };

        State state;

        List<QuestionTemplate> templateCandidatesTime;
        List<QuestionTemplate> templateCandidatesInput;
        List<QuestionTemplate> templateCandidatesCommand;
        List<QuestionTemplate> templateCandidatesOpen;
        List<QuestionTemplate> templateCandidatesSave;

        List<Question> questionCandidates;
        List<Question> questionShortList;
        List<QuestionType> questionShortListTypes;
        Question? question;

        char[] ignoreCharacters;


        long questionCharactersWait;
        long questionCommandsWait;
        long questionOpensWait;
        long questionSavesWait;


        Random random;

        MainWindow window;
        FileEditor fileEditor;

        DispatcherTimer timer;

        TextBlock instructorTextBox;

        long idleWait;
        long fetchWait;

        long idleTimer;
        long fetchTimer;
        long warningTimer;

        long inactiveTimer;

        long inputCharactersCounter;
        long opensCounter;
        long savesCounter;
        long commandsCounter;
        

        public Instructor(MainWindow _window)
        {
            _window.FileEditor.OnContentsChanged += OnFileEditorContentsChanged;
            _window.CommandConsole.OnCommandExecuted += OnCommandConsoleExecutedCommand;

            fileEditor = _window.FileEditor;
            instructorTextBox = _window.InstructorTextBox;

            window = _window;

            random = new Random((int)(DateTimeOffset.Now.ToUnixTimeSeconds() % Int32.MaxValue));

            instructorTextBox.Text = "";
            instructorTextBox.Background = new SolidColorBrush(Colors.Transparent);

            questionCharactersWait = random.NextInt64(Constants.minQuestionCharactersWait, Constants.maxQuestionCharactersWait + 1);
            questionOpensWait = random.NextInt64(Constants.minQuestionOpensWait, Constants.maxQuestionOpensWait + 1);
            questionSavesWait = random.NextInt64(Constants.minQuestionSavesWait, Constants.maxQuestionSavesWait + 1);
            questionCommandsWait = random.NextInt64(Constants.minQuestionCommandsWait, Constants.maxQuestionCommandsWait + 1);

            idleWait = random.NextInt64(Constants.minQuestionsTimeWait, Constants.maxQuestionsTimeWait + 1);

            questionCandidates = new();
            questionShortList = new();
            questionShortListTypes = new();

            idleTimer = 0;
            fetchTimer = 0;

            inputCharactersCounter = 0;
            opensCounter = 0;
            savesCounter = 0;
            commandsCounter = 0;

            inactiveTimer = 0;

            state = State.idle;

            List<char> separators = new();
            separators.AddRange(Constants.tabEquivalents);
            separators.AddRange(Constants.wordSeparators);
            ignoreCharacters = separators.ToArray();

            List<QuestionTemplate> templatesList = new(questionTemplates);
            templateCandidatesTime = templatesList.FindAll((q) => q.type == QuestionType.time);
            templateCandidatesInput = templatesList.FindAll((q) => q.type == QuestionType.input);
            templateCandidatesCommand = templatesList.FindAll((q) => q.type == QuestionType.command);
            templateCandidatesOpen = templatesList.FindAll((q) => q.type == QuestionType.fileOpen);
            templateCandidatesSave = templatesList.FindAll((q) => q.type == QuestionType.fileSave);


            timer = new();
            timer.Tick += OnTimerTick;
            timer.Interval = new TimeSpan(0, 0, 1);


            if (!Switches.NoQuestions)
            {
                timer.Start();
            }

        }

        void OnTimerTick(object? sender, EventArgs e)
        {
            if (Switches.NoQuestions) { return; }

            if (!fileEditor.IsFileLoaded) { return;  }

            Console.WriteLine("Instructor: " + state + ": IDLE " + idleTimer + "/" + idleWait +
                                                       ", FETC" + fetchTimer + "/" + fetchWait +
                                                       ", WARN " + warningTimer + "/" + Constants.questionWarningTime +
                                                       " IC " + inputCharactersCounter + "/" + questionCharactersWait + 
                                                       " CC " + commandsCounter + "/" + questionCommandsWait);

            inactiveTimer++;

            if (CanFetchQuestion(QuestionType.time))
            {
                TryFetchTimeQuestion();
            }

            if (state == State.idle)
            {
                if (inactiveTimer <= Constants.inactiveWait)
                {
                    idleTimer++;
                }

                if (idleTimer > idleWait)
                {
                    fetchWait = random.NextInt64(Constants.minQuestionsFetchWait, Constants.maxQuestionsFetchWait + 1);
                    fetchTimer = 0;

                    questionCandidates.Clear();
                    
                    instructorTextBox.Text = "Observando";
                    instructorTextBox.Background = new SolidColorBrush(Colors.Orange);

                    state = State.fetching;
                }

            }
            else if (state == State.fetching)
            {
                if (inactiveTimer <= Constants.inactiveWait)
                {
                    fetchTimer++;
                }

                if (fetchTimer > fetchWait)
                {
                    if(questionCandidates.Count > 0)
                    {
                        Console.WriteLine("Choosing question between " + questionCandidates.Count + " candidates");

                        Predicate<Question> filter = (Question q) => q.type == QuestionType.time;
                        Question? timeQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Question q) => q.type == QuestionType.input;
                        Question? inputQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Question q) => q.type == QuestionType.command;
                        Question? commandsQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Question q) => q.type == QuestionType.fileOpen;
                        Question? opensQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Question q) => q.type == QuestionType.fileSave;
                        Question? savesQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        questionShortList.Clear();

                        if (timeQuestion.HasValue) { questionShortList.Add(timeQuestion.Value); }
                        if (inputQuestion.HasValue) { questionShortList.Add(inputQuestion.Value); }
                        if (commandsQuestion.HasValue) { questionShortList.Add(commandsQuestion.Value); }
                        if (opensQuestion.HasValue) { questionShortList.Add(opensQuestion.Value); }
                        if (savesQuestion.HasValue) { questionShortList.Add(savesQuestion.Value); }

                        Console.WriteLine("Shortlist:");
                        foreach (Question q in questionShortList)
                        {
                            Console.WriteLine(q.type + ": " + q.text);
                        }

                        questionShortListTypes.Clear();

                        if (questionShortList.FindIndex(q => q.type == QuestionType.time) >= 0) { questionShortListTypes.Add(QuestionType.time); }
                        if (questionShortList.FindIndex(q => q.type == QuestionType.input) >= 0) { questionShortListTypes.Add(QuestionType.input); }
                        if (questionShortList.FindIndex(q => q.type == QuestionType.command) >= 0) { questionShortListTypes.Add(QuestionType.command); }
                        if (questionShortList.FindIndex(q => q.type == QuestionType.fileOpen) >= 0) { questionShortListTypes.Add(QuestionType.fileOpen); }
                        if (questionShortList.FindIndex(q => q.type == QuestionType.fileSave) >= 0) { questionShortListTypes.Add(QuestionType.fileSave); }

                        int totalWeight = 0;

                        bool hasTime = questionShortListTypes.Contains(QuestionType.time);
                        bool hasInput = questionShortListTypes.Contains(QuestionType.input);
                        bool hasCommand = questionShortListTypes.Contains(QuestionType.command);
                        bool hasOpen = questionShortListTypes.Contains(QuestionType.fileOpen);
                        bool hasSave = questionShortListTypes.Contains(QuestionType.fileSave);

                        if (hasTime) { totalWeight += Constants.questionsTimeWeight; }
                        if (hasInput) { totalWeight += Constants.questionsInputWeight; }
                        if (hasCommand) { totalWeight += Constants.questionsCommandsWeight; }
                        if (hasOpen) { totalWeight += Constants.questionsOpensWeight; }
                        if (hasSave) { totalWeight += Constants.questionsSavesWeight; }

                        int randomWeight = random.Next(0, totalWeight);

                        Predicate<Question> criteria;

                        if (hasTime)
                        {
                            if (randomWeight < Constants.questionsTimeWeight)
                            {
                                criteria = (q => q.type == QuestionType.time);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsTimeWeight;

                        }


                        if (question == null && hasInput)
                        {
                            if (randomWeight < Constants.questionsInputWeight)
                            {
                                criteria = (q => q.type == QuestionType.input);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsInputWeight;
                        }


                        if (question == null && hasCommand)
                        {
                            if (randomWeight < Constants.questionsCommandsWeight)
                            {
                                criteria = (q => q.type == QuestionType.command);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsCommandsWeight;
                        }

                        if (question == null && hasOpen)
                        {
                            if (randomWeight < Constants.questionsOpensWeight)
                            {
                                criteria = (q => q.type == QuestionType.fileOpen);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsOpensWeight;
                        }


                        if (question == null)
                        {
                            criteria = (q => q.type == QuestionType.fileSave);
                            question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                        }


                        // Question should be != null by this point, but I leave the condition check
                        // to avoid crashes for the students

                        Debug.Assert(question != null);

                        //if (question != null)
                        //{
                            warningTimer = 0;
                            state = State.warning;
                        //}


                    }
                }

            }
            else if (state == State.warning)
            {
                warningTimer++;

                if (warningTimer < Constants.questionWarningTime)
                {
                    instructorTextBox.Text = "Pregunta en " + (Constants.questionWarningTime - warningTimer);
                    instructorTextBox.Background = new SolidColorBrush(Colors.Yellow);
                }
                else
                {
                    instructorTextBox.Text = "";
                    instructorTextBox.Background = new SolidColorBrush(Colors.Transparent);

                    Debug.Assert(question.HasValue);

                    DialogQuestion d = new();
                    d.Question = question.Value;
                    d.Show();

                    question = null;

                    inputCharactersCounter = 0;
                    commandsCounter = 0;
                    opensCounter = 0;
                    savesCounter = 0;

                    idleWait = random.NextInt64(Constants.minQuestionsTimeWait, Constants.maxQuestionsTimeWait);
                    idleTimer = 0;

                    state = State.idle;

                }
            }

        }

        void OnFileEditorContentsChanged(FileEditor.ContentChange c)
        {
            if (Switches.NoQuestions) { return;  }

            if(c.Added > 0)
            {
                inputCharactersCounter += c.AddedText.Count(c => !ignoreCharacters.Contains(c));

                if (CanFetchQuestion(QuestionType.input))
                {
                    TryFetchInputQuestion();
                }

            }

            inactiveTimer = 0;
        }

        void OnCommandConsoleExecutedCommand(string command, string _)
        {
            commandsCounter++;

            if (CanFetchQuestion(QuestionType.command))
            {
                TryFetchCommandQuestion(command);
            }

            inactiveTimer = 0;
        }

        bool CanFetchQuestion(QuestionType t)
        {
            bool charactersWaitFinished = inputCharactersCounter >= questionCharactersWait;
            bool opensWaitFinished = opensCounter >= questionOpensWait;
            bool savesWaitFinished = savesCounter >= questionSavesWait;
            bool commandsWaitFinished = commandsCounter >= questionCommandsWait;

            bool isTimeTrigger = (t == QuestionType.time);
            bool isCharactersTrigger = (t == QuestionType.input);
            bool isOpensTrigger = (t == QuestionType.fileOpen);
            bool isSavesTrigger = (t == QuestionType.fileSave);
            bool isCommandsTrigger = (t == QuestionType.command);

            bool result = (state == State.fetching) &&
                          (
                            charactersWaitFinished && isCharactersTrigger ||
                            opensWaitFinished && isOpensTrigger ||
                            savesWaitFinished && isSavesTrigger ||
                            commandsWaitFinished && isCommandsTrigger ||
                            isTimeTrigger
                          );

            return result;
        }

        void TryFetchInputQuestion()
        {
            if (templateCandidatesInput.Count > 0)
            {
                int index = random.Next(templateCandidatesInput.Count);

                QuestionTemplate template = templateCandidatesInput[index];

                Console.WriteLine("Trying to fetch input question");

                if (template.contextLines > 0)
                {
                    int currentLine = fileEditor.TextBox.GetLineIndexFromCharacterIndex(fileEditor.TextBox.CaretIndex);
                    int numLines = fileEditor.TextBox.LineCount;
                    int contextStart = currentLine - template.contextLines / 2;
                    int contextEnd = currentLine + template.contextLines / 2;

                    if (contextStart < 0) { contextEnd += -contextStart; contextStart = 0; }
                    else if (contextEnd >= numLines) { contextStart += -(contextEnd - (numLines - 1)); contextEnd = numLines - 1; }

                    string lineText = fileEditor.TextBox.GetLineText(currentLine).Replace("\r\n", "");

                    //int lineSize = lineText.Count((c) => !(Constants.wordSeparators.Contains(c) || Constants.tabEquivalents.Contains(c)));
                    int lineWords = lineText.Split(ignoreCharacters, StringSplitOptions.RemoveEmptyEntries).Length;

                    if (contextStart >= 0 && contextEnd < numLines &&
                        (contextEnd - contextStart + 1) > template.contextLines &&
                        lineWords >= template.contextWords)
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


                        string[] words = lineText.Split(ignoreCharacters, StringSplitOptions.RemoveEmptyEntries);
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

                        questionCandidates.Add(q);
                    }
                    else
                    {
                        if (contextStart < 0 || contextEnd > numLines || (contextEnd - contextStart + 1) < template.contextLines)
                        {
                            Console.WriteLine("Not enough context available (lines)");
                        }
                        else
                        {
                            Console.WriteLine("Not enough context available (words)");
                        }

                    }


                }

            }
        }

        private void TryFetchTimeQuestion()
        {
            if (templateCandidatesTime.Count > 0)
            {
                int index = random.Next(templateCandidatesTime.Count);

                QuestionTemplate template = templateCandidatesTime[index];

                Question q = new();

                q.text = template.question;
                q.timeLimit = template.timeLimit;
                q.type = template.type;

                questionCandidates.Add(q);

            }
        }

        private void TryFetchCommandQuestion(string command)
        {
            if (templateCandidatesCommand.Count > 0)
            {
                int index = random.Next(templateCandidatesCommand.Count);

                QuestionTemplate template = templateCandidatesCommand[index];

                Question q = new();

                string text = template.question;
                text = text.Replace("[commandName]", command);
                q.text = text;

                q.context.command = command;

                q.timeLimit = template.timeLimit;
                q.type = template.type;

                questionCandidates.Add(q);

            }
        }
    }
}