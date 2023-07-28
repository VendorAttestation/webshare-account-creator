public static class StreamWriterExtensions
{
    public static async Task WriteLineAsync(this StreamWriter writer, string value)
    {
        await writer.WriteLineAsync(value);
    }
}