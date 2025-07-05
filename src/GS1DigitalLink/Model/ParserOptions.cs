using GS1DigitalLink.Model.Algorithms;

namespace GS1DigitalLink.Model;

public sealed class ParserOptions(IGS1Algorithm[] Algorithms)
{
    public IGS1Algorithm DefaultAlgorithm => Algorithms.First();

    public IGS1Algorithm FindAlgorithm(string code)
    {
        return Algorithms.FirstOrDefault(a => a.Matches(code)) ?? throw new InvalidOperationException("Unknown algorithm version " + code);
    }
}
