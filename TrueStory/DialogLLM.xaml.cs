using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;

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
            set { apiKey = value; UpdateKeyText(apiKey); }
        }

        public string ApiURL
        {
            get { return apiURL; }
            set { apiURL = value; UpdateURLText(apiURL); }
        }

        public string? InitialUserLine
        {
            set { initialUserLine = value; }
        }

        public RecordData.AssistantData Parameters { get; set; }

        string? initialUserLine;

        public RecordData.LanguageModelChatData Chat
        {
            get
            {
                RecordData.LanguageModelChatData result = new();

                List<RecordData.LanguageModelChatLineData> lines = new();

                foreach (ChatLine line in chatHistory)
                {
                    if (!line.isError)
                    {
                        lines.Add(new() { isUser = line.isUser, text = line.text });
                    }
                }

                result.lines = lines.ToArray();

                return result;
            }
        }

        public int TextSize
        {
            set
            {
                AnswerText.FontSize = value;
                InputText.FontSize = value;

            }
        }

        List<ChatLine> chatHistory;

        StringBuilder builder;

        string apiKey = "";
        string apiURL = "";

        public DialogLLM()
        {
            InitializeComponent();

            WaitBar.IsIndeterminate = false;
            WaitBar.Value = 0;

            Loaded += OnLoaded;
            InputText.PreviewKeyDown += OnPreviewKeyDown;

            StatusLabel.Content = "";

            chatHistory = new();
            builder = new();
        }

        private async void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool isControlPressed = ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);

            if (e.Key == Key.Return && !isControlPressed)
            {
                int previousCaret = InputText.CaretIndex;

                InputText.Text = InputText.Text.Insert(InputText.CaretIndex, Environment.NewLine);
                InputText.CaretIndex = previousCaret + Environment.NewLine.Length;

                e.Handled = true;
            }
            else if (e.Key == Key.Return && isControlPressed)
            {
                bool canSend = CheckCanSend();

                if (canSend)
                {
                    LockUI();
                    await SendLine(InputText.Text);
                    UnlockUI();
                    InputText.Focus();
                }

                e.Handled = true;
            }

        }

        void LockUI()
        {
            InputText.IsEnabled = false;
            WaitBar.IsIndeterminate = true;
        }

        void UnlockUI()
        {
            WaitBar.IsIndeterminate = false;
            InputText.IsEnabled = true;
        }

        bool CheckCanSend()
        {
            bool result = true;

            if (apiKey.Trim().Length == 0)
            {
                MessageBox.Show("Tienes que definir una clave de API Gemini para hacer consultas", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                result = false;
            }
            else if (apiURL.Trim().Length == 0)
            {
                MessageBox.Show("Tienes que definir una URL de API Gemini para hacer consultas", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                result = false;
            }

            return result;
        }

        async private Task SendLine(string userLine)
        {

            //userLine = userLine.Replace("\"", "\\\"");

            ChatLine lineInput = new() { isUser = true, text = userLine };

            bool success;
            string errorText = "";
            int retries = 0;

            do
            {
                if (retries > 0)
                {
                    int seconds = (int)Math.Pow(Constants.languageModelBaseWait, retries);
                    StatusLabel.Content = "<esperando " + seconds + " segundos (reintento " + retries + " de " + Constants.languageModelMaxRetries + ")>";
                    await Task.Delay(seconds * 1000);
                }

                StatusLabel.Content = "<esperando respuesta>";

                chatHistory.Add(lineInput);

                ChatLine lineOutput = await SendHistory(chatHistory, apiKey);

                if (!lineOutput.isError)
                {
                    chatHistory.Add(lineOutput);

                    builder.Clear();

                    foreach (ChatLine line in chatHistory)
                    {
                        builder.Append((line.isUser ? "Estudiante: " : Parameters.name + ": ") + line.text);
                        builder.AppendLine();
                        builder.AppendLine();
                    }

                    AnswerText.Text = builder.ToString();
                    AnswerText.ScrollToEnd();

                    InputText.Clear();

                    success = true;

                }
                else
                {
                    chatHistory.RemoveAt(chatHistory.Count - 1);

                    errorText = lineOutput.errorText;

                    retries++;
                    success = false;

                }
            } while (!success && retries < Constants.languageModelMaxRetries);

            if (!success)
            {
                MessageBox.Show("No se ha podido enviar la consulta. Error: " + errorText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            StatusLabel.Content = "";

        }

        private async Task<ChatLine> SendHistory(List<ChatLine> chatHistory, string apiKey)
        {
            ChatLine result;

            JsonObject root = new JsonObject();
            JsonArray contents = new JsonArray();

            List<ChatLine> validLines = chatHistory.FindAll(l => !l.isError);

            foreach (ChatLine line in validLines)
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

            JsonObject instruction = new JsonObject();

            JsonArray instructionParts = new JsonArray();
            JsonObject instructionPart = new JsonObject();

            instructionPart.Add("text", Parameters.instructions);
            instructionParts.Add(instructionPart);

            instruction.Add("parts", instructionParts);

            root.Add("systemInstruction", instruction);

            string query = root.ToJsonString();

            Console.WriteLine(query);
            File.WriteAllText("Query.txt", query);

            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, apiURL);
            request.Headers.Add("x-goog-api-key", apiKey);
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
                    if (parts?.GetType() == typeof(JsonArray))
                    {
                        JsonArray partsArray = parts.AsArray();
                        for (int i = 0; i < partsArray.Count; i++)
                        { builder.Append(partsArray[i]?["text"]?.ToString() ?? ""); }
                    }
                    answerText = builder.ToString().Replace("\n", "\r\n");

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
            catch (HttpRequestException httpException)
            {
                isError = true;
                errorText = httpException.HttpRequestError.ToString();
                requestSuccess = false;
            }
            catch (TaskCanceledException taskCancelledException)
            {
                isError = true;
                errorText = taskCancelledException.Message.ToString();
                requestSuccess = false;
            }

            result = new() { isUser = false, text = answerText, isError = isError, errorText = errorText };

            return result;

        }

        async private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (initialUserLine != null)
            {
                InputText.Text = initialUserLine;

                bool canSend = CheckCanSend();

                if (canSend)
                {
                    LockUI();
                    await SendLine(initialUserLine);
                    UnlockUI();
                }
            }

            InputText.Focus();
        }

        private void OnSetApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogAskName d = new();

            d.Title = "Introducir clave de API";
            d.Message.Text = "Introduce la clave de API Gemini que quieras utilizar para las consultas.";
            d.Input.Text = apiKey;

            bool? r = d.ShowDialog();

            if (r.GetValueOrDefault())
            {
                apiKey = d.Input.Text.Trim();
                UpdateKeyText(apiKey);
            }

        }


        private void OnSetApiURLButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogAskName d = new();

            d.Title = "Introducir URL de API";
            d.Message.Text = "Introduce la URL de API Gemini que quieras utilizar para las consultas.";
            d.Input.Text = apiURL;

            bool? r = d.ShowDialog();

            if (r.GetValueOrDefault())
            {
                apiURL = d.Input.Text.Trim();
                UpdateURLText(apiURL);
            }

        }

        void UpdateKeyText(string key)
        {
            if (key.Trim().Length <= 0) { ApiKeyInputText.Text = "<sin clave> "; }
            else
            {
                string asterisks = "";
                for (int i = 0; i < key.Length; i++)
                {
                    asterisks = asterisks + (key.Length - i < 4 ? key[i] : "*");
                }

                ApiKeyInputText.Text = asterisks;
            }

        }

        void UpdateURLText(string url)
        {
            if (url.Trim().Length <= 0) { ApiKeyInputText.Text = "<sin url> "; }
            else
            {
                URLInputText.Text = url.Trim();
            }

        }

        private void OnCloseButtonPressed(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
