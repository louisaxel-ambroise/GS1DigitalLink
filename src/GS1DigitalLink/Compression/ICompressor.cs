using GS1DigitalLink.Model;

namespace GS1DigitalLink.Compression;

public interface ICompressor
{
    public string CompressPartial(IEnumerable<AI> AIs);
    public string Compress(IEnumerable<AI> AIs);
}
