using GS1DigitalLink.Utils;
using System.Text;

namespace GS1DigitalLink.Services.Algorithms;

public class ComponentFormatter
{
    public static string Format(ApplicationIdentifier.AIComponent component, string value)
    {
        return component.Charset switch
        {
            Charset.Numeric => FormatNumeric(component, value),
            Charset.Alpha => FormatAlpha(component, value),
            _ => throw new Exception("Unknown charset")
        };
    }

    public static string FormatNumeric(ApplicationIdentifier.AIComponent component, string value)
    {
        if (component.FixedLength)
        {
            var componentValue = value[..component.Length];
            var c = Convert.ToString(Convert.ToInt64(componentValue, 10), 2);
            var expectedLength = (int)Math.Ceiling(component.Length * Math.Log(10) / Math.Log(2) + 0.01);

            return c.PadLeft(expectedLength, '0');
        }
        else
        {
            var c = Convert.ToString(Convert.ToInt32(value, 10), 2);
            var lengthSize = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2) + 0.01);
            var l2 = Convert.ToString(value.Length, 2).PadLeft(lengthSize);
            var nl = (int)Math.Ceiling(lengthSize * Math.Log(10) / Math.Log(2) + 0.01);

            return l2 + c.PadLeft(nl, '0');
        }
    }

    public static string FormatAlpha(ApplicationIdentifier.AIComponent component, string value)
    {
        var prefix = "";

        if (component.FixedLength)
        {
            value = value[..component.Length];
        }
        else
        {
            var nli = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2) + 0.01);
            prefix = Convert.ToString(value.Length, 2).PadLeft(nli, '0');

        }

        return FormatHex(value, prefix);
    }

    public static string FormatHex(string componentValue, string? prefix = null)
    {
        if (componentValue.IsNumeric())
        {
            var nv = (int)Math.Ceiling(componentValue.Length * Math.Log(10) / Math.Log(2) + 0.01);

            return $"000{prefix}{Convert.ToString(Convert.ToInt64(componentValue, 10), 2).PadLeft(nv, '0')}";
        }
        else if (componentValue.IsLowerCaseHex())
        {
            return $"001{prefix}{Alphabets.GetAlphaBinary(componentValue)}";
        }
        else if (componentValue.IsUpperCaseHex())
        {
            return $"010{prefix}{Alphabets.GetAlphaBinary(componentValue)}";
        }
        else if (componentValue.IsUriSafeBase64())
        {
            return $"011{prefix}{Alphabets.GetBase64Binary(componentValue)}";
        }
        else
        {
            return $"100{prefix}{string.Concat(Encoding.ASCII.GetBytes(componentValue).Select(x => Convert.ToString(x, 2).PadLeft(7, '0')))}";
        }
    }
}