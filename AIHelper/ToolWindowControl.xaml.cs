using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Markdig;
using Markdown.ColorCode;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace AIHelper
{
    /// <summary>
    /// Interaction logic for ToolWindowControl.
    /// </summary>
    public partial class ToolWindowControl : System.Windows.Controls.UserControl
    {
        private string oldMarkdownHtml;
        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";
        private string API_KEY;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowControl"/> class.
        /// </summary>
        public ToolWindowControl()
        {
            this.InitializeComponent();

            SetHTML("Введите запрос и нажмите Enter(можно также выделять код)");
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
                SetHTML("Я:" + " " + inputText);
                textBoxMessage.Clear(); // Очищаем сообщение
                //scrollViewerChat.ScrollToEnd(); // Прокручиваем скролл в самый низ,
                Task.Run(async () => await SendRequestAsync(inputText));
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Введите текст!");
            }
        }

        private async Task SendRequestAsync(string inputText)
        {
            // Загружаем API ключ из файла
            LoadJson();

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
                SetHTML(messageContent);

                Debug.WriteLine(messageContent);
            }
            catch (Exception ex)
            {
                SetHTML(ex.Message);
            }
        }

        public string SetHTML(string messageContent)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseColorCode(
                    HtmlFormatterType.Style // use style-based colorization (default)
                    //myCustomStyleDictionary, // use a custom colorization style dictionary
                    //myAdditionalLanguages, // augment the built-in language support
                    //myCustomLanguageId // set a default language ID to fall back to
                )
                .Build();

            string markdownHtml = Markdig.Markdown.ToHtml(messageContent, pipeline); // Для парса markdown в html

            string codedarkCss = LoadResource("codedark.css");

            string htmlText =
                $"<html>" +
                $"<head>" +
                $"<meta charset=\"UTF-8\">" +
                $"<style>{codedarkCss}</style>" +
                $"</head>" +
                //$"<body style=\"background-color: #1e1e1e; color: #dcdcdc; font-size: small; font-family: Arial, monospace;\">" +
                $"<body>" +
                $"{oldMarkdownHtml}" +
                $"{markdownHtml}" +
                $"</body>" +
                $"</html>";

            Dispatcher.Invoke((Action)(() =>
            {
                webBrowser.NavigateToString(htmlText);
            }));

            oldMarkdownHtml += markdownHtml;
            return htmlText;
        }

        private void LoadJson()
        {
#if DEBUG
            // Подключаем json чтобы получить из него API-ключ
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "Resources"))
                .AddJsonFile("appsettings.json")
                .Build();
#else
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Подключаем json чтобы получить из него API-ключ
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(assemblyPath, "Resources"))
                .AddJsonFile("appsettings.json")
                .Build();
#endif

            API_KEY = config["ApiSettings:ApiKey"];
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

        private static string LoadResource(string filename)
        {
#if DEBUG
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", filename);
#else
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var path = Path.Combine(assemblyPath, "Resources", filename);
#endif

            //Загружаем все символы файла, если можем
            return File.Exists(path) ? File.ReadAllText(path) : "";
        }
    }
}