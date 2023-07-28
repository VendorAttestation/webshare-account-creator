using System.Security.Cryptography;

public static class RandomExtensions
{
    private static string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string NextString(this Random random, int length)
    {
        var stringChars = new char[length];

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[bytes[i] % chars.Length];
        }

        return new String(stringChars);
    }
}