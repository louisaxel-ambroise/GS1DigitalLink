namespace GS1DigitalLink.Model;

public record DigitalLinkFormatterOptions
{
    public DLCompressionType CompressionType { get; set; }
}

public enum DLCompressionType
{
    Full,
    Partial
}
