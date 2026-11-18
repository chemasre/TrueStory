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
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrueStory
{
    /// <summary>
    /// Lógica de interacción para DialogLLM.xaml
    /// </summary>
    public partial class DialogLLM : Window
    {
        struct ChatLine
        {
            public bool isUser;
            public string text;
            public bool isError;
            public string errorText;
        }

        public string ApiKey
        {
            get { return apiKey; } 
            set { apiKey = value; DoSetKeyAsterisks(apiKey); }
        }

        public string URL { get; set; }

        public RecordData.LanguageModelChatData Chat
        {
            get
            {
                RecordData.LanguageModelChatData result = new();

                List<RecordData.LanguageModelChatLineData> lines = new();

                foreach(ChatLine line in chatHistory)
                {
                    if(!line.isError)
                    {
                        lines.Add(new() { isUser = line.isUser, text = line.text});
                    }
                }

                result.lines = lines.ToArray();

                return result;
            }
        }

        List<ChatLine> chatHistory;

        StringBuilder builder;

        string apiKey = "";

        public DialogLLM()
        {
            InitializeComponent();

            WaitBar.IsIndeterminate = false;
            WaitBar.Value = 0;

            Loaded += OnLoaded;
            InputText.PreviewKeyDown += OnPreviewKeyDown;

            chatHistory = new();
            builder = new();
        }

        private async void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool isControlPressed = ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);

            if(e.Key == Key.Return && !isControlPressed)
            {
                int previousCaret = InputText.CaretIndex;

                InputText.Text = InputText.Text.Insert(InputText.CaretIndex, Environment.NewLine);
                InputText.CaretIndex = previousCaret + Environment.NewLine.Length;

                e.Handled = true;
            }
            else if(e.Key == Key.Return && isControlPressed)
            {
                InputText.IsEnabled = false;

                string userInput = InputText.Text;

                WaitBar.IsIndeterminate = true;

                userInput = userInput.Replace("\"", "\\\"");

                ChatLine lineInput = new() { isUser = true, text = userInput };

                chatHistory.Add(lineInput);

                ChatLine lineOutput = await LLMAsk(chatHistory, apiKey);

                if(!lineOutput.isError)
                {
                    chatHistory.Add(lineOutput);

                    builder.Clear();

                    foreach(ChatLine line in chatHistory)
                    {
                        builder.Append((line.isUser ? "User: " : "Model: ") + line.text);
                        builder.AppendLine();
                        builder.AppendLine();
                    }

                    AnswerText.Text = builder.ToString();
                    AnswerText.ScrollToEnd();

                    InputText.Clear();

                    Console.WriteLine(AnswerText.Text);
                }
                else
                {
                    MessageBox.Show("Se ha producido un error: " + lineOutput.errorText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                WaitBar.IsIndeterminate = false;

                InputText.IsEnabled = true;
                InputText.Focus();

                e.Handled = true;
            }

        }

        private async Task<ChatLine> LLMAsk(List<ChatLine> chatHistory, string apiKey)
        {
            ChatLine result;


            JsonObject root = new JsonObject();
            JsonArray contents = new JsonArray();

            List<ChatLine> validLines = chatHistory.FindAll(l  => !l.isError);
            int lineCount = 0;

            foreach(ChatLine line in validLines)
            {
                JsonObject lineObject = new JsonObject();
                lineObject.Add("role", line.isUser ? "user" : "model");

                JsonArray partsArray = new JsonArray();
                JsonObject partObject = new JsonObject();
                partObject.Add("text", line.text);

                partsArray.Add(partObject);
                lineObject.Add("parts", partsArray);

                contents.Add(lineObject);

            }

            root.Add("contents", contents);

            string query = root.ToJsonString();

            Console.WriteLine(query);
            File.WriteAllText("Query.txt", query);

            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, URL);
            request.Headers.Add("x-goog-api-key",apiKey);
            request.Content = new StringContent(query, Encoding.UTF8, "application/json");

            client.Timeout = new TimeSpan(0, 0, 200);

            bool requestSuccess = false;
            string responseText = "";
            bool isError = false;
            string errorText = "";
            string answerText = "";

            try
            {
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    responseText = (await response.Content.ReadAsStringAsync());

                    JsonNode? node = JsonNode.Parse(responseText);

                    builder.Clear();

                    JsonNode? parts = node?["candidates"]?[0]?["content"]?["parts"];
                    if(parts?.GetType() == typeof(JsonArray))
                    {
                        JsonArray partsArray = parts.AsArray();
                        for(int i = 0; i < partsArray.Count; i ++)
                        { builder.Append(partsArray[i]?["text"]?.ToString() ?? ""); }
                    }
                    answerText = builder.ToString().Replace("\n","\r\n");
                    
                    File.WriteAllText("Answer.txt", responseText);

                    requestSuccess = true;
                }
                else
                {
                    isError = true;
                    errorText = $"Error: {response.StatusCode}\n{response.ReasonPhrase}\n";
                    requestSuccess = false;
                }

            }
            catch(HttpRequestException httpException)
            {
                isError = true;
                errorText = httpException.HttpRequestError.ToString();
                requestSuccess = false;
            }
            catch(TaskCanceledException taskCancelledException)
            {
                isError = true;
                errorText = taskCancelledException.Message.ToString();
                requestSuccess = false;
            }

            result = new() { isUser = false, text = answerText, isError = isError, errorText = errorText };

            return result;

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InputText.Focus();
        }

        private void OnSetApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogAskName d = new();

            d.Title = "Introducir clave de API";
            d.Message.Text = "Introduce la clave de API Gemini que quieras utilizar para las consultas.";
            d.Input.Text = "API_KEY";
            
            bool? r = d.ShowDialog();

            if(r.GetValueOrDefault())
            {
                apiKey = d.Input.Text.Trim();
                DoSetKeyAsterisks(apiKey);
            }
                
        }

        void DoSetKeyAsterisks(string key)
        {
            string asterisks = "";
            for(int i = 0; i < key.Length; i ++)
            {
                asterisks = asterisks + (key.Length - i < 4 ? key[i] : "*");
            }

            ApiKeyInputText.Text = asterisks;
        }
    }
}
