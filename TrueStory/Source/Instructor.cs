using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static TrueStory.Instructor;

namespace TrueStory
{
    public class Instructor
    {
        public event OnQuestionStartsHandler? OnQuestionStarts;
        public event OnQuestionEndsHandler? OnQuestionEnds;

        public delegate void OnQuestionStartsHandler();
        public delegate void OnQuestionEndsHandler();


        public struct CountersState
        {
            public int charactersCounter;
            public int charactersWait;
            public int commandsCounter;
            public int commandsWait;
            public int opensCounter;
            public int opensWait;
            public int savesCounter;
            public int savesWait;
        }

        struct QuestionTemplate
        {
            public Story.QuestionType type;
            public string question;
            public int contextLines;
            public int contextWords;
            public int timeLimit;
        }

        QuestionTemplate[] questionTemplates =
        {
            new() { type = Story.QuestionType.input, question = "¿Para qué sirve '[lineWord]' en la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = Story.QuestionType.input, question = "¿Qué es la palabra '[lineWord]' de la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = Story.QuestionType.input, question = "¿Para qué sirve la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = Story.QuestionType.input, question = "¿Por qué cambiaste la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
            new() { type = Story.QuestionType.fileSave, question = "¿Qué has cambiado en el fichero [fileName] que has guardado recientemente?", contextLines = -1, timeLimit = 15  },
            new() { type = Story.QuestionType.fileOpen, question = "¿Por qué has abierto recientemente el fichero [fileName]?", contextLines = -1, timeLimit = 15  },
            new() { type = Story.QuestionType.fileOpen, question = "¿Para qué sirve el fichero [fileName] que has abierto recientemente?", contextLines = 0, timeLimit = 10  },
            new() { type = Story.QuestionType.command, question = "¿Qué querías probar la última vez que ejecutaste este comando?", contextLines = 0, timeLimit = 10  },
            new() { type = Story.QuestionType.command, question = "¿Tuviste algún problema la última vez que ejecutaste el comando '[commandName]'?", contextLines = 0, timeLimit = 10  },
            new() { type = Story.QuestionType.time, question = "¿Qué estás haciendo ahora mismo en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = Story.QuestionType.time, question = "¿Qué has hecho antes que lo que estás haciendo ahora en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = Story.QuestionType.time, question = "¿Cuál es el último problema que has tenido en esta actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = Story.QuestionType.time, question = "¿Cuál es el problema más importante que has tenido hasta ahora en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = Story.QuestionType.time, question = "¿Qué te preocupa ahora mismo que crees que no sabrás resolver en la actividad?", contextLines = 0, timeLimit = 20  },
            new() { type = Story.QuestionType.time, question = "Explica algo que estés particularmente feliz de haber solucionado en la actividad", contextLines = 0, timeLimit = 20  },
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

        List<Story.QuestionData> questionCandidates;
        List<Story.QuestionData> questionShortList;
        List<Story.QuestionType> questionShortListTypes;
        Story.QuestionData? question;

        char[] ignoreCharacters;


        int questionCharactersWait;
        int questionCommandsWait;
        int questionOpensWait;
        int questionSavesWait;


        Random random;

        MainWindow window;
        FileEditor fileEditor;

        DispatcherTimer timer;

        TextBlock instructorTextBox;

        int idleWait;
        int fetchWait;

        int idleTimer;
        int fetchTimer;
        int warningTimer;

        int inactiveTimer;

        int inputCharactersCounter;
        int opensCounter;
        int savesCounter;
        int commandsCounter;

        public CountersState Counters
        {
            get
            {
                CountersState s = new();
                s.charactersCounter = inputCharactersCounter;
                s.charactersWait = questionCharactersWait;
                s.commandsCounter = commandsCounter;
                s.commandsWait = questionCommandsWait;
                s.opensCounter = opensCounter;
                s.opensWait = questionOpensWait;
                s.savesCounter = savesCounter;
                s.savesWait = questionSavesWait;


                return s;
            }

            set
            {
                int randCharsWait = random.Next(Constants.minQuestionCharactersWait, Constants.maxQuestionCharactersWait + 1);
                int randOpensWait = random.Next(Constants.minQuestionOpensWait, Constants.maxQuestionOpensWait + 1);
                int randSavesWait = random.Next(Constants.minQuestionSavesWait, Constants.maxQuestionSavesWait + 1);
                int randCommandsWait = random.Next(Constants.minQuestionCommandsWait, Constants.maxQuestionCommandsWait + 1);


                inputCharactersCounter = value.charactersCounter;
                questionCharactersWait = value.charactersWait > 0 ? value.charactersWait : randCharsWait;
                commandsCounter = value.commandsCounter;
                questionCommandsWait = value.commandsWait > 0 ? value.commandsWait : randCommandsWait; ;
                opensCounter = value.opensCounter;
                questionOpensWait = value.opensWait > 0 ? value.opensWait: randOpensWait; ;
                savesCounter = value.savesCounter;
                questionSavesWait = value.savesWait > 0 ? value.savesWait: randSavesWait; ;
            }
        }
        

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

            questionCharactersWait = random.Next(Constants.minQuestionCharactersWait, Constants.maxQuestionCharactersWait + 1);
            questionOpensWait = random.Next(Constants.minQuestionOpensWait, Constants.maxQuestionOpensWait + 1);
            questionSavesWait = random.Next(Constants.minQuestionSavesWait, Constants.maxQuestionSavesWait + 1);
            questionCommandsWait = random.Next(Constants.minQuestionCommandsWait, Constants.maxQuestionCommandsWait + 1);

            idleWait = random.Next(Constants.minQuestionsTimeWait, Constants.maxQuestionsTimeWait + 1);

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
            templateCandidatesTime = templatesList.FindAll((q) => q.type == Story.QuestionType.time);
            templateCandidatesInput = templatesList.FindAll((q) => q.type == Story.QuestionType.input);
            templateCandidatesCommand = templatesList.FindAll((q) => q.type == Story.QuestionType.command);
            templateCandidatesOpen = templatesList.FindAll((q) => q.type == Story.QuestionType.fileOpen);
            templateCandidatesSave = templatesList.FindAll((q) => q.type == Story.QuestionType.fileSave);


            timer = new();
            timer.Tick += OnTimerTick;
            timer.Interval = new TimeSpan(0, 0, 1);


            timer.Start();

        }

        void OnTimerTick(object? sender, EventArgs e)
        {
            if (!fileEditor.IsFileLoaded) { return;  }

            Utils.Log("Instructor: " + state + ": IDLE " + idleTimer + "/" + idleWait +
                                                    ", FETC" + fetchTimer + "/" + fetchWait +
                                                    ", WARN " + warningTimer + "/" + Constants.questionWarningTime +
                                                    " IC " + inputCharactersCounter + "/" + questionCharactersWait + 
                                                    " CC " + commandsCounter + "/" + questionCommandsWait);

            inactiveTimer++;

            if (CanFetchQuestion(Story.QuestionType.time))
            {
                TryFetchTimeQuestion();
            }

            if (state == State.idle)
            {
                if (inactiveTimer <= Constants.inactiveWait)
                {
                    idleTimer++;
                }

                if (Keyboard.IsKeyDown(Key.F12))
                {
                    idleTimer = idleWait;
                    inputCharactersCounter = questionCharactersWait;
                    opensCounter = questionOpensWait;
                    savesCounter = questionSavesWait;
                    commandsCounter = questionCommandsWait;
                }


                if (idleTimer > idleWait)
                {
                    fetchWait = random.Next(Constants.minQuestionsFetchWait, Constants.maxQuestionsFetchWait + 1);
                    fetchTimer = 0;

                    questionCandidates.Clear();
                    

                    //instructorTextBox.Text = "Observando";
                    //instructorTextBox.Background = new SolidColorBrush(Colors.Orange);

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
                        Utils.Log("Choosing question between " + questionCandidates.Count + " candidates");

                        Predicate<Story.QuestionData> filter = (Story.QuestionData q) => q.type == Story.QuestionType.time;
                        Story.QuestionData? timeQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Story.QuestionData q) => q.type == Story.QuestionType.input;
                        Story.QuestionData? inputQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Story.QuestionData q) => q.type == Story.QuestionType.command;
                        Story.QuestionData? commandsQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Story.QuestionData q) => q.type == Story.QuestionType.fileOpen;
                        Story.QuestionData? opensQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (Story.QuestionData q) => q.type == Story.QuestionType.fileSave;
                        Story.QuestionData? savesQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        questionShortList.Clear();

                        if (timeQuestion.HasValue) { questionShortList.Add(timeQuestion.Value); }
                        if (inputQuestion.HasValue) { questionShortList.Add(inputQuestion.Value); }
                        if (commandsQuestion.HasValue) { questionShortList.Add(commandsQuestion.Value); }
                        if (opensQuestion.HasValue) { questionShortList.Add(opensQuestion.Value); }
                        if (savesQuestion.HasValue) { questionShortList.Add(savesQuestion.Value); }

                        Utils.Log("Shortlist:");
                        foreach (Story.QuestionData q in questionShortList)
                        {
                            Utils.Log(q.type + ": " + q.text);
                        }

                        questionShortListTypes.Clear();

                        if (questionShortList.FindIndex(q => q.type == Story.QuestionType.time) >= 0) { questionShortListTypes.Add(Story.QuestionType.time); }
                        if (questionShortList.FindIndex(q => q.type == Story.QuestionType.input) >= 0) { questionShortListTypes.Add(Story.QuestionType.input); }
                        if (questionShortList.FindIndex(q => q.type == Story.QuestionType.command) >= 0) { questionShortListTypes.Add(Story.QuestionType.command); }
                        if (questionShortList.FindIndex(q => q.type == Story.QuestionType.fileOpen) >= 0) { questionShortListTypes.Add(Story.QuestionType.fileOpen); }
                        if (questionShortList.FindIndex(q => q.type == Story.QuestionType.fileSave) >= 0) { questionShortListTypes.Add(Story.QuestionType.fileSave); }

                        int totalWeight = 0;

                        bool hasTime = questionShortListTypes.Contains(Story.QuestionType.time);
                        bool hasInput = questionShortListTypes.Contains(Story.QuestionType.input);
                        bool hasCommand = questionShortListTypes.Contains(Story.QuestionType.command);
                        bool hasOpen = questionShortListTypes.Contains(Story.QuestionType.fileOpen);
                        bool hasSave = questionShortListTypes.Contains(Story.QuestionType.fileSave);

                        if (hasTime) { totalWeight += Constants.questionsTimeWeight; }
                        if (hasInput) { totalWeight += Constants.questionsInputWeight; }
                        if (hasCommand) { totalWeight += Constants.questionsCommandsWeight; }
                        if (hasOpen) { totalWeight += Constants.questionsOpensWeight; }
                        if (hasSave) { totalWeight += Constants.questionsSavesWeight; }

                        int randomWeight = random.Next(0, totalWeight);

                        Predicate<Story.QuestionData> criteria;

                        if (hasTime)
                        {
                            if (randomWeight < Constants.questionsTimeWeight)
                            {
                                criteria = (q => q.type == Story.QuestionType.time);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsTimeWeight;

                        }


                        if (question == null && hasInput)
                        {
                            if (randomWeight < Constants.questionsInputWeight)
                            {
                                criteria = (q => q.type == Story.QuestionType.input);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsInputWeight;
                        }


                        if (question == null && hasCommand)
                        {
                            if (randomWeight < Constants.questionsCommandsWeight)
                            {
                                criteria = (q => q.type == Story.QuestionType.command);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsCommandsWeight;
                        }

                        if (question == null && hasOpen)
                        {
                            if (randomWeight < Constants.questionsOpensWeight)
                            {
                                criteria = (q => q.type == Story.QuestionType.fileOpen);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= Constants.questionsOpensWeight;
                        }


                        if (question == null)
                        {
                            criteria = (q => q.type == Story.QuestionType.fileSave);
                            question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                        }


                        // Question should be != null by this point, but I leave the condition check
                        // to avoid crashes for the students
                        //Debug.Assert(question != null);

                        if (question != null)
                        {
                            OnQuestionStarts?.Invoke();

                            warningTimer = 0;
                            state = State.warning;
                        }


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

                    Debug.Assert(window.Story != null);

                    window.Story.AddQuestion(question.Value);

                    question = null;

                    OnQuestionEnds?.Invoke();

                    inputCharactersCounter = 0;
                    commandsCounter = 0;
                    opensCounter = 0;
                    savesCounter = 0;

                    questionCharactersWait = random.Next(Constants.minQuestionCharactersWait, Constants.maxQuestionCharactersWait + 1);
                    questionOpensWait = random.Next(Constants.minQuestionOpensWait, Constants.maxQuestionOpensWait + 1);
                    questionSavesWait = random.Next(Constants.minQuestionSavesWait, Constants.maxQuestionSavesWait + 1);
                    questionCommandsWait = random.Next(Constants.minQuestionCommandsWait, Constants.maxQuestionCommandsWait + 1);

                    idleWait = random.Next(Constants.minQuestionsTimeWait, Constants.maxQuestionsTimeWait + 1);
                    idleTimer = 0;

                    state = State.idle;

                }
            }

        }

        void OnFileEditorContentsChanged(FileEditor.ContentChange c)
        {
            if(c.Added > 0)
            {
                inputCharactersCounter += c.AddedText.Count(c => !ignoreCharacters.Contains(c));

                if (CanFetchQuestion(Story.QuestionType.input))
                {
                    TryFetchInputQuestion();
                }

            }

            inactiveTimer = 0;
        }

        void OnCommandConsoleExecutedCommand(string command, string _)
        {
            commandsCounter++;

            if (CanFetchQuestion(Story.QuestionType.command))
            {
                TryFetchCommandQuestion(command);
            }

            inactiveTimer = 0;
        }

        bool CanFetchQuestion(Story.QuestionType t)
        {
            bool charactersWaitFinished = inputCharactersCounter >= questionCharactersWait;
            bool opensWaitFinished = opensCounter >= questionOpensWait;
            bool savesWaitFinished = savesCounter >= questionSavesWait;
            bool commandsWaitFinished = commandsCounter >= questionCommandsWait;

            bool isTimeTrigger = (t == Story.QuestionType.time);
            bool isCharactersTrigger = (t == Story.QuestionType.input);
            bool isOpensTrigger = (t == Story.QuestionType.fileOpen);
            bool isSavesTrigger = (t == Story.QuestionType.fileSave);
            bool isCommandsTrigger = (t == Story.QuestionType.command);

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

                Utils.Log("Trying to fetch input question");

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
                        Story.QuestionContextData context = new();

                        string? fileName = window.LoadedFile;

                        Debug.Assert(fileName != null);

                        context.fileName = fileName;
                        context.currentLine = currentLine;

                        List<Story.QuestionContextLineData> lines = new();
                        for (int i = contextStart; i <= contextEnd; i++)
                        {
                            Story.QuestionContextLineData line = new();
                            line.number = i;
                            line.text = fileEditor.TextBox.GetLineText(i).Replace("\r\n", "");
                            lines.Add(line);
                        }

                        context.lines = lines.ToArray();

                        Story.QuestionData q = new Story.QuestionData();


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
                            Utils.Log("Not enough context available (lines)");
                        }
                        else
                        {
                            Utils.Log("Not enough context available (words)");
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

                Story.QuestionData q = new();

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

                Story.QuestionData q = new();

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