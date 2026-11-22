using System.IO;
using System.Text;
using System.Text.Json;
using TrueStory;
using static TrueStory.RecordData;

namespace ActivityDesigner
{
    public class Profiles
    {
        public class IntRange
        {
            public int Min { get; set; } = 0;
            public int Max { get; set; } = 0;

            public IntRange() { }
            public IntRange(RecordData.IntRangeData data)
            {
                Min = data.min;
                Max = data.max;
            }

            public RecordData.IntRangeData ToData()
            {
                return new() { min = Min, max = Max };
            }
        }

        public class QuestionFrequency
        {
            public IntRange WaitTime { get; set; } = new();
            public IntRange WaitCharacters { get; set; } = new();
            public IntRange WaitFileOpens { get; set; } = new();
            public IntRange WaitFileSaves { get; set; } = new();
            public IntRange WaitCommands { get; set; } = new();
            public IntRange ObserveTime { get; set; } = new();

        }

        public class QuestionTemplates
        {
            public QuestionTemplateData[] Templates { get; set; } = new QuestionTemplateData[0];
        }

        public class QuestionChances
        {
            public QuestionChanceData[] Chances { get; set; } = new QuestionChanceData[0];
        }

        public class ExtensionSettings
        {
            public string[] EditableExtensions { get; set; } = new string[0];
            public InstructorObserveWordSeparators[] WordSeparators { get; set; } = new InstructorObserveWordSeparators[0];
        }

        public class AssistantParams
        {
            public string Name { get; set; } = "";
            public string Instructions { get; set; } = "";
            public ContextualQueryData ContextualQuery1 { get; set; } = new();
            public ContextualQueryData ContextualQuery2 { get; set; } = new();
        }

        public static AssistantParams? LoadAssistantParams(string fileName)
        {
            AssistantParams? result;
            string text = File.ReadAllText(fileName);
            result = JsonSerializer.Deserialize<AssistantParams>(text);

            return result;
        }

        public static void SaveAssistantParams(AssistantParams profile, string fileName)
        {
            JsonSerializerOptions options = new() { WriteIndented = true };
            string text = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(fileName, text, Encoding.UTF8);
        }

        public static QuestionFrequency? LoadQuestionFrequency(string fileName)
        {
            QuestionFrequency? result;
            string text = File.ReadAllText(fileName);
            result = JsonSerializer.Deserialize<QuestionFrequency>(text);

            return result;
        }

        public static void SaveQuestionFrequency(QuestionFrequency profile, string fileName)
        {
            JsonSerializerOptions options = new() { WriteIndented = true };
            string text = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(fileName, text, Encoding.UTF8);
        }

        public static QuestionTemplates? LoadQuestionTemplates(string fileName)
        {
            QuestionTemplates? result;
            string text = File.ReadAllText(fileName);
            JsonSerializerOptions options = new() { IncludeFields = true };
            result = JsonSerializer.Deserialize<QuestionTemplates>(text, options);

            return result;
        }

        public static void SaveQuestionTemplates(QuestionTemplates profile, string fileName)
        {
            JsonSerializerOptions options = new() { WriteIndented = true, IncludeFields = true };
            string text = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(fileName, text, Encoding.UTF8);
        }

        public static QuestionChances? LoadQuestionChances(string fileName)
        {
            QuestionChances? result;
            string text = File.ReadAllText(fileName);
            JsonSerializerOptions options = new() { IncludeFields = true };
            result = JsonSerializer.Deserialize<QuestionChances>(text, options);

            return result;
        }

        public static void SaveQuestionChances(QuestionChances profile, string fileName)
        {
            JsonSerializerOptions options = new() { WriteIndented = true, IncludeFields = true };
            string text = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(fileName, text, Encoding.UTF8);
        }

        public static ExtensionSettings? LoadExtensionSettings(string fileName)
        {
            ExtensionSettings? result;

            string text = File.ReadAllText(fileName);
            JsonSerializerOptions options = new() { IncludeFields = true };
            result = JsonSerializer.Deserialize<ExtensionSettings>(text, options);

            return result;
        }

        public static void SaveExtensionSettings(ExtensionSettings profile, string fileName)
        {
            JsonSerializerOptions options = new() { WriteIndented = true, IncludeFields = true };
            string text = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(fileName, text, Encoding.UTF8);
        }
    }
}
