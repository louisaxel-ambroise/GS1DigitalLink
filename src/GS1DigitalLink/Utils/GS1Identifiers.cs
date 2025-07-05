using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GS1DigitalLink.Utils;

public record GS1Identifiers
{
    [JsonPropertyName("applicationIdentifiers")]
    public required IReadOnlyList<ApplicationIdentifier> ApplicationIdentifiers { get; init; }

    public Dictionary<string, int> CodeLength => ApplicationIdentifiers.GroupBy(x => x.Code[..2]).ToDictionary(x => x.Key, x => x.First().Code.Length);
}

public record ApplicationIdentifier
{
    public static ApplicationIdentifier None = new() { Code = "00", Pattern = "", Components = [] };

    [JsonPropertyName("applicationIdentifier")]
    public required string Code { get; init; }

    [JsonPropertyName("gs1DigitalLinkPrimaryKey")]
    public bool IsPrimaryKey { get; init; }

    [JsonPropertyName("components")]
    public required IReadOnlyList<AIComponent> Components { get; init; }

    [JsonPropertyName("regex")]
    public required string Pattern { get; set; }

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

        public string ReadFrom(BitStream inputStream)
        {
            var encoding = GetEncoding(Charset, inputStream);
            var length = GetBitsLength(inputStream);

            return encoding.Read(length, inputStream);
        }

        private int GetBitsLength(BitStream stream)
        {
            if (FixedLength)
            {
                return Length;
            }
            else
            {
                var lengthBits = (int)Math.Ceiling(Math.Log(Length) / Math.Log(2));
                stream.Buffer(lengthBits);

                return Convert.ToInt32(stream.Current, 2);
            }
        }

        private static Encodings GetEncoding(string charset, BitStream stream)
        {
            if (charset == "N")
            {
                return Encodings.Numeric;
            }
            else
            {
                stream.Buffer(3);

                var encodingIndex = Convert.ToInt32(stream.Current, 2);

                return Encodings.Values.ElementAt(encodingIndex);
            }
        }
    }

    internal string ReadFrom(BitStream inputStream)
    {
        var buffer = new StringBuilder();

        foreach (var component in Components)
        {
            buffer.Append(component.ReadFrom(inputStream));
        }

        return buffer.ToString();
    }

    public bool Validate(string value)
    {
        return Regex.IsMatch(value, $"^{Pattern}$");
    }
}