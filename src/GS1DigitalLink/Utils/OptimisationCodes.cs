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

        public int Priority => SequenceAIs.Length;

        public bool IsFulfilledBy(IEnumerable<string> identifierCodes) => SequenceAIs.All(identifierCodes.Contains);

        public static readonly OptimizationCode Default = new()
        {
            Code = string.Empty,
            SequenceAIs = [],
            Meaning = string.Empty,
            Usage = string.Empty
        };
    }
}
