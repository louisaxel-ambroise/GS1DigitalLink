namespace GS1DigitalLink.Utils;

public sealed class StoredOptimisationCodes
{
    public required IReadOnlyList<OptimizationCode> OptimizationCodes { get; init; }

    public record OptimizationCode
    {
        public required string Code { get; init; }
        public required string[] SequenceAIs { get; init; }
        public required string Meaning { get; init; }
        public required string Usage { get; init; }

        public int CompressedAIsCount => SequenceAIs.Length;

        public bool IsFulfilledBy(IEnumerable<string> identifierCodes)
        {
            return SequenceAIs.All(identifierCodes.Contains);
        }

        public static readonly OptimizationCode Default = new()
        {
            Code = string.Empty,
            SequenceAIs = [],
            Meaning = string.Empty,
            Usage = string.Empty
        };
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
}
