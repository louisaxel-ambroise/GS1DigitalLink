using System.Text;
using System.Text.RegularExpressions;
using static GS1DigitalLink.Utils.ApplicationIdentifier;

namespace GS1DigitalLink.Utils;

public static class Extensions
{
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T value)
    {
        return source.Where(x => !Equals(x, value));
    }

    public static string ReadFrom(this AIComponent component, BitStream inputStream)
    {
        var encoding = GetEncoding(component.Charset, inputStream);
        var length = GetBitsLength(component, inputStream);

        return encoding.Read(length, inputStream);
    }

    private static int GetBitsLength(AIComponent component, BitStream stream)
    {
        if (component.FixedLength)
        {
            return component.Length;
        }
        else
        {
            var lengthBits = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2));
            stream.Buffer(lengthBits);

            return Convert.ToInt32(stream.Current.ToString(), 2);
        }
    }

    private static Encodings GetEncoding(string charset, BitStream stream)
    {
        if (charset == "N")
        {
            return Encodings.Numeric;
        }
        else
        {
            stream.Buffer(3);

            var encodingIndex = Convert.ToInt32(stream.Current.ToString(), 2);

            return Encodings.Values.ElementAt(encodingIndex);
        }
    }

    public static string ReadFrom(this ApplicationIdentifier identifier, BitStream inputStream)
    {
        var buffer = new StringBuilder();

        foreach (var component in identifier.Components)
        {
            buffer.Append(component.ReadFrom(inputStream));
        }

        return buffer.ToString();
    }

    public static bool Validate(this ApplicationIdentifier identifier, string value)
    {
        return Regex.IsMatch(value, $"^{identifier.Pattern}$");
    }
}