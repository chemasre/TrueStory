using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueStory
{
    public struct IntRange
    {
        public int min;
        public int max;
    }

    public struct QuestionChance
    {
        public Story.QuestionType type;
        public int weight;
    }

    public struct WaitConfiguration
    {
        public IntRange time;
        public IntRange characters;
        public IntRange fileOpens;
        public IntRange fileSaves;
        public IntRange commands;

    }

    public struct ObserveWordSeparators
    {
        public string[] Extensions;
        public char[] Separators;
    }

    public struct ObserveConfiguration
    {
        public IntRange time;
        public ObserveWordSeparators[] wordSeparators;
    }

    public struct QuestionTemplate
    {
        public Story.QuestionType type;
        public string question;
        public int contextLines;
        public int contextWords;
        public int timeLimit;
    }

    public struct AskConfiguration
    {
        public QuestionTemplate[] templates;
        public QuestionChance[] chances;
    }

    public struct InstructorConfiguration
    {
        public WaitConfiguration wait;
        public ObserveConfiguration observe;
        public AskConfiguration ask;
    }
}
