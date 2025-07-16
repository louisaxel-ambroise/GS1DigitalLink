using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Model;

public record DigitalLink
{
    public string Result { get; set; } = "";
}

public class RegisteredIdentifier
{
    public ApplicationIdentifier Ai { get; set; }
    public string Value { get; set; }
    public IdentifierType Type { get; set; }
}

public enum IdentifierType
{
    Qualifier,
    Attribute
}

public class DigitalLinkBuilder
{
    private List<RegisteredIdentifier> _identifiers = [];

    public void Set(ApplicationIdentifier ai, string value, IdentifierType type)
    {
        var existing = _identifiers.SingleOrDefault(i => i.Ai == ai);

        if (existing is not null && existing.Value != value)
        {
            throw new Exception($"Another value for AI was already added. Existing: '{existing}', Current: '{value}'");
        }
        else if (existing is null)
        {
            _identifiers.Add(new() { Ai = ai, Value = value, Type = type });
        }
    }

    public DigitalLink Build()
    {
        var result = new DigitalLink();
        Validate();

        foreach(var identifier in _identifiers)
        {
            result.Result += $"({identifier.Ai.Code}){identifier.Value}";
        }

        return result;
    }

    private void Validate()
    {
        if (_identifiers.Count == 0 || !_identifiers.First().Ai.IsPrimaryKey)
        {
            throw new Exception("DL must contain at least one AI that is a PrimaryKey identifier");
        }

        var qualifiersValidator = new KeyQualifierValidator(_identifiers.First().Ai.Qualifiers);

        foreach (var identifier in _identifiers)
        {
            if (!identifier.Ai.Requirements.IsEmpty && !identifier.Ai.Requirements.IsFulfilledBy(_identifiers.Select(x => x.Ai.Code)))
            {
                throw new Exception($"AI '{identifier.Ai.Code}' required associations is not fulfilled");
            }
            if (!identifier.Ai.Exclusions.IsEmpty && identifier.Ai.Exclusions.IsFulfilledBy(_identifiers.Except([identifier]).Select(x => x.Ai.Code)))
            {
                throw new Exception($"AI '{identifier.Ai.Code}' contains invalid AI pairing");
            }

            if (!identifier.Ai.IsPrimaryKey && identifier.Type == IdentifierType.Qualifier && !qualifiersValidator.Validate(identifier))
            {
                throw new Exception($"AI '{_identifiers.First().Ai.Code}' has invalid qualifier or qualifier order");
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

    public bool Validate(RegisteredIdentifier ai)
    {
        if (_candidates is null || ai.Type != IdentifierType.Qualifier) return true;

        _candidates = _candidates
            .Select(c => c.SkipWhile(e => e != ai.Ai.Code).ToArray())
            .Where(c => c.Length > 0)
            .ToList();

        return _candidates.Count > 0;
    }
}