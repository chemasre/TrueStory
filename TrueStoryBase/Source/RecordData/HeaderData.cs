using System.Reflection.PortableExecutable;

namespace TrueStory
{
    public partial class  RecordData
    {

        // Common

        public struct IntRangeData
        {
            public int min = 0;
            public int max = 0;

            public IntRangeData() { }
        }

        // Instructor

        public struct QuestionChanceData
        {
            public RecordData.QuestionType type = RecordData.QuestionType.time;
            public int weight = 1;

            public QuestionChanceData() { }
        }

        public struct InstructorWaitData
        {
            public IntRangeData time = new()
            {
                min = 60,
                max = 180
            };

            public IntRangeData characters = new()
            {
                min = 120,
                max = 240
            };

            public IntRangeData fileOpens = new()
            {
                min = 0,
                max = 2
            };

            public IntRangeData fileSaves = new()
            {
                min = 0,
                max = 2
            };

            public IntRangeData commands = new()
            {
                min = 0,
                max = 2
            };

            public InstructorWaitData() { }

        }

        public struct InstructorObserveWordSeparators
        {
            public string extension = "txt" ;
            public char[] separators = new char[] {'.',',','-','(',')','\'' };

            public InstructorObserveWordSeparators() { }
        }

        public struct InstructorObserveData
        {
            public IntRangeData time = new()
            {
                min = (int)(60 * 0.5f),
                max = (int)(60 * 1.5f)
            };

