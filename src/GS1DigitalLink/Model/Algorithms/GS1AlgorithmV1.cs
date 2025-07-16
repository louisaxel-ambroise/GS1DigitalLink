using GS1DigitalLink.Utils;
using System.Text;
using static GS1DigitalLink.Utils.StoredOptimisationCodes;

namespace GS1DigitalLink.Model.Algorithms;

public sealed class GS1AlgorithmV1 : IDLAlgorithm
{
    public IReadOnlyList<OptimizationCode> OptimizationCodes { get; }
    public IReadOnlyList<ApplicationIdentifier> ApplicationIdentifiers { get; }
    public IReadOnlyList<ApplicationIdentifier> Qualifiers { get; }
    public IReadOnlyList<ApplicationIdentifier> DataAttributes { get; }

    public GS1AlgorithmV1(IReadOnlyList<OptimizationCode> optimizationCodes, IReadOnlyList<ApplicationIdentifier> applicationIdentifiers)
    {
        OptimizationCodes = optimizationCodes;
        ApplicationIdentifiers = applicationIdentifiers;
        Qualifiers = ApplicationIdentifiers.Where(ai => ai.IsPrimaryKey || ApplicationIdentifiers.SelectMany(i => i.Qualifiers?.AllowedQualifiers?.SelectMany(a => a) ?? []).Contains(ai.Code)).ToList();
        DataAttributes = ApplicationIdentifiers.Except(Qualifiers).ToList();
    }

    public void Parse(BitStream binaryStream, DigitalLinkBuilder result)
    {
        var ais = new List<string>();
        var current = Convert.ToByte(binaryStream.Current.ToString(), 2);

        if (!IsNumeric(current))
        {
            if (!TryGetOptimizedCode(current, out var optimizedAis))
            {
                throw new Exception();
            }

            ais.AddRange(optimizedAis!.SequenceAIs);
        }
        else
        {
            var code = current.ToString("X2");
            var length = GetLength(code);

            if (length < 0)
            {
                throw new Exception("No AI matches the value " + code);
            }
            for (var i = 2; i < length; i++)
            {
                binaryStream.Buffer(4);
                var remain = Convert.ToByte(binaryStream.Current.ToString(), 2);

                if (!IsNumeric(remain))
                {
                    throw new Exception("AI code must only contain numeric value");
                }

                code += remain.ToString("X1");
            }

            ais.Add(code);
        }

        ais.ForEach(ai =>
        {
            var value = ParseApplicationIdentifier(ai, binaryStream);

            result.Set(value.Item1, value.Item2, IdentifierType.Qualifier);
        });
    }

    public string Format(IEnumerable<Entry> entries, DigitalLinkFormatterOptions options)
    {
        var buffer = new StringBuilder();
        var compression = new StringBuilder();

        if (options.CompressionType is DLCompressionType.Partial)
        {
            var key = entries.FirstOrDefault(x => TryGetQualifier(x.Key, out var ai) && ai.IsPrimaryKey);

            if (key is null) throw new Exception("No AI key found in entries");

            buffer.Append('/').Append(key.Key).Append('/').Append(key.Value).Append('/');

            entries = entries.Except([key]);
        }
        else if(options.CompressionType is DLCompressionType.Full)
        {
            if (TryGetBestOptimization(entries.Select(x => x.Key), out var optimization))
            {
                foreach (var c in optimization.Code)
                {
                    compression.Append(Characters.GetAlphaBinary(c));
                }
                foreach (var element in optimization.SequenceAIs)
                {
                    if (TryGetQualifier(element, out var applicationIdentifier))
                    {
                        var entry = entries.Single(a => a.Key == element);

                        FormatApplicationIdentifier(applicationIdentifier, entry.Value, compression);
                    }
                }

                entries = entries.Where(x => !optimization.SequenceAIs.Contains(x.Key));
            }
        }

        foreach (var entry in entries)
        {
            if (!TryGetQualifier(entry.Key, out var applicationIdentifier))
            {
                throw new InvalidOperationException($"Unknown AI: {entry.Key}");
            }

            compression = entry.Key.Aggregate(compression, (b, c) => b.Append(Characters.GetAlphaBinary(c)));

            FormatApplicationIdentifier(applicationIdentifier, entry.Value, compression);
        }

        compression.Append(new string('0', 6 - buffer.Length % 6));
        buffer.Append(compression.GetChars());

        return buffer.ToString();
    }

    public bool TryGetQualifier(string? code, out ApplicationIdentifier ai)
    {
        ai = Qualifiers.SingleOrDefault(x => x.Code == code, ApplicationIdentifier.None);

        return ai != ApplicationIdentifier.None;
    }
    public bool TryGetDataAttribute(string? code, out ApplicationIdentifier ai)
    {
        ai = DataAttributes.SingleOrDefault(x => x.Code == code, ApplicationIdentifier.None);

        return ai != ApplicationIdentifier.None;
    }

    private int GetLength(string code)
    {
        return CodeLength.TryGetValue(code, out var length) ? length : -1;
    }

    private bool TryGetOptimizedCode(byte input, out OptimizationCode optimizationCode)
    {
        optimizationCode = OptimizationCodes
            .Where(x => input.ToString("X2") == x.Code)
            .FirstOrDefault(OptimizationCode.Default);

        return optimizationCode != OptimizationCode.Default;
    }

    private bool TryGetBestOptimization(IEnumerable<string> ais, out OptimizationCode optimizationCode)
    {
        optimizationCode = OptimizationCodes
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault(x => x.IsFulfilledBy(ais), OptimizationCode.Default);

        return optimizationCode != OptimizationCode.Default;
    }

    private (ApplicationIdentifier, string) ParseApplicationIdentifier(string code, BitStream inputStream)
    {
        if (!TryGetQualifier(code, out var ai))
        {
            throw new Exception("AI not found");
        }
     
        return (ai, ai.ReadFrom(inputStream));
    }

    private static bool IsNumeric(byte current)
    {
        return current >> 4 <= 9 && (current & 0x0F) <= 9;
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

    private Dictionary<string, int> CodeLength => ApplicationIdentifiers.GroupBy(x => x.Code[..2]).ToDictionary(x => x.Key, x => x.First().Code.Length);
}
