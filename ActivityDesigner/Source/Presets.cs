using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrueStory;
using static TrueStory.RecordData;

namespace ActivityDesigner
{
    public class Presets
    {
        public class QuestionFrequency
        {
            public string Title { get; set; } = "";
            public RecordData.IntRangeData WaitTime { get; set; }
            public RecordData.IntRangeData WaitCharacters { get; set; }
            public RecordData.IntRangeData WaitFileOpens { get; set; }
            public RecordData.IntRangeData WaitFileSaves { get; set; }
            public RecordData.IntRangeData WaitCommands { get; set; }
            public RecordData.IntRangeData ObserveTime { get; set; }

        }

        public class QuestionTemplates
        {
            public string Title { get; set; } = "";
            public QuestionTemplateData[] Templates { get; set; } = new QuestionTemplateData[0];
        }

        public class QuestionChances
        {
            public string Title { get; set; } = "";
            public QuestionChanceData[] Chances { get; set; } = new QuestionChanceData[0];
        }

        public class QuestionWordSeparators
        {
            public string Title { get; set; } = "";
            public InstructorObserveWordSeparators[] WordSeparators { get; set; } = new InstructorObserveWordSeparators[0];
        }

    }
}
