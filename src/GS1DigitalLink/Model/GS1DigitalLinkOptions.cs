using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Model;

public class GS1DigitalLinkOptions
{
    public required GS1Algorithms Algorithms { get; init; }
    public required StoredOptimisationCodes OptimizationCodes { get; init; }
    public required GS1Identifiers ApplicationIdentifiers { get; init; }
}

public class GS1Algorithms(IGS1Algorithm[] algorithms)
{
    public IGS1Algorithm Default => algorithms.First();

    public IGS1Algorithm? Find(string code) => algorithms.Single(a => a.Matches(code));
}

public interface IGS1Algorithm
{
    void Decompress(BitStream binaryStream, DigitalLink result, GS1DigitalLinkOptions options);
    bool Matches(string code);
}

public sealed class CompressionAlgorithmV1 : IGS1Algorithm
{
    public void Decompress(BitStream binaryStream, DigitalLink result, GS1DigitalLinkOptions options)
    {
        var ais = new List<string>();
        var current = Convert.ToByte(binaryStream.Current, 2);

        if (!IsNumeric(current))
        {
            if (!options.OptimizationCodes.TryGetOptimizedCode(current, out var optimizedAis))
            {
                throw new Exception();
            }

            ais.AddRange(optimizedAis!.SequenceAIs);
        }
        else
        {
            var code = current.ToString("X2");
            var length = options.ApplicationIdentifiers.GetLength(code);

            if (length < 0)
            {
                throw new InvalidOperationException("No AI matches the value " + code);
            }
            for (var i = 2; i < length; i++)
            {
                binaryStream.Buffer(4);
                var remain = Convert.ToByte(binaryStream.Current, 2);

                if (!IsNumeric(remain))
                {
                    throw new InvalidOperationException("AI code must only contain numeric value");
                }

                code += remain.ToString("X1");
            }

            ais.Add(code);
        }

        ais.ForEach(ai =>
        {
            var value = ParseApplicationIdentifier(ai, binaryStream, options);

            result.OnParsedAI(ai, value);
        });
    }

    private static string ParseApplicationIdentifier(string code, BitStream inputStream, GS1DigitalLinkOptions options)
    {
        var ai = options.ApplicationIdentifiers.Find(code) ?? throw new InvalidOperationException($"No matching AI for Code {code}");

        return ai.ReadFrom(inputStream);
    }

    private static bool IsNumeric(byte current)
    {
        return current >> 4 <= 9 && (current & 0x0F) <= 9;
    }

    public bool Matches(string code) => code == "0000";
}