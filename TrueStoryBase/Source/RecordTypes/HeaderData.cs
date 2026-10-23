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
                min = 60 * 2,
                max = 60 * 5
            };

            public IntRangeData characters = new()
            {
                min = 30 * 4,
                max = 30 * 8
            };

            public IntRangeData fileOpens = new()
            {
                min = 1,
                max = 2
            };

            public IntRangeData fileSaves = new()
            {
                min = 1,
                max = 2
            };

            public IntRangeData commands = new()
            {
                min = 2,
                max = 5
            };

            public InstructorWaitData() { }

        }

        public struct InstructorObserveWordSeparators
        {
            public string[] extensions = new string[] { "txt" };
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
                    extensions = new string[] {"txt"},
                    separators = new char[] {'.',',','-','(',')','\'' }

                },
                new ()
                {
                    extensions = new string[] { "h", "c", "hpp", "cpp" },
                    separators = new char[] { ';', '(', ')', '[', ']', '"', '\'', '.', ',' }
                }
            };

            public InstructorObserveData() { }
        }

        public struct QuestionTemplateData
        {
            public RecordData.QuestionType type = RecordData.QuestionType.time;
            public string question = "";
            public int contextLines = 0;
            public int contextWords = 0;
            public int timeLimit = 0;

            public QuestionTemplateData() { }
        }

        public struct InstructorAskQuestionData
        {
            public QuestionTemplateData[] templates = new QuestionTemplateData[0];

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

        public struct FolderViewData
        {
            public string[] allowedExtensions = new string[]
            {
                "txt", "csv", "md",
                "htm", "html", "css", "js",
                "php",
                "tsx",
                "c", "h",
                "hpp", "cpp",
                "cs",
                "java"
            };


            public FolderViewData() { }
        }


        public struct NewHeaderData
        {
            public string title = "";
            public InstructorData instructor = new();

            public NewHeaderData() { }
        }


        // Old structures

        public struct HeaderData
        {
            public string guid;
            public string publicKey;
            public string author;
        }

        public static HeaderData DecodeHeaderRecord(Story.Record r)
        {
            HeaderData data = new();

            BinaryBufferReader buffer = new(r.data);

            data.guid = buffer.ReadString();
            data.publicKey = buffer.ReadString();
            data.author = buffer.ReadString();

            return data;
        }


    }
}
