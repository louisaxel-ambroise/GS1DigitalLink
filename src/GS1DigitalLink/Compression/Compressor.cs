using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;
using System.Text;

namespace GS1DigitalLink.Compression;

public sealed class Compressor(GS1DigitalLinkOptions options)
{
    public string CompressPartial(IEnumerable<AI> AIs)
    {
        var buffer = new StringBuilder();
        var aiKeys = AIs.Where(x => options.ApplicationIdentifiers.Find(x.Key)?.IsPrimaryKey ?? false);

        // Do not compress Primary Keys of the DigitalLink
        foreach (var aiKey in aiKeys)
        {
            buffer.Append(aiKey.Key).Append('/').Append(aiKey.Value).Append('/');
        }

        buffer.Append(Compress(AIs.Except(aiKeys)));

        return buffer.ToString();
    }

    public string Compress(IEnumerable<AI> AIs)
    {
        var buffer = new StringBuilder();

        if (options.OptimizationCodes.TryGetBestOptimization(AIs.Select(x => x.Key), out var optimization))
        {
            foreach (var c in optimization.Code)
            {
                buffer.Append(Characters.GetAlphaBinary(c));
            }
            foreach (var ai in optimization.SequenceAIs.Select(x => AIs.Single(a => a.Key == x)))
            {
                FormatApplicationIdentifier(ai, buffer);
            }

            AIs = AIs.Where(x => !optimization.SequenceAIs.Contains(x.Key));
        }

        foreach (var ai in AIs)
        {
            if (!ai.Key.IsNumeric())
            {
                throw new InvalidOperationException("Can only compress numeric AIs");
            }

            buffer = ai.Key.Aggregate(buffer, (buffer, c) => buffer.Append(Characters.GetAlphaBinary(c)));

            FormatApplicationIdentifier(ai, buffer);
        }

        var binaryValue = buffer.ToString().PadRight(buffer.Length + (6 - buffer.Length % 6), '0');
        buffer.Clear();

        for (var i = 0; i < binaryValue.Length; i += 6)
        {
            buffer.Append(Characters.GetChar(binaryValue[i..(i + 6)]));
        }

        return buffer.ToString();
    }

    private void FormatApplicationIdentifier(AI ai, StringBuilder buffer)
    {
        var applicationIdentifier = options.ApplicationIdentifiers.Find(ai.Key)
            ?? throw new InvalidOperationException($"{ai.Key} is not a GS1 AI");

        foreach (var component in applicationIdentifier.Components)
        {
            if (component.Charset == "N")
            {
                if (component.FixedLength)
                {
                    var componentValue = ai.Value[..component.Length];
                    var c = Convert.ToString(Convert.ToInt64(componentValue, 10), 2);
                    var expectedLength = (int)Math.Ceiling(component.Length * Math.Log(10) / Math.Log(2) + 0.01);

                    buffer.Append(c.PadLeft(expectedLength, '0'));
                }
                else
                {
                    var c = Convert.ToString(Convert.ToInt32(ai.Value, 10), 2);
                    var lengthSize = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2) + 0.01);
                    var l2 = Convert.ToString(ai.Value.Length, 2).PadLeft(lengthSize);
                    var nl = (int)Math.Ceiling(lengthSize * Math.Log(10) / Math.Log(2) + 0.01);

                    buffer.Append(l2).Append(c.PadLeft(nl, '0'));
                }
            }
            else
            {
                if (component.FixedLength)
                {
                    var componentValue = ai.Value[..component.Length];

                    FormatValue(componentValue, buffer);
                }
                else
                {
                    var nli = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2) + 0.01);
                    var li = Convert.ToString(ai.Value.Length, 2).PadLeft(nli, '0');

                    FormatValue(ai.Value, buffer, li);
                }
            }

            ai = ai with { Value = ai.Value[(component.FixedLength ? component.Length : Math.Min(component.Length, ai.Value.Length))..] };
        }
    }

    private static void FormatValue(string componentValue, StringBuilder buffer, string? prefix = null)
    {
        if (componentValue.IsNumeric())
        {
            var nv = (int)Math.Ceiling(componentValue.Length * Math.Log(10) / Math.Log(2) + 0.01);

            buffer.Append("000").Append(prefix);
            buffer.Append(Convert.ToString(Convert.ToInt64(componentValue, 10), 2).PadLeft(nv, '0'));
        }
        else if (componentValue.IsLowerCaseHex())
        {
            buffer.Append("001").Append(prefix);

            foreach (var c in componentValue)
            {
                buffer.Append(Characters.GetAlphaBinary(c));
            }
        }
        else if (componentValue.IsUpperCaseHex())
        {
            buffer.Append("010").Append(prefix);

            foreach (var c in componentValue)
            {
                buffer.Append(Characters.GetAlphaBinary(c));
            }
        }
        else if (componentValue.IsUriSafeBase64())
        {
            buffer.Append("011").Append(prefix);

            foreach (var c in componentValue)
            {
                buffer.Append(Characters.GetBinary(c));
            }
        }
        else
        {
            buffer.Append("100").Append(prefix);
            buffer.Append(string.Concat(Encoding.ASCII.GetBytes(componentValue).Select(x => Convert.ToString(x, 2).PadLeft(7, '0'))));
        }
    }
}
