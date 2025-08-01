using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Services;

public class DigitalLinkBuilder
{
    private readonly List<KeyValue> _values = [];
    private readonly List<string> _errors = [];

    public DigitalLinkBuilder Add(KeyValue keyValue)
    {
        _values.Add(keyValue);

        return this;
    }

    public DigitalLinkBuilder AddRange(IEnumerable<KeyValue> keyValues)
    {
        _values.AddRange(keyValues);

        return this;
    }

    public bool Validate(GS1Identifiers identifiers, out DigitalLink? result)
    {
        result = null;
        ApplyValidationRules(identifiers);

        if (!_errors.Any())
        {
            result = new DigitalLink
            {
                PrimaryKey = _values.Single(v => v.Type == KeyValueType.PrimaryKey),
                Qualifiers = _values.Where(v => v.Type == KeyValueType.Qualifier).ToArray(),
                Attributes = _values.Where(v => v.Type == KeyValueType.Attribute).ToArray(),
                QueryElements = _values.Where(v => v.Type == KeyValueType.QueryElement).ToArray(),
            };
        }

        return result is not null;
    }

    public ErrorResult GetErrorResult()
    {
        return new ErrorResult
        {
            Errors = _errors
        };
    }

    // TODO: review
    private void ApplyValidationRules(GS1Identifiers identifiers)
    {
        var gs1Values = _values.Where(v => v.Type is not KeyValueType.QueryElement);
        var allKeys = gs1Values.Select(x => x.Key).ToArray();

        foreach (var identifier in gs1Values)
        {
            var ai = identifiers.ApplicationIdentifiers.Single(i => i.Code == identifier.Key);

            Validate(ai, allKeys);

            if(identifier.Type is KeyValueType.PrimaryKey && ai.Qualifiers is not null && ai.Qualifiers.AllowedQualifiers.Any())
            {
                if (!ValidateQualifier(ai.Qualifiers.AllowedQualifiers, _values.Where(v => v.Type == KeyValueType.Qualifier).Select(x => x.Key)))
                {
                    _errors.Add($"Invalid qualifier");
                }
            }
        }
    }

    private void Validate(ApplicationIdentifier ai, IEnumerable<string> allKeys)
    {
        if (!ai.Requirements.IsEmpty && !ai.Requirements.IsFulfilledBy(allKeys.Except(ai.Code)))
        {
            _errors.Add($"AI '{ai.Code}' required associations is not fulfilled");
        }
        else if (!ai.Exclusions.IsEmpty && ai.Exclusions.IsFulfilledBy(allKeys.Except(ai.Code)))
        {
            _errors.Add($"AI '{ai.Code}' contains invalid AI pairing");
        }
    }

    private static bool ValidateQualifier(IEnumerable<string[]> candidateQualifiers, IEnumerable<string> actualQualifiers)
    {
        candidateQualifiers = actualQualifiers.Aggregate(candidateQualifiers, (current, candidate) => current
            .Select(c => c.SkipWhile(e => e != candidate).ToArray())
            .Where(c => c.Length > 0));

        return candidateQualifiers.Any();
    }
}
