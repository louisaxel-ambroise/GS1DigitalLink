using System.Text.Json;
using System.Text.Json.Serialization;

namespace GS1DigitalLink.Utils;

public record GS1Identifiers
{
    [JsonPropertyName("applicationIdentifiers")]
    public required IReadOnlyList<ApplicationIdentifier> ApplicationIdentifiers { get; init; }

    public Dictionary<string, int> CodeLength => ApplicationIdentifiers.GroupBy(x => x.Code[..2]).ToDictionary(x => x.Key, x => x.First().Code.Length);
}

public record ApplicationIdentifier
{
    private static readonly ApplicationIdentifier _none = new();
    public static ApplicationIdentifier None => _none;

    [JsonPropertyName("applicationIdentifier")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("gs1DigitalLinkPrimaryKey")]
    public bool IsPrimaryKey { get; init; }

    [JsonPropertyName("components")]
    public IReadOnlyList<AIComponent> Components { get; init; } = [];

    [JsonPropertyName("regex")]
    public string Pattern { get; set; } = string.Empty;

    [JsonPropertyName("excludes")]
    [JsonConverter(typeof(RequirementConverter<DisjuctionAIRequirementGroup>))]
    public AIRequirements Exclusions { get; set; } = new();

    [JsonPropertyName("requires")]
    [JsonConverter(typeof(RequirementConverter<ConjuctionAIRequirementGroup>))]
    public AIRequirements Requirements { get; set; } = new();

    [JsonPropertyName("gs1DigitalLinkQualifiers")]
    [JsonConverter(typeof(KeyQualifierConverter))]
    public KeyQualifiers? Qualifiers { get; set; }

    public class AIComponent
    {
        [JsonPropertyName("type")]
        public required string Charset { get; init; }

        [JsonPropertyName("length")]
        public required int Length { get; init; }

        [JsonPropertyName("fixedLength")]
        public required bool FixedLength { get; init; }

        [JsonPropertyName("checkDigit")]
        public bool CheckDigit { get; init; }
    }
}

public class KeyQualifiers
{
    public List<string[]> AllowedQualifiers { get; set; } = [];

    internal bool Validate(ApplicationIdentifier ai)
    {
        throw new NotImplementedException();
    }
}

public class AIRequirements
{
    public AIRequirementGroup[] Groups { get; set; } = [];

    public bool IsEmpty => Groups.Length == 0;

    internal bool IsFulfilledBy(IEnumerable<string> values)
    {
        return Groups.Any(g => g.IsFulfilledBy(values));
    }
}

public abstract class AIRequirementGroup
{
    public abstract bool IsFulfilledBy(IEnumerable<string> values);
}

public abstract class AIListRequirementGroup : AIRequirementGroup
{
    public List<string> RequiredAIs { get; set; } = [];
}

public class ConjuctionAIRequirementGroup : AIListRequirementGroup
{
    public override bool IsFulfilledBy(IEnumerable<string> values)
    {
        return RequiredAIs.All(ai => values.Any(v => v == ai));
    }
}

public class DisjuctionAIRequirementGroup : AIListRequirementGroup
{
    public override bool IsFulfilledBy(IEnumerable<string> values)
    {
        return RequiredAIs.Any(ai => values.Any(v => v == ai));
    }
}

public class RangeAIRequirementGroup : AIRequirementGroup
{
    public int Start { get; set; }
    public int End { get; set; }

    public override bool IsFulfilledBy(IEnumerable<string> values)
    {
        return values.Select(int.Parse).Any(v => v >= Start && v <= End);
    }
}

public class KeyQualifierConverter : JsonConverter<KeyQualifiers>
{
    public override KeyQualifiers? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var qualifierLists = new List<string[]>();
        var groups = new List<string>();
        var depth = 1;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                groups.Add(reader.GetString()!);
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                groups = [];
                depth++;
            }
            else if (reader.TokenType == JsonTokenType.EndArray)
            {
                depth--;

                qualifierLists.Add([.. groups]);

                if (depth == 0) break;
            }
        }

        return new KeyQualifiers { AllowedQualifiers = qualifierLists };
    }

    public override void Write(Utf8JsonWriter writer, KeyQualifiers value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class RequirementConverter<T> : JsonConverter<AIRequirements> where T : AIListRequirementGroup, new()
{
    public override AIRequirements? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var groups = new List<AIRequirementGroup>();
        var group = new T();
        var depth = 1;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (depth == 1)
                {
                    groups.Add(new DisjuctionAIRequirementGroup() { RequiredAIs = [reader.GetString()!] });
                }
                else
                {
                    group.RequiredAIs.Add(reader.GetString()!);
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                var start = 0;
                var end = 0;

                while (reader.TokenType != JsonTokenType.EndObject)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "start")
                    {
                        reader.Read();
                        start = int.Parse(reader.GetString() ?? "");
                    }
                    else if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "end")
                    {
                        reader.Read();
                        end = int.Parse(reader.GetString() ?? "");
                    }
                }

                if(start > 0 && end > 0 && start < end)
                {
                    groups.Add(new RangeAIRequirementGroup() { Start = start, End = end });
                }
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                group = new T();
                depth++;
            }
            else if (reader.TokenType == JsonTokenType.EndArray)
            {
                depth--;

                if (group.RequiredAIs.Count > 0) { 
                groups.Add(group);}

                if (depth == 0) break;
            }
        }

        return new AIRequirements { Groups = [.. groups] };
    }

    public override void Write(Utf8JsonWriter writer, AIRequirements value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}