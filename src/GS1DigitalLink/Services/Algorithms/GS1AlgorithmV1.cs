using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;
using System.Text;
using static GS1DigitalLink.Utils.StoredOptimisationCodes;

namespace GS1DigitalLink.Services.Algorithms;

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
        Qualifiers = [.. ApplicationIdentifiers.Where(ai => ai.IsPrimaryKey || ApplicationIdentifiers.SelectMany(i => i.Qualifiers?.AllowedQualifiers?.SelectMany(a => a) ?? []).Contains(ai.Code))];
        DataAttributes = [.. ApplicationIdentifiers.Except(Qualifiers)];
    }

    public IEnumerable<KeyValue> Parse(BitStream binaryStream)
    {
        var ais = new List<string>();
        var current = Convert.ToByte(binaryStream.Current.ToString(), 2);

        if (!current.IsNumeric())
        {
            if (!TryGetOptimizedCode(current, out var optimizedAis))
            {
                throw new InvalidOperationException();
            }

            ais.AddRange(optimizedAis!.SequenceAIs);
        }
        else
        {
            var code = current.ToString("X2");
            var length = GetLength(code);

            if (length < 0)
            {
                throw new InvalidOperationException("No AI matches the value " + code);
            }
            for (var i = 2; i < length; i++)
            {
                binaryStream.Buffer(4);
                var remain = Convert.ToByte(binaryStream.Current.ToString(), 2);

                code += remain.ToNumericString();
            }

            ais.Add(code);
        }

        return ais.Select(ai => ParseApplicationIdentifier(ai, binaryStream));
    }

    public string Format(IEnumerable<KeyValue> entries, DigitalLinkFormatterOptions options)
    {
        var buffer = new StringBuilder();
        var compression = new StringBuilder();

        if (options.CompressionType is DLCompressionType.Partial)
        {
            var key = entries.FirstOrDefault(x => TryGet(x.Key, Qualifiers, out var identifier) && identifier.IsPrimaryKey) 
                   ?? throw new Exception("No AI key found in entries");
            buffer.Append('/').Append(key.Key).Append('/').Append(key.Value).Append('/');

            entries = entries.Except([key]);
        }
        else if(options.CompressionType is DLCompressionType.Full)
        {
            if (TryGetBestOptimization(entries.Select(x => x.Key), out var optimization))
            {
                compression.Append(Alphabets.GetAlphaBinary(optimization.Code));

                foreach (var element in optimization.SequenceAIs)
                {
                    if (TryGet(element, ApplicationIdentifiers, out var applicationIdentifier))
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
            if (!TryGet(entry.Key, ApplicationIdentifiers, out var applicationIdentifier))
            {
                throw new InvalidOperationException($"Unknown AI: {entry.Key}");
            }

            compression = entry.Key.Aggregate(compression, (b, c) => b.Append(Alphabets.GetAlphaBinary(c)));

            FormatApplicationIdentifier(applicationIdentifier, entry.Value, compression);
        }

        compression.Append(new string('0', (6 - compression.Length % 6)%6));
        buffer.Append(compression.GetChars());

        return buffer.ToString();
    }

    public bool TryGetQualifier(string? code, out ApplicationIdentifier ai) => TryGet(code, Qualifiers, out ai);
    public bool TryGetDataAttribute(string? code, out ApplicationIdentifier ai) => TryGet(code, DataAttributes, out ai);

    public static bool TryGet(string? code, IEnumerable<ApplicationIdentifier> identifiers, out ApplicationIdentifier ai)
    {
        ai = identifiers.SingleOrDefault(x => x.Code == code, ApplicationIdentifier.None);

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

    private KeyValue ParseApplicationIdentifier(string code, BitStream inputStream)
    {
        if (TryGetQualifier(code, out var qualifier))
        {
            var value = qualifier.ReadFrom(inputStream);

            return qualifier.IsPrimaryKey
                ? KeyValue.PrimaryKey(code, value)
                : KeyValue.Qualifier(code, value);
        }
        if (TryGetDataAttribute(code, out var attribute))
        {
            var value = attribute.ReadFrom(inputStream);

            return KeyValue.Attribute(code, value);
        }

        throw new Exception("AI not found");
    }

    private static void FormatApplicationIdentifier(ApplicationIdentifier ai, string value, StringBuilder buffer)
    {
        var remaining = ai.Components.Aggregate(value, (remaining, component) =>
        {
            buffer.Append(ComponentFormatter.Format(component, value));

            var startIndex = component.FixedLength ? component.Length : Math.Min(component.Length, value.Length);
            return value[startIndex..];
        });

        if(remaining.Length > 0)
        {
            throw new Exception("Value does not match the list of components");
        }
    }

    // TODO: get correct algorithm from (version, type) tuple.
    public void UseAlgorithm(string version, AlgorithmType type)
    {
        if ((version, type) == ("0001", AlgorithmType.GS1)) return;

        throw new Exception("Unknown algorithm version");
    }

    private Dictionary<string, int> CodeLength => ApplicationIdentifiers.GroupBy(x => x.Code[..2]).ToDictionary(x => x.Key, x => x.First().Code.Length);
}