            public InstructorObserveWordSeparators[] wordSeparators = new InstructorObserveWordSeparators[]
            {
                new ()
                {
                    extension = "txt",
                    separators = new char[] {'.',',','-','(',')','\'' }

                },
                new ()
                {
                    extension = "h",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '{', '}', ':', '=' }
                },
                new ()
                {
                    extension = "c",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '{', '}', ':', '=' }
                },
                new ()
                {
                    extension = "hpp",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '{', '}', ':', '=' }
                },
                new ()
                {
                    extension = "cpp",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '{', '}', ':', '=' }
                },
                new ()
                {
                    extension = "htm",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '<','>','/' }
                },
                new ()
                {
                    extension = "html",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '<','>','/' }
                },
                new ()
                {
                    extension = "css",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '<','>','/','{','}',':' }
                },
                new ()
                {
                    extension = "js",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '<','>','/','{','}',':', '=' }
                },
                new ()
                {
                    extension = "java",
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',', '<','>','/','{','}',':','=' }
                }

            };

            public InstructorObserveData() { }
        }

        public struct QuestionTemplateData
        {
            public RecordData.QuestionType type = RecordData.QuestionType.time;
            public string question = "Texto de la pregunta";
            public int contextLines = 10;
            public int contextWords = 3;
            public int timeLimit = 10;

            public QuestionTemplateData() { }
        }

        public struct InstructorAskQuestionData
        {
            public QuestionTemplateData[] templates = new QuestionTemplateData[]
            {
                new() { type = RecordData.QuestionType.input, question = "¿Para qué sirve '[lineWord]' en la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
                new() { type = RecordData.QuestionType.input, question = "¿Qué es la palabra '[lineWord]' de la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
                new() { type = RecordData.QuestionType.input, question = "¿Para qué sirve la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
                new() { type = RecordData.QuestionType.input, question = "¿Por qué cambiaste la línea [lineIndex]?", contextLines = 10, contextWords = 3, timeLimit = 20 },
                new() { type = RecordData.QuestionType.fileSave, question = "¿Qué has cambiado en el fichero [fileName] que has guardado recientemente?", contextLines = -1, timeLimit = 15  },
                new() { type = RecordData.QuestionType.fileOpen, question = "¿Por qué has abierto recientemente el fichero [fileName]?", contextLines = -1, timeLimit = 15  },
                new() { type = RecordData.QuestionType.fileOpen, question = "¿Para qué sirve el fichero [fileName] que has abierto recientemente?", contextLines = 0, timeLimit = 10  },
                new() { type = RecordData.QuestionType.command, question = "¿Qué querías probar la última vez que ejecutaste este comando?", contextLines = 0, timeLimit = 10  },
                new() { type = RecordData.QuestionType.command, question = "¿Tuviste algún problema la última vez que ejecutaste el comando '[commandName]'?", contextLines = 0, timeLimit = 10  },
                new() { type = RecordData.QuestionType.time, question = "¿Qué estás haciendo ahora mismo en la actividad?", contextLines = 0, timeLimit = 20  },
                new() { type = RecordData.QuestionType.time, question = "¿Qué has hecho antes que lo que estás haciendo ahora en la actividad?", contextLines = 0, timeLimit = 20  },
                new() { type = RecordData.QuestionType.time, question = "¿Cuál es el último problema que has tenido en esta actividad?", contextLines = 0, timeLimit = 20  },
                new() { type = RecordData.QuestionType.time, question = "¿Cuál es el problema más importante que has tenido hasta ahora en la actividad?", contextLines = 0, timeLimit = 20  },
                new() { type = RecordData.QuestionType.time, question = "¿Qué te preocupa ahora mismo que crees que no sabrás resolver en la actividad?", contextLines = 0, timeLimit = 20  },
                new() { type = RecordData.QuestionType.time, question = "Explica algo que estés particularmente feliz de haber solucionado en la actividad", contextLines = 0, timeLimit = 20  },
            };

            public QuestionChanceData[] chances = new QuestionChanceData[]
            {
                new()
                {
                    type = QuestionType.time,
                    weight = 5
                },
                new()
                {
                    type = QuestionType.input,
                    weight = 75
                },
                new()
                {
                    type = QuestionType.fileOpen,
                    weight = 1
                },
                new()
                {
                    type = QuestionType.fileSave,
                    weight = 1
                },
                new()
                {
                    type = QuestionType.command,
                    weight = 20
                }
            };

            public InstructorAskQuestionData() { }
        }

        public struct InstructorData
        {
            public bool enabled = false;
            public InstructorWaitData wait = new();
            public InstructorObserveData observe = new();
            public InstructorAskQuestionData ask = new();

            public InstructorData() { }
        }


        public struct NewHeaderData
        {
            public string title = "Actividad sin título";
            public string student = "Nombre estudiante";
            public string guid = "";
            public string[] editableExtensions = new string[]
            {
                "txt", // "csv", "md",
                "htm", "html", "css", "js",
                //"php",
                //"tsx",
                "c", "h",
                "hpp", "cpp",
                //"cs",
                "java"
            };
            
            public InstructorData instructor = new();

            public NewHeaderData() { }
        }

        public static Story.Record EncodeNewHeaderRecord(NewHeaderData r)
        {
            Story.Record result = new();
            result.type = Story.RecordType.Header;

            BinaryBufferWriter buffer = new();

            buffer.WriteString(r.title);
            buffer.WriteString(r.student);
            buffer.WriteString(r.guid);
            buffer.WriteInt(r.editableExtensions.Length);
            foreach (string extension in r.editableExtensions) { buffer.WriteString(extension); }
            
            buffer.WriteInt(r.instructor.enabled ? 1 : 0);
            
            buffer.WriteInt(r.instructor.wait.time.min);
            buffer.WriteInt(r.instructor.wait.time.max);
            buffer.WriteInt(r.instructor.wait.characters.min);
            buffer.WriteInt(r.instructor.wait.characters.max);
            buffer.WriteInt(r.instructor.wait.fileSaves.min);
            buffer.WriteInt(r.instructor.wait.fileSaves.max);
            buffer.WriteInt(r.instructor.wait.fileOpens.min);
            buffer.WriteInt(r.instructor.wait.fileOpens.max);
            buffer.WriteInt(r.instructor.wait.commands.min);
            buffer.WriteInt(r.instructor.wait.commands.max);

            buffer.WriteInt(r.instructor.observe.time.min);
            buffer.WriteInt(r.instructor.observe.time.max);
            buffer.WriteInt(r.instructor.observe.wordSeparators.Length);
            foreach (InstructorObserveWordSeparators wordSeparators in r.instructor.observe.wordSeparators)
            {
                buffer.WriteString(wordSeparators.extension);
                buffer.WriteInt(wordSeparators.separators.Length);
                foreach (char c in wordSeparators.separators)
                {
                    buffer.WriteString(c.ToString());
                }
            }

            buffer.WriteInt(r.instructor.ask.templates.Length);
            foreach (QuestionTemplateData template in r.instructor.ask.templates)
            {
                buffer.WriteInt((int)template.type);
                buffer.WriteString(template.question);
                buffer.WriteInt(template.contextLines);
                buffer.WriteInt(template.contextWords);
                buffer.WriteInt(template.timeLimit);
            }

            buffer.WriteInt(r.instructor.ask.chances.Length);
            foreach (QuestionChanceData chance in r.instructor.ask.chances)
            {
                buffer.WriteInt((int)chance.type);
                buffer.WriteInt(chance.weight);
            }

            result.data = buffer.GetBytes();

            return result;
        }

        public static NewHeaderData DecodeNewHeaderRecord(Story.Record r)
        {
            NewHeaderData result = new();

            BinaryBufferReader buffer = new(r.data);

            result.title = buffer.ReadString();
            result.student = buffer.ReadString();
            result.guid = buffer.ReadString();
            int extensionsCount = buffer.ReadInt();
            result.editableExtensions = new string[extensionsCount];
            for(int i = 0; i < extensionsCount; i++) { result.editableExtensions[i] = buffer.ReadString(); }

            result.instructor.enabled = (buffer.ReadInt() != 0);

            result.instructor.wait.time.min = buffer.ReadInt();
            result.instructor.wait.time.max = buffer.ReadInt();
            result.instructor.wait.characters.min = buffer.ReadInt();
            result.instructor.wait.characters.max = buffer.ReadInt();
            result.instructor.wait.fileSaves.min = buffer.ReadInt();
            result.instructor.wait.fileSaves.max = buffer.ReadInt();
            result.instructor.wait.fileOpens.min = buffer.ReadInt();
            result.instructor.wait.fileOpens.max = buffer.ReadInt();
            result.instructor.wait.commands.min = buffer.ReadInt();
            result.instructor.wait.commands.max = buffer.ReadInt();

            result.instructor.observe.time.min = buffer.ReadInt();
            result.instructor.observe.time.max = buffer.ReadInt();
            int wordSeparatorsCount = buffer.ReadInt();
            result.instructor.observe.wordSeparators = new InstructorObserveWordSeparators[wordSeparatorsCount];
            for(int i = 0; i < wordSeparatorsCount; i++)
            {
                InstructorObserveWordSeparators wordSeparator = new();
                wordSeparator.extension = buffer.ReadString();

                int separatorsCount = buffer.ReadInt();
                wordSeparator.separators = new char[separatorsCount];
                for(int j = 0; j < separatorsCount; j++)
                {
                    wordSeparator.separators[j] = buffer.ReadString()[0];
                }

                result.instructor.observe.wordSeparators[i] = wordSeparator;
            }

            int templatesCount = buffer.ReadInt();
            result.instructor.ask.templates = new QuestionTemplateData[templatesCount];

            for(int i = 0; i < templatesCount; i++)
            {
                QuestionTemplateData template = new();

                template.type = (QuestionType)buffer.ReadInt();
                template.question = buffer.ReadString();
                template.contextLines = buffer.ReadInt();
                template.contextWords = buffer.ReadInt();
                template.timeLimit = buffer.ReadInt();

                result.instructor.ask.templates[i] = template;
            }

            int chancesCount = buffer.ReadInt();
            result.instructor.ask.chances = new QuestionChanceData[chancesCount];
            for(int i = 0; i < chancesCount; i++)
            {
                QuestionChanceData chance;

                chance.type = (QuestionType)buffer.ReadInt();
                chance.weight = buffer.ReadInt();

                result.instructor.ask.chances[i] = chance;
            }

            return result;
        }

    }
}
