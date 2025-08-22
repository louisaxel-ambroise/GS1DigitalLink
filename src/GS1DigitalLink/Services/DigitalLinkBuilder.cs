using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;

namespace GS1DigitalLink.Services;

public class DigitalLinkBuilder
{
    public static DigitalLinkBuilder Create() => new();

    private readonly List<KeyValue> _values = [];
    private readonly List<string> _errors = [];

    private DigitalLinkBuilder() { }

    public DigitalLinkBuilder Add(KeyValue keyValue)
    {
        _values.Add(keyValue);

        return this;
    }

    public DigitalLinkBuilder AddRange(IEnumerable<KeyValue> keyValues)
    {
        return keyValues.Aggregate(this, (builder, keyValue) => builder.Add(keyValue));
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
                Qualifiers = [.. _values.Where(v => v.Type == KeyValueType.Qualifier)],
                Attributes = [.. _values.Where(v => v.Type == KeyValueType.Attribute)],
                QueryElements = [.. _values.Where(v => v.Type == KeyValueType.QueryElement)],
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

            Validate(ai, allKeys.Except(ai.Code));

            if(identifier.Type is KeyValueType.PrimaryKey)
            {
                foreach(var component in ai.Components.Where(c => c.CheckDigit))
                {
                    CheckDigitHelper.EnsureIsValid(identifier.Value);
                }

                if (ai.Qualifiers.AllowedQualifiers.Count > 0)
                {
                    var qualifierKeys = _values.Where(v => v.Type == KeyValueType.Qualifier).Select(x => x.Key);

                    if (!ValidateQualifier(ai.Qualifiers.AllowedQualifiers, qualifierKeys))
                    {
                        _errors.Add($"Invalid qualifier");
                    }
                }
            }
        }
    }

    private void Validate(ApplicationIdentifier ai, IEnumerable<string> otherKeys)
    {
        if (!ai.Requirements.IsEmpty && !ai.Requirements.IsFulfilledBy(otherKeys))
        {
            _errors.Add($"AI '{ai.Code}' required associations is not fulfilled");
        }
        else if (!ai.Exclusions.IsEmpty && ai.Exclusions.IsFulfilledBy(otherKeys))
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
