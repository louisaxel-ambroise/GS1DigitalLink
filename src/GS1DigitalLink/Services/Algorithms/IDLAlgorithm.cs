using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Services.Algorithms;

public interface IDLAlgorithm
{
    void UseAlgorithm(string version, AlgorithmType type);
    bool TryGetQualifier(string? key, out ApplicationIdentifier ai);
    bool TryGetDataAttribute(string? key, out ApplicationIdentifier ai);
    IEnumerable<KeyValue> Parse(BitStream binaryStream);
    string Format(IEnumerable<KeyValue> entries, DigitalLinkFormatterOptions options);
}
