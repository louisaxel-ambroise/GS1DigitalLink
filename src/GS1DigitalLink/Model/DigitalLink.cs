using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Model;

public record DigitalLink
{
    public string Result { get; set; } = "";
}

public class DigitalLinkBuilder
{
    private List<(ApplicationIdentifier, string)> _values = [];

    public void Add(ApplicationIdentifier ai, string value)
    {
        _values.Add((ai, value));
    }

    public DigitalLink Build()
    {
        var result = new DigitalLink();

        foreach(var (key, value) in _values)
        {
            Validate(key, value);
            result.Result += $"({key.Code}){value}";
        }

        return result;
    }

    private void Validate(ApplicationIdentifier ai, string value)
    {
        foreach(var exclusion in ai.Excludes)
        {
            if(_values.Any(x => x.Item1.Code == exclusion))
            {
                throw new Exception($"AI {exclusion} cannot be used in conjuction with {ai.Code}");
            }
        }
        foreach (var requirement in ai.Requires)
        {
            if (!_values.Any(x => x.Item1.Code == requirement))
            {
                throw new Exception($"AI {requirement} cannot be used in conjuction with {ai.Code}");
            }
        }
    }
}

public record DigitalLinkFormatterOptions
{
    public DLCompressionType CompressionType { get; set; }
}

public enum DLCompressionType
{
    Full,
    Partial
}