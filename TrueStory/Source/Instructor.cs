using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        public Story? Story { get { return story; } set { story = value; OnStoryChanged(); } }

        Story? story;

        //RecordData.QuestionTemplateData[] questionTemplates =
        //{
        //    new() { type = RecordData.QuestionType.input, question = "¿Para qué sirve '[lineWord]' en la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
        //    new() { type = RecordData.QuestionType.input, question = "¿Qué es la palabra '[lineWord]' de la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
        //    new() { type = RecordData.QuestionType.input, question = "¿Para qué sirve la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
        //    new() { type = RecordData.QuestionType.input, question = "¿Por qué cambiaste la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
        //    new() { type = RecordData.QuestionType.fileSave, question = "¿Qué has cambiado en el fichero [fileName] que has guardado recientemente?", contextLines = -1, timeLimit = 15  },
        //    new() { type = RecordData.QuestionType.fileOpen, question = "¿Por qué has abierto recientemente el fichero [fileName]?", contextLines = -1, timeLimit = 15  },
        //    new() { type = RecordData.QuestionType.fileOpen, question = "¿Para qué sirve el fichero [fileName] que has abierto recientemente?", contextLines = 0, timeLimit = 10  },
        //    new() { type = RecordData.QuestionType.command, question = "¿Qué querías probar la última vez que ejecutaste este comando?", contextLines = 0, timeLimit = 10  },
        //    new() { type = RecordData.QuestionType.command, question = "¿Tuviste algún problema la última vez que ejecutaste el comando '[commandName]'?", contextLines = 0, timeLimit = 10  },
        //    new() { type = RecordData.QuestionType.time, question = "¿Qué estás haciendo ahora mismo en la actividad?", contextLines = 0, timeLimit = 20  },
        //    new() { type = RecordData.QuestionType.time, question = "¿Qué has hecho antes que lo que estás haciendo ahora en la actividad?", contextLines = 0, timeLimit = 20  },
        //    new() { type = RecordData.QuestionType.time, question = "¿Cuál es el último problema que has tenido en esta actividad?", contextLines = 0, timeLimit = 20  },
        //    new() { type = RecordData.QuestionType.time, question = "¿Cuál es el problema más importante que has tenido hasta ahora en la actividad?", contextLines = 0, timeLimit = 20  },
        //    new() { type = RecordData.QuestionType.time, question = "¿Qué te preocupa ahora mismo que crees que no sabrás resolver en la actividad?", contextLines = 0, timeLimit = 20  },
        //    new() { type = RecordData.QuestionType.time, question = "Explica algo que estés particularmente feliz de haber solucionado en la actividad", contextLines = 0, timeLimit = 20  },
        //};

        enum State
        {
            idle,
            fetching,
            warning
        };

        State state;

        List<RecordData.QuestionTemplateData> templateCandidatesTime;
        List<RecordData.QuestionTemplateData> templateCandidatesInput;
        List<RecordData.QuestionTemplateData> templateCandidatesCommand;
        List<RecordData.QuestionTemplateData> templateCandidatesOpen;
        List<RecordData.QuestionTemplateData> templateCandidatesSave;

        List<RecordData.QuestionData> questionCandidates;
        List<RecordData.QuestionData> questionShortList;
        List<RecordData.QuestionType> questionShortListTypes;
        RecordData.QuestionData? question;

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

        Dictionary<RecordData.QuestionType, int> chancesWeight;
        Dictionary<string, char[]> separators;

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
                Debug.Assert(story != null);

                ref readonly RecordData.InstructorWaitData wait = ref story.Header.instructor.wait;

                int randCharsWait = random.Next(wait.characters.min, wait.characters.max + 1);
                int randOpensWait = random.Next(wait.fileOpens.min, wait.fileOpens.max + 1);
                int randSavesWait = random.Next(wait.fileSaves.min, wait.fileSaves.max + 1);
                int randCommandsWait = random.Next(wait.commands.min, wait.commands.max + 1);


                inputCharactersCounter = value.charactersCounter;
                questionCharactersWait = value.charactersWait > 0 ? value.charactersWait : randCharsWait;
                commandsCounter = value.commandsCounter;
                questionCommandsWait = value.commandsWait > 0 ? value.commandsWait : randCommandsWait;
                opensCounter = value.opensCounter;
                questionOpensWait = value.opensWait > 0 ? value.opensWait: randOpensWait;
                savesCounter = value.savesCounter;
                questionSavesWait = value.savesWait > 0 ? value.savesWait: randSavesWait;
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

            templateCandidatesTime = new();
            templateCandidatesInput = new();
            templateCandidatesCommand = new();
            templateCandidatesOpen = new();
            templateCandidatesSave = new();

            questionCandidates = new();
            questionShortList = new();
            questionShortListTypes = new();

            chancesWeight = new();
            separators = new();

            instructorTextBox.Text = "";
            instructorTextBox.Background = new SolidColorBrush(Colors.Transparent);

            timer = new();
            timer.Tick += OnTimerTick;
            timer.Interval = new TimeSpan(0, 0, 1);

            state = State.idle;

        }

        void OnStoryChanged()
        {
            bool startInstructor;

            if (story == null) { startInstructor = false; }
            else if (story.Header.instructor.enabled) { startInstructor = true; }
            else { startInstructor = false; }

            if (startInstructor)
            {
                Debug.Assert(story != null);

                ref readonly RecordData.InstructorWaitData wait = ref story.Header.instructor.wait;

                questionCharactersWait = random.Next(wait.characters.min, wait.characters.max + 1);
                questionOpensWait = random.Next(wait.fileOpens.min, wait.fileOpens.max + 1);
                questionSavesWait = random.Next(wait.fileSaves.min, wait.fileSaves.max + 1);
                questionCommandsWait = random.Next(wait.commands.min, wait.commands.max + 1);

                idleWait = random.Next(wait.time.min, wait.time.max + 1); ;

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

                List<RecordData.QuestionTemplateData> templatesList = new(story.Header.instructor.ask.templates);
                templateCandidatesTime = templatesList.FindAll((q) => q.type == RecordData.QuestionType.time);
                templateCandidatesInput = templatesList.FindAll((q) => q.type == RecordData.QuestionType.input);
                templateCandidatesCommand = templatesList.FindAll((q) => q.type == RecordData.QuestionType.command);
                templateCandidatesOpen = templatesList.FindAll((q) => q.type == RecordData.QuestionType.fileOpen);
                templateCandidatesSave = templatesList.FindAll((q) => q.type == RecordData.QuestionType.fileSave);

                Debug.Assert(story != null);

                chancesWeight.Clear();
                foreach (RecordData.QuestionChanceData c in story.Header.instructor.ask.chances)
                {
                    chancesWeight.Add(c.type, c.weight);
                }

                separators.Clear();
                foreach (RecordData.InstructorObserveWordSeparators s in story.Header.instructor.observe.wordSeparators)
                {
                    char[] all = new char[Constants.tabEquivalents.Length + s.separators.Length];
                    Constants.tabEquivalents.CopyTo(all, 0);
                    s.separators.CopyTo(all, Constants.tabEquivalents.Length);
                    separators.Add(s.extension, all);
                }

                timer.Start();

                Console.WriteLine("Instructor started");
            }
            else
            {
                timer.Stop();

                Console.WriteLine("Instructor stopped");
            }

            instructorTextBox.Text = "";
            instructorTextBox.Background = new SolidColorBrush(Colors.Transparent);
            state = State.idle;

        }

        void OnTimerTick(object? sender, EventArgs e)
        {
            if (!fileEditor.IsFileLoaded) { return;  }
            if (story == null) { return;  }

            ref readonly RecordData.InstructorWaitData wait = ref story.Header.instructor.wait;
            ref readonly RecordData.InstructorObserveData observe = ref story.Header.instructor.observe;
            ref readonly RecordData.InstructorAskQuestionData ask = ref story.Header.instructor.ask;

            Utils.ClearLog();
            Utils.Log("Instructor      : " + state);
            Utils.Log("Idle timer      : " + idleTimer + "/" + idleWait );
            Utils.Log("Fetch timer     : " + fetchTimer + "/" + fetchWait );
            Utils.Log("Warning timer   : " + warningTimer + "/" + Constants.questionWarningTime);
            Utils.Log("Characters wait : " + inputCharactersCounter + "/" + questionCharactersWait);
            Utils.Log("Commands wait   : " + commandsCounter + "/" + questionCommandsWait);

            inactiveTimer++;

            if (CanFetchQuestion(RecordData.QuestionType.time))
            {
                TryFetchTimeQuestion();
            }

            if (state == State.idle)
            {
                if (inactiveTimer <= Constants.inactiveWait)
                {
                    idleTimer++;
                }

                if (Keyboard.IsKeyDown(Key.F11))
                {
                    idleTimer = idleWait;
                    inputCharactersCounter = questionCharactersWait;
                    opensCounter = questionOpensWait;
                    savesCounter = questionSavesWait;
                    commandsCounter = questionCommandsWait;
                }




                if (idleTimer > idleWait)
                {
                    fetchWait = random.Next(observe.time.min, observe.time.max + 1);
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

                if (Keyboard.IsKeyDown(Key.F12))
                {
                    fetchTimer = fetchWait + 1;
                }

                if (fetchTimer > fetchWait)
                {
                    if(questionCandidates.Count > 0)
                    {
                        //Utils.Log("Choosing question between " + questionCandidates.Count + " candidates");

                        Predicate<RecordData.QuestionData> filter = (RecordData.QuestionData q) => q.type == RecordData.QuestionType.time;
                        RecordData.QuestionData? timeQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (RecordData.QuestionData q) => q.type == RecordData.QuestionType.input;
                        RecordData.QuestionData? inputQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (RecordData.QuestionData q) => q.type == RecordData.QuestionType.command;
                        RecordData.QuestionData? commandsQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (RecordData.QuestionData q) => q.type == RecordData.QuestionType.fileOpen;
                        RecordData.QuestionData? opensQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        filter = (RecordData.QuestionData q) => q.type == RecordData.QuestionType.fileSave;
                        RecordData.QuestionData? savesQuestion = questionCandidates.FindLastIndex(filter) >= 0 ? questionCandidates.Find(filter) : null;

                        questionShortList.Clear();

                        if (timeQuestion.HasValue) { questionShortList.Add(timeQuestion.Value); }
                        if (inputQuestion.HasValue) { questionShortList.Add(inputQuestion.Value); }
                        if (commandsQuestion.HasValue) { questionShortList.Add(commandsQuestion.Value); }
                        if (opensQuestion.HasValue) { questionShortList.Add(opensQuestion.Value); }
                        if (savesQuestion.HasValue) { questionShortList.Add(savesQuestion.Value); }

                        //Utils.Log("Shortlist:");
                        foreach (RecordData.QuestionData q in questionShortList)
                        {
                            //Utils.Log(q.type + ": " + q.text);
                        }

                        questionShortListTypes.Clear();

                        if (questionShortList.FindIndex(q => q.type == RecordData.QuestionType.time) >= 0) { questionShortListTypes.Add(RecordData.QuestionType.time); }
                        if (questionShortList.FindIndex(q => q.type == RecordData.QuestionType.input) >= 0) { questionShortListTypes.Add(RecordData.QuestionType.input); }
                        if (questionShortList.FindIndex(q => q.type == RecordData.QuestionType.command) >= 0) { questionShortListTypes.Add(RecordData.QuestionType.command); }
                        if (questionShortList.FindIndex(q => q.type == RecordData.QuestionType.fileOpen) >= 0) { questionShortListTypes.Add(RecordData.QuestionType.fileOpen); }
                        if (questionShortList.FindIndex(q => q.type == RecordData.QuestionType.fileSave) >= 0) { questionShortListTypes.Add(RecordData.QuestionType.fileSave); }

                        int totalWeight = 0;

                        bool hasTime = questionShortListTypes.Contains(RecordData.QuestionType.time);
                        bool hasInput = questionShortListTypes.Contains(RecordData.QuestionType.input);
                        bool hasCommand = questionShortListTypes.Contains(RecordData.QuestionType.command);
                        bool hasOpen = questionShortListTypes.Contains(RecordData.QuestionType.fileOpen);
                        bool hasSave = questionShortListTypes.Contains(RecordData.QuestionType.fileSave);

                        Debug.Assert(chancesWeight.ContainsKey(RecordData.QuestionType.time));
                        Debug.Assert(chancesWeight.ContainsKey(RecordData.QuestionType.input));
                        Debug.Assert(chancesWeight.ContainsKey(RecordData.QuestionType.command));
                        Debug.Assert(chancesWeight.ContainsKey(RecordData.QuestionType.fileOpen));
                        Debug.Assert(chancesWeight.ContainsKey(RecordData.QuestionType.fileSave));


                        if (hasTime) { totalWeight += chancesWeight[RecordData.QuestionType.time]; }
                        if (hasInput) { totalWeight += chancesWeight[RecordData.QuestionType.input]; }
                        if (hasCommand) { totalWeight += chancesWeight[RecordData.QuestionType.command]; }
                        if (hasOpen) { totalWeight += chancesWeight[RecordData.QuestionType.fileOpen]; }
                        if (hasSave) { totalWeight += chancesWeight[RecordData.QuestionType.fileSave]; }

                        int randomWeight = random.Next(0, totalWeight);

                        Predicate<RecordData.QuestionData> criteria;

                        if (hasTime)
                        {
                            if (randomWeight < chancesWeight[RecordData.QuestionType.time])
                            {
                                criteria = (q => q.type == RecordData.QuestionType.time);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= chancesWeight[RecordData.QuestionType.time];

                        }


                        if (question == null && hasInput)
                        {
                            if (randomWeight < chancesWeight[RecordData.QuestionType.input])
                            {
                                criteria = (q => q.type == RecordData.QuestionType.input);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= chancesWeight[RecordData.QuestionType.input];
                        }


                        if (question == null && hasCommand)
                        {
                            if (randomWeight < chancesWeight[RecordData.QuestionType.command])
                            {
                                criteria = (q => q.type == RecordData.QuestionType.command);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= chancesWeight[RecordData.QuestionType.command];
                        }

                        if (question == null && hasOpen)
                        {
                            if (randomWeight < chancesWeight[RecordData.QuestionType.fileOpen])
                            {
                                criteria = (q => q.type == RecordData.QuestionType.fileOpen);
                                question = questionShortList.FindLastIndex(criteria) >= 0 ? questionShortList.FindLast(criteria) : null;
                            }

                            randomWeight -= chancesWeight[RecordData.QuestionType.fileOpen];
                        }


                        if (question == null)
                        {
                            criteria = (q => q.type == RecordData.QuestionType.fileSave);
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

                    questionCharactersWait = random.Next(wait.characters.min, wait.characters.max + 1);
                    questionOpensWait = random.Next(wait.fileOpens.min, wait.fileOpens.max + 1);
                    questionSavesWait = random.Next(wait.fileSaves.min, wait.fileSaves.max + 1);
                    questionCommandsWait = random.Next(wait.commands.min, wait.commands.max + 1);

                    idleWait = random.Next(wait.time.min, wait.time.max + 1);
                    idleTimer = 0;

                    state = State.idle;

                }
            }

        }

        void OnFileEditorContentsChanged(FileEditor.ContentChange c)
        {

            if(c.Added > 0)
            {
                string? extension = Utils.GetExtension(fileEditor.LoadedFilePath)?.ToLower();
                Debug.Assert(extension != null);
                Debug.Assert(separators.ContainsKey(extension));

                char[] ignoreCharacters = separators[extension];

                inputCharactersCounter += c.AddedText.Count(c => !ignoreCharacters.Contains(c));

                if (CanFetchQuestion(RecordData.QuestionType.input))
                {
                    TryFetchInputQuestion();
                }

            }

            inactiveTimer = 0;
        }

        void OnCommandConsoleExecutedCommand(string command, string _)
        {
            commandsCounter++;

            if (CanFetchQuestion(RecordData.QuestionType.command))
            {
                TryFetchCommandQuestion(command);
            }

            inactiveTimer = 0;
        }

        bool CanFetchQuestion(RecordData.QuestionType t)
        {
            bool charactersWaitFinished = inputCharactersCounter >= questionCharactersWait;
            bool opensWaitFinished = opensCounter >= questionOpensWait;
            bool savesWaitFinished = savesCounter >= questionSavesWait;
            bool commandsWaitFinished = commandsCounter >= questionCommandsWait;

            bool isTimeTrigger = (t == RecordData.QuestionType.time);
            bool isCharactersTrigger = (t == RecordData.QuestionType.input);
            bool isOpensTrigger = (t == RecordData.QuestionType.fileOpen);
            bool isSavesTrigger = (t == RecordData.QuestionType.fileSave);
            bool isCommandsTrigger = (t == RecordData.QuestionType.command);

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

                RecordData.QuestionTemplateData template = templateCandidatesInput[index];

                //Utils.Log("Trying to fetch input question");

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

                    string? extension = Utils.GetExtension(fileEditor.LoadedFilePath)?.ToLower();
                    Debug.Assert(extension != null);
                    Debug.Assert(separators.ContainsKey(extension));

                    char[] ignoreCharacters = separators[extension];

                    int lineWords = lineText.Split(ignoreCharacters, StringSplitOptions.RemoveEmptyEntries).Length;

                    if (contextStart >= 0 && contextEnd < numLines &&
                        (contextEnd - contextStart + 1) > template.contextLines &&
                        lineWords >= template.contextWords)
                    {
                        RecordData.QuestionContextData context = new();

                        string? fileName = window.LoadedFile;

                        Debug.Assert(fileName != null);

                        context.fileName = fileName;
                        context.currentLine = currentLine;

                        List<RecordData.QuestionContextLineData> lines = new();
                        for (int i = contextStart; i <= contextEnd; i++)
                        {
                            RecordData.QuestionContextLineData line = new();
                            line.number = i;
                            line.text = fileEditor.TextBox.GetLineText(i).Replace("\r\n", "");
                            lines.Add(line);
                        }

                        context.lines = lines.ToArray();

                        RecordData.QuestionData q = new RecordData.QuestionData();


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
                            //Utils.Log("Not enough context available (lines)");
                        }
                        else
                        {
                            //Utils.Log("Not enough context available (words)");
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

                RecordData.QuestionTemplateData template = templateCandidatesTime[index];

                RecordData.QuestionData q = new();

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

                RecordData.QuestionTemplateData template = templateCandidatesCommand[index];

                RecordData.QuestionData q = new();

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