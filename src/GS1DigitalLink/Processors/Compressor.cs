using GS1DigitalLink.Model;
using GS1DigitalLink.Model.Algorithms;
using GS1DigitalLink.Utils;
using System.Text;

namespace GS1DigitalLink.Compression;

public sealed class Compressor : ICompressor
{
    public string CompressPartial(IEnumerable<Entry> entries, IGS1Algorithm algorithm)
    {
        var buffer = new StringBuilder();
        var keys = entries.Where(x => algorithm.FindAI(x.Key).IsPrimaryKey);

        // Do not compress Primary Key(s) of the DigitalLink
        foreach (var key in keys)
        {
            buffer.Append(key.Key).Append('/').Append(key.Value).Append('/');
        }

        buffer.Append(Compress(entries.Except(keys), algorithm));

        return buffer.ToString();
    }

    // TODO: let the algorithm format the AIs, as the way to process might change between versions.
    public string Compress(IEnumerable<Entry> entries, IGS1Algorithm algorithm)
    {
        var buffer = new StringBuilder();

        if (algorithm.TryGetBestOptimization(entries.Select(x => x.Key), out var optimization))
        {
            foreach (var c in optimization.Code)
            {
                buffer.Append(Characters.GetAlphaBinary(c));
            }
            foreach (var element in optimization.SequenceAIs)
            {
                var applicationIdentifier = algorithm.FindAI(element);
                var entry = entries.Single(a => a.Key == element);

                FormatApplicationIdentifier(applicationIdentifier, entry.Value, buffer);
            }

            entries = entries.Where(x => !optimization.SequenceAIs.Contains(x.Key));
        }

        foreach (var entry in entries)
        {
            if (!algorithm.TryGetAI(entry.Key, out var applicationIdentifier))
            {
                throw new InvalidOperationException($"Unknown AI: {entry.Key}");
            }

            buffer = entry.Key.Aggregate(buffer, (buffer, c) => buffer.Append(Characters.GetAlphaBinary(c)));

            FormatApplicationIdentifier(applicationIdentifier, entry.Value, buffer);
        }

        var binaryValue = buffer.ToString().PadRight(buffer.Length + (6 - buffer.Length % 6), '0');
        buffer.Clear();

        for (var i = 0; i < binaryValue.Length; i += 6)
        {
            buffer.Append(Characters.GetChar(binaryValue[i..(i + 6)]));
        }

        return buffer.ToString();
    }

    private static void FormatApplicationIdentifier(ApplicationIdentifier ai, string value, StringBuilder buffer)
    {
        foreach (var component in ai.Components)
        {
            if (component.Charset == "N")
            {
                if (component.FixedLength)
                {
                    var componentValue = value[..component.Length];
                    var c = Convert.ToString(Convert.ToInt64(componentValue, 10), 2);
                    var expectedLength = (int)Math.Ceiling(component.Length * Math.Log(10) / Math.Log(2) + 0.01);

                    buffer.Append(c.PadLeft(expectedLength, '0'));
                }
                else
                {
                    var c = Convert.ToString(Convert.ToInt32(value, 10), 2);
                    var lengthSize = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2) + 0.01);
                    var l2 = Convert.ToString(value.Length, 2).PadLeft(lengthSize);
                    var nl = (int)Math.Ceiling(lengthSize * Math.Log(10) / Math.Log(2) + 0.01);

                    buffer.Append(l2).Append(c.PadLeft(nl, '0'));
                }
            }
            else
            {
                if (component.FixedLength)
                {
                    var componentValue = value[..component.Length];

                    FormatValue(componentValue, buffer);
                }
                else
                {
                    var nli = (int)Math.Ceiling(Math.Log(component.Length) / Math.Log(2) + 0.01);
                    var li = Convert.ToString(value.Length, 2).PadLeft(nli, '0');

                    FormatValue(value, buffer, li);
                }
            }

            value = value[(component.FixedLength ? component.Length : Math.Min(component.Length, value.Length))..];
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
