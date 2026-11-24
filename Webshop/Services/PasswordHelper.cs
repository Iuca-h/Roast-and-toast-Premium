using System.Security.Cryptography;

namespace Webshop.Services;

public static class PasswordHelper
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*?";
    private const string All = Upper + Lower + Digits + Symbols;

    // Generates a random password that always contains one of each category,
    // so it satisfies the default Identity password rules.
    public static string Generate(int length = 16)
    {
        var chars = new List<char>
        {
            Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols)
        };

        while (chars.Count < length)
            chars.Add(Pick(All));

        // Shuffle so the required characters aren't always at the start
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}