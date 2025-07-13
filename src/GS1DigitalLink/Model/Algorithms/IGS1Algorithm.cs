using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Model.Algorithms;

public interface IGS1Algorithm
{
    bool TryGetAI(string key, out ApplicationIdentifier ai);
    void Parse(BitStream binaryStream, DigitalLinkBuilder result);
    string Format(IEnumerable<Entry> entries, DigitalLinkFormatterOptions options);
}
