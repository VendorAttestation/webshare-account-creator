public static class DictionaryExtensions
{
    public static string ExtractStringValue(this Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out object? value) && value is not null)
        {
            return value.ToString() ?? string.Empty;
        }
        else
        {
            throw new Exception($"Failed to extract {key} from response");
        }
    }
}