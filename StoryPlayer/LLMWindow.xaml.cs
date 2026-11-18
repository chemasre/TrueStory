using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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

                foreach(RecordData.LanguageModelChatLineData line in value.lines)
                {
                    builder.Append((line.isUser ? "Alumno" : "IA") + ": " + line.text);
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
