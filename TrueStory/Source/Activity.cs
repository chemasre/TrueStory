using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TrueStory
{
    // Common

    public struct IntRange
    {
        public int min = 0;
        public int max = 0;

        public IntRange() { }
    }

    // Instructor

    public struct InstructorQuestionChance
    {
        public RecordData.QuestionType type = RecordData.QuestionType.time;
        public int weight = 0;

        public InstructorQuestionChance() { }
    }

    public struct InstructorWaitSettings
    {
        public IntRange time = new();
        public IntRange characters = new();
        public IntRange fileOpens = new();
        public IntRange fileSaves = new();
        public IntRange commands = new();

        public InstructorWaitSettings() { }

    }

    public struct InstructorObserveWordSeparators
    {
        public string[] extensions = new string[0];
        public char[] separators = new char[0];

        public InstructorObserveWordSeparators() { }
    }

    public struct InstructorObserveSettings
    {
        public IntRange time = new();
        public InstructorObserveWordSeparators[] wordSeparators = new InstructorObserveWordSeparators[0];

        public InstructorObserveSettings() { }
    }

    public struct InstructorQuestionTemplate
    {
        public RecordData.QuestionType type = RecordData.QuestionType.time;
        public string question = "";
        public int contextLines = 0;
        public int contextWords = 0;
        public int timeLimit = 0;

        public InstructorQuestionTemplate() { }
    }

    public struct InstructorAskQuestionSettings
    {
        public InstructorQuestionTemplate[] templates = new InstructorQuestionTemplate[0];
        public InstructorQuestionChance[] chances = new InstructorQuestionChance[0];

        public InstructorAskQuestionSettings() { }
    }

    public struct InstructorSettings
    {
        public bool enabled = false;
        public InstructorWaitSettings wait = new();
        public InstructorObserveSettings observe = new();
        public InstructorAskQuestionSettings ask = new();

        public InstructorSettings() { }
    }


    public struct ActivitySettings
    {
        string title = "";
        public string[] allowedExtensions = new string[0];
        public InstructorSettings instructor = new();

        public ActivitySettings() { }
    }

    public class Activity
    {
        string uniqueId = Guid.NewGuid().ToString();
        string studentName;

        ActivitySettings Settings { get { return settings; } }
        
        ActivitySettings settings;

        public Activity(string _studentName, ActivitySettings _settings)
        {
            studentName = _studentName;
            settings = _settings;
        }
    }
}
