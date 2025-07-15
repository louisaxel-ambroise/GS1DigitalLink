using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Model;

public record DigitalLink
{
    public string Result { get; set; } = "";
}

public class DigitalLinkBuilder
{
    private OrderedDictionary<ApplicationIdentifier, string> _qualifiers = [];
    private OrderedDictionary<ApplicationIdentifier, string> _attributes = [];

    public void Set(ApplicationIdentifier ai, string value)
    {
        if(_qualifiers.TryGetValue(ai, out var existing))
        {
            if(existing != value)
            {
                throw new Exception($"Another value for AI was already added. Existing: '{existing}', Current: '{value}'");
            }
        }

        _qualifiers[ai] = value;
    }

    public DigitalLink Build()
    {
        var result = new DigitalLink();
        Validate();

        foreach(var (key, value) in _qualifiers)
        {
            result.Result += $"({key.Code}){value}";
        }

        return result;
    }

    private void Validate()
    {
        if (_qualifiers.Count == 0 || !_qualifiers.First().Key.IsPrimaryKey)
        {
            throw new Exception("DL must contain at least one AI that is a PrimaryKey identifier");
        }

        var qualifiersValidator = new KeyQualifierValidator(_qualifiers.First().Key.Qualifiers);

        foreach (var (ai, value) in _qualifiers)
        {
            if (ai.Requirements is not null && !ai.Requirements.IsFulfilledBy(_qualifiers.Keys.Select(x => x.Code)))
            {
                throw new Exception($"AI '{ai.Code}' required associations is not fulfilled");
            }
            if (ai.Exclusions is not null && ai.Exclusions.IsFulfilledBy(_qualifiers.Keys.Select(x => x.Code)))
            {
                throw new Exception($"AI '{ai.Code}' contains invalid AI pairing");
            }

            if (!ai.IsPrimaryKey && !qualifiersValidator.Validate(ai))
            {
                throw new Exception($"AI '{_qualifiers.First().Key.Code}' has invalid qualifier or qualifier order");
            }
        }
    }
}

internal class KeyQualifierValidator
{
    private List<string[]>? _candidates;

    public KeyQualifierValidator(KeyQualifiers qualifiers)
    {
        _candidates = qualifiers?.AllowedQualifiers;
    }

    public bool Validate(ApplicationIdentifier ai)
    {
        if (_candidates is null) return true;

        _candidates = _candidates
            .Select(c => c.SkipWhile(e => e != ai.Code).ToArray())
            .Where(c => c.Length > 0)
            .ToList();

        return _candidates.Count > 0;
    }
}