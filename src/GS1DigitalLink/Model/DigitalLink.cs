namespace GS1DigitalLink.Model;

public record DigitalLink
{
    public required KeyValue PrimaryKey { get; set; }
    public KeyValue[] Qualifiers { get; set; } = [];
    public KeyValue[] Attributes { get; set; } = [];
    public KeyValue[] QueryElements { get; set; } = [];
}

public record KeyValue(string Key, string Value, KeyValueType Type)
{
    public static KeyValue PrimaryKey(string key, string value) => new(key, value, KeyValueType.PrimaryKey);
    public static KeyValue Qualifier(string key, string value) => new(key, value, KeyValueType.Qualifier);
    public static KeyValue Attribute(string key, string value) => new(key, value, KeyValueType.Attribute);
    public static KeyValue QueryElement(string key, string value) => new(key, value, KeyValueType.QueryElement);
}

public enum KeyValueType
{
    PrimaryKey,
    Qualifier,
    Attribute,
    QueryElement
}

public record ErrorResult
{
    public List<string> Errors { get; set; } = [];
}
