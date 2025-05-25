using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Model
{
    public class GS1CompressionOptions
    {
        public required StoredOptimisationCodes OptimizationCodes { get; init; }
        public required GS1Identifiers ApplicationIdentifiers { get; init; }
    }
}
