using System.Text.Json;
using System.Text;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient httpClient, string requestUri, object content)
    {
        var jsonPayload = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        return await httpClient.PostAsync(requestUri, jsonPayload);
    }

    public static async Task<Dictionary<string, object>?> GetJsonAsync(this HttpClient httpClient, string requestUri)
    {
        var response = await httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var respBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, object>?>(
            respBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }
}