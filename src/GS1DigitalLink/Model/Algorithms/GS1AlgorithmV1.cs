using GS1DigitalLink.Utils;
using static GS1DigitalLink.Utils.StoredOptimisationCodes;

namespace GS1DigitalLink.Model.Algorithms;

public sealed class GS1AlgorithmV1(IReadOnlyList<OptimizationCode> OptimizationCodes, IReadOnlyList<ApplicationIdentifier> ApplicationIdentifiers) : IGS1Algorithm
{
    public bool TryGetAI(string code, out ApplicationIdentifier ai)
    {
        ai = FindAI(code);

        return ai != ApplicationIdentifier.None;
    }

    public ApplicationIdentifier FindAI(string code)
    {
        return ApplicationIdentifiers.SingleOrDefault(x => x.Code == code, ApplicationIdentifier.None);
    }
    public int GetLength(string code)
    {
        return CodeLength.TryGetValue(code, out var length) ? length : -1;
    }

    public bool TryGetOptimizedCode(byte input, out OptimizationCode optimizationCode)
    {
        optimizationCode = OptimizationCodes
            .Where(x => input.ToString("X2") == x.Code)
            .FirstOrDefault(OptimizationCode.Default);

        return optimizationCode != OptimizationCode.Default;
    }

    public bool TryGetBestOptimization(IEnumerable<string> ais, out OptimizationCode optimizationCode)
    {
        optimizationCode = OptimizationCodes
            .Where(x => x.IsFulfilledBy(ais))
            .OrderByDescending(x => x.CompressedAIsCount)
            .FirstOrDefault(OptimizationCode.Default);

        return optimizationCode != OptimizationCode.Default;
    }

    public void Parse(BitStream binaryStream, DigitalLink result, ParserOptions options)
    {
        var ais = new List<string>();
        var current = Convert.ToByte(binaryStream.Current, 2);

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
                result.OnError("No AI matches the value " + code);
                return;
            }
            for (var i = 2; i < length; i++)
            {
                binaryStream.Buffer(4);
                var remain = Convert.ToByte(binaryStream.Current, 2);

                if (!IsNumeric(remain))
                {
                    result.OnError("AI code must only contain numeric value");
                    return;
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

    private string ParseApplicationIdentifier(string code, BitStream inputStream, ParserOptions options)
    {
        var ai = FindAI(code);

        return ai.ReadFrom(inputStream);
    }

    private static bool IsNumeric(byte current)
    {
        return current >> 4 <= 9 && (current & 0x0F) <= 9;
    }

    private Dictionary<string, int> CodeLength => ApplicationIdentifiers.GroupBy(x => x.Code[..2]).ToDictionary(x => x.Key, x => x.First().Code.Length);

    public bool Matches(string code) => code == "0000";
}