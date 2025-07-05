using GS1DigitalLink.Utils;
using static GS1DigitalLink.Utils.StoredOptimisationCodes;

namespace GS1DigitalLink.Model.Algorithms;

public interface IGS1Algorithm
{
    ApplicationIdentifier FindAI(string code);
    int GetLength(string code);
    bool TryGetAI(string key, out ApplicationIdentifier ai);
    bool TryGetBestOptimization(IEnumerable<string> ais, out OptimizationCode optimizationCode);
    bool TryGetOptimizedCode(byte input, out OptimizationCode optimizationCode);
    void Parse(BitStream binaryStream, DigitalLink result, ParserOptions options);
    bool Matches(string code);
}
