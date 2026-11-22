using System.Text;
using System.Windows;
using TrueStory;

namespace StoryPlayer
{
    /// <summary>
    /// Lógica de interacción para LLMWindow.xaml
    /// </summary>
    public partial class LLMWindow : Window
    {
        StringBuilder builder = new();

        public RecordData.LanguageModelChatData Chat
        {
            set
            {
                builder.Clear();

                foreach (RecordData.LanguageModelChatLineData line in value.lines)
                {
                    builder.Append((line.isUser ? "Alumno" : "IA") + ": " + line.text);
                    builder.AppendLine();
                    builder.AppendLine();
                }

                ChatText.Text = builder.ToString();
            }
        }

        public LLMWindow()
        {
            InitializeComponent();
        }
    }
}
