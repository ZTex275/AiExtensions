using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AIHelper
{
    /// <summary>
    /// Interaction logic for ToolWindowControl.
    /// </summary>
    public partial class ToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowControl"/> class.
        /// </summary>
        public ToolWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void buttonSendRequest_Click(object sender, RoutedEventArgs e)
        {
            string inputText = textBoxMessage.Text;

            if (inputText != string.Empty)
            {
                textBoxChat.Text += "\n\n" + "Я: " + inputText; // Выводим свое сообщение в окно
                textBoxMessage.Clear(); // Очищаем сообщение
                scrollViewerChat.ScrollToEnd(); // Прокручиваем скролл в самый низ
                Task.Run(async () => await SendRequestAsync(inputText));
            }
            else MessageBox.Show("Введите текст!");
        }

        private async Task SendRequestAsync(string inputText)
        {
            // Подключаем json чтобы получить из него API-ключ
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            string API_KEY = config["ApiSettings:ApiKey"];
            string API_URL = "https://openrouter.ai/api/v1/chat/completions";

            string sendText;
            string getBuffer = GetSelectedText();

            // Если мышкой не выделен буфер, то ничего не добавляем
            if (getBuffer == string.Empty)
                sendText = inputText;
            else
                sendText = inputText + "\n" + getBuffer;

            var requestData = new
            {
                //model = "deepseek/deepseek-v3-base:free",
                model = "deepseek/deepseek-chat:free",
                messages = new[]
                {
                    new { role = "user", content = $"{sendText}" }
                }
            };

            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(API_URL),
                Headers = {
                    Authorization = AuthenticationHeaderValue.Parse($"Bearer {API_KEY}"),
                    Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json")}
                },
                Content = new StringContent(JsonSerializer.Serialize(requestData))
                {
                    Headers = { ContentType = MediaTypeHeaderValue.Parse("application/json") }
                }
            };

            try
            {
                using var httpClient = new HttpClient();

                var response = await httpClient.SendAsync(httpRequest);
                // Дальше работаем с response так, как нам нужно
                var contentString = await response.Content.ReadAsStringAsync();

                // Вытаскиваем оттуда сообщение
                JsonNode rootNode = JsonNode.Parse(contentString);
                string messageContent = rootNode["choices"][0]["message"]["content"].ToString();

                Debug.WriteLine(messageContent);

                await Dispatcher.InvokeAsync((Action)(() =>
                {
                    textBoxChat.AppendText("\n\n" + messageContent);
                }));
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync((Action)(() =>
                {
                    textBoxChat.AppendText("\n\n" + ex.Message);
                }));
            }
        }

        public string GetSelectedText()
        {
            // Получаем сервис текстового менеджера
            var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            // Получаем текущее активное окно
            textManager.GetActiveView(1, null, out IVsTextView textView);

            // Получаем текстовый буфер
            var userData = textView as IVsUserData;
            if (userData == null) return null;

            Guid guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out object holder);
            IWpfTextViewHost host = (IWpfTextViewHost)holder;

            // Получаем выделенный текст
            var selection = host.TextView.Selection;
            if (selection.IsEmpty) return null;

            var selectedSpan = new SnapshotSpan(selection.Start.Position, selection.End.Position);

            Debug.WriteLine(selectedSpan.GetText());
            return selectedSpan.GetText();
        }
    }
}