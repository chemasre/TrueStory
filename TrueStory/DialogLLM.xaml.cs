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
        public DialogLLM()
        {
            InitializeComponent();

            WaitBar.IsIndeterminate = false;
            WaitBar.Value = 0;

            Loaded += OnLoaded;
            InputText.KeyDown += OnKeyDown;
        }

        private async void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                string userInput = InputText.Text;

                WaitBar.IsIndeterminate = true;

                string apiKey = ApiKeyInputText.Text;

                string output = await SendRequestAndGetResponse(userInput, apiKey);

                output = output.Replace("\\n", Environment.NewLine)
                               .Replace("\n", "")
                               .Replace("**", "");

                AnswerText.Text = output;

                Console.WriteLine(AnswerText.Text);

                WaitBar.IsIndeterminate = false;
            }
        }

        private async Task<string> SendRequestAndGetResponse(string userInput, string apiKey)
        {
    //        "model": "gemini-3.8-flash",
    //"input": [{
    //  "type": "user_input",
    //  "content": [{
    //    "type": "text",
    //    "text": "Explain serverless computing in one sentence."
    //  }]

            string query =
                "{" +
                "   \"contents\" : [{" +
                "        \"parts\" : [{" +
                "            \"text\": \"" + userInput + "\"" +
                "        }]" +
                "     }]" +
                "}";
            //"{" + 
            //"   \"contents\" : [{" +
            //"        \"parts\" : [{" +
            //"            \"text\": \"" + userInput + "\"" +
            //"        }]"+
            //"     }],"+
            //"   \"generation_config\": {" +
            //"        \"thinking_level\" : \"medium\"" +
            //"   }" +
            //"}";

            //"{" +
            //    "\"model\": \"gemini-3.8-flash\"," + 
            //    "\"input\": \"" + userInput + "\"" +
            //"}";

            //"\"generation_config\": {" +
            //"\"thinking_level\": \"LOW\"" +
            //"}}";

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
                    answerText = node?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";
                    answerText = answerText.Replace("\n","\r\n");
                    
                    File.WriteAllText("Answer.txt", responseText);

                    //responseText = responseText.Substring(responseText.IndexOf("\"text\": \"") + 9, responseText.IndexOf("\"", responseText.IndexOf("\"text\": \"") + 10) - responseText.IndexOf("\"text\": \"") - 9);
                    requestSuccess = true;
                }
                else
                {
                    errorText = $"Error: {response.StatusCode}\n{response.ReasonPhrase}\n{response.Content.ToString()}";
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
                string key = d.Input.Text.Trim();
                ApiKeyInputText.Text = key;
            }
                
        }
    }
}
