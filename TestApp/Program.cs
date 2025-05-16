using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

var requestData = new
{
    model = "deepseek/deepseek-chat:free",
    messages = new[]
    {
        new { role = "user", content = "Write calc in c#" }
    }
};

string API_KEY = "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";
string API_URL = "https://openrouter.ai/api/v1/chat/completions";

var httpRequest = new HttpRequestMessage
{
    Method = HttpMethod.Post,
    RequestUri = new Uri(API_URL),
    Headers = {
        Authorization = AuthenticationHeaderValue.Parse($"Bearer {API_KEY}"),
        Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json")}
    },
    Content = new StringContent(JsonSerializer.Serialize(requestData))
    {
        Headers = { ContentType = MediaTypeHeaderValue.Parse("application/json") }
    }
};

// По возможности httpClient нужно переиспользовать. Инициализируй 1 раз, сделай побольше запросов, а в конце Dispose()
using var httpClient = new HttpClient();

var response = await httpClient.SendAsync(httpRequest);
// Дальше работаем с response так, как нам нужно
var contentString = await response.Content.ReadAsStringAsync();
//Console.WriteLine(contentString);

// Вытаскиваем оттуда сообщение
JsonNode rootNode = JsonNode.Parse(contentString);
string messageContent = rootNode["choices"][0]["message"]["content"].ToString();

Console.WriteLine(messageContent);