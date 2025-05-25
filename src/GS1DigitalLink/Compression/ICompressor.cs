using GS1DigitalLink.Model;

namespace GS1DigitalLink.Compression
{
    public interface ICompressor
    {
        public string CompressPartial(IEnumerable<AI> AIs, GS1CompressionOptions options);
        public string Compress(IEnumerable<AI> AIs, GS1CompressionOptions options);
    }
}
