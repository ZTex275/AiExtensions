using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using Markdig;
using Markdown.ColorCode;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using mshtml;

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
        private string AiModel;
        private DateTime lastTimeExecution;

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
#if !DEBUG
               System.Windows.Forms.MessageBox.Show("Введите текст!");
#else
                DebugMessage();
#endif
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
                model = AiModel,
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

                string messageContent;

                if (rootNode["choices"] != null) // Проверка на ошибку с хоста
                    messageContent = rootNode["choices"][0]["message"]["content"].ToString();
                else
                    messageContent = rootNode["error"]["metadata"]["raw"].ToString();
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
            string mainJs = LoadResource("main.js");

            string htmlText =
                $"<html>" +
                $"<script>{mainJs}</script>" +
                $"<head>" +
                $"<meta charset=\"UTF-8\">" +
                $"<style>{codedarkCss}</style>" +
                $"</head>" +
                $"<body>" +
                $"{oldMarkdownHtml}" +
                $"{markdownHtml}" +
                $"</body>" +
                $"</html>";

            Dispatcher.Invoke((Action)(() =>
            {
                webBrowser.NavigateToString(htmlText);
                webBrowser.LoadCompleted += (s, e) =>
                {
                    webBrowser.InvokeScript("scrollToEnd");
                    //webBrowser.InvokeScript("ctrlC");

                    try
                    {
                        var doc = webBrowser.Document as HTMLDocument;
                        if (doc != null)
                        {
                            var iEvent = (HTMLDocumentEvents2_Event)doc;
                            if (iEvent != null)
                            {
                                iEvent.onkeypress += new HTMLDocumentEvents2_onkeypressEventHandler(CopySelectedTextFromWindow);
                            }
                        }
                    }
                    catch { }
                };
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
            AiModel = config["ApiSettings:AiModel"];
        }

        private bool CopySelectedTextFromWindow(IHTMLEventObj pEvtObj)
        {
            if (pEvtObj.ctrlKey && pEvtObj.keyCode == 3) // Ctrl + C
            {
                if (DateTime.Now - lastTimeExecution >= TimeSpan.FromSeconds(3)) // Ставим ограничение на повторный заход
                {
                    var doc = webBrowser.Document as HTMLDocument;
                    if (doc != null)
                    {
                        var currentSelection = doc.selection;
                        if (currentSelection != null)
                        {
                            dynamic selectionRange = currentSelection.createRange();
                            if (selectionRange != null)
                            {
                                // Получаем выделенный текст в браузере
                                dynamic selectionText = selectionRange.Text;
                                if (!string.IsNullOrEmpty(selectionText))
                                {
                                    // Записываем в буффер обмена
                                    Clipboard.SetText(selectionText);
                                    lastTimeExecution = DateTime.Now;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
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

        private void DebugMessage()
        {
            SetHTML(
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd" +
                "ssssssssssssssssssdasdadasdasddaadads C+++dasdasdddddddddddddddddddddddddddd"
            );
        }
    }
}