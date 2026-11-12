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
        public string ApiKey
        {
            get { return apiKey; } 
            set { apiKey = value; DoSetKeyAsterisks(apiKey); }
        }

        StringBuilder builder;

        string apiKey = "";

        public DialogLLM()
        {
            InitializeComponent();

            WaitBar.IsIndeterminate = false;
            WaitBar.Value = 0;

            Loaded += OnLoaded;
            InputText.PreviewKeyDown += OnPreviewKeyDown;

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
                string userInput = InputText.Text;

                WaitBar.IsIndeterminate = true;

                string output = await LLMRequest(userInput, apiKey);

                AnswerText.Text = output;

                Console.WriteLine(AnswerText.Text);

                WaitBar.IsIndeterminate = false;

                e.Handled = true;
            }

        }

        private async Task<string> LLMRequest(string userInput, string apiKey)
        {
            userInput = userInput.Replace("\"", "\\\"");

            string query =
                "{" +
                "   \"contents\" : [{" +
                "        \"parts\" : [{" +
                "            \"text\": \"" + userInput + "\"" +
                "        }]" +
                "     }]" +
                "}";

            Console.WriteLine(query);

            
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent");
            request.Headers.Add("x-goog-api-key",apiKey);
            request.Content = new StringContent(query, Encoding.UTF8, "application/json");

            client.Timeout = new TimeSpan(0, 0, 200);

            bool requestSuccess = false;
            string responseText = "";
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
                    errorText = $"Error: {response.StatusCode}\n{response.ReasonPhrase}\n";
                    requestSuccess = false;
                }

            }
            catch(HttpRequestException httpException)
            {
                MessageBox.Show("El servidor no respondió a la petición");
                errorText = httpException.HttpRequestError.ToString();
                requestSuccess = false;
            }
            catch(TaskCanceledException taskCancelledException)
            {
                MessageBox.Show("La petición superó el tiempo de espera máximo");
                errorText = taskCancelledException.Message.ToString();
                requestSuccess = false;
            }

            return requestSuccess ? answerText : errorText;

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
