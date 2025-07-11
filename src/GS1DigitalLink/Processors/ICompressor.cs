using GS1DigitalLink.Model;
using GS1DigitalLink.Model.Algorithms;

namespace GS1DigitalLink.Processors;

public interface ICompressor
{
    public string CompressPartial(IEnumerable<Entry> entries, IGS1Algorithm algorithm);
    public string Compress(IEnumerable<Entry> entries, IGS1Algorithm algorithm);
}
