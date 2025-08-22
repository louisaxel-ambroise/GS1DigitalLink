using System.Text;
using System.Text.RegularExpressions;
using static GS1DigitalLink.Utils.ApplicationIdentifier;

namespace GS1DigitalLink.Utils;

public static class Extensions
{
    internal static bool IsNumeric(this byte value)
    {
        return value >> 4 <= 9 && (value & 0x0F) <= 9;
    }

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

    private static Encodings GetEncoding(Charset charset, BitStream stream)
    {
        static Encodings GetCharsetFromBuffer(BitStream stream)
        {
            stream.Buffer(3);
            var encodingIndex = Convert.ToInt32(stream.Current.ToString(), 2);

            return Encodings.Values.ElementAt(encodingIndex);
        }

        return charset switch
        {
            Charset.Numeric => Encodings.Numeric,
            Charset.Alpha => GetCharsetFromBuffer(stream),
            _ => throw new Exception("Unknown charset")
        };
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