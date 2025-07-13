using System.Text;

namespace GS1DigitalLink.Utils;

public static class Characters
{
    public static char GetChar(ReadOnlySpan<char> input) => Base64UrlSafe.ElementAt(Convert.ToInt32(input.ToString(), 2));
    public static char GetAlpha(ReadOnlySpan<char> input) => Alpha.ElementAt(Convert.ToInt32(input.ToString(), 2));
    public static string GetBinary(char input) => Convert.ToString(Base64UrlSafe.IndexOf(input, StringComparison.Ordinal), 2).PadLeft(6, '0');
    public static string GetAlphaBinary(char input) => Convert.ToString(Alpha.IndexOf(input, StringComparison.OrdinalIgnoreCase), 2).PadLeft(4, '0');

    private static readonly string Alpha = "0123456789ABCDEF";
    private static readonly string Base64UrlSafe = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    public static StringBuilder GetChars(this StringBuilder builder)
    {
        var parsed = new StringBuilder(builder.Length / 6);
        var buffer = new StringBuilder(6);

        foreach (var chunk in builder.GetChunks())
        {
            foreach (var c in chunk.Span)
            {
                buffer.Append(c);

                if (buffer.Length == 6)
                {
                    parsed.Append(Characters.GetChar(buffer.ToString()));

                    buffer.Clear();
                }
            }
        }

        return parsed;
    }
}
