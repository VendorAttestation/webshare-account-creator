using System.Text.Json;
using System.Text;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient httpClient, string requestUri, object content)
    {
        var jsonPayload = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        return await httpClient.PostAsync(requestUri, jsonPayload);
    }
}