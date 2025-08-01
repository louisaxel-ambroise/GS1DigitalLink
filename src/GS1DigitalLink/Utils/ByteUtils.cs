namespace GS1DigitalLink.Utils;

static class ByteUtils
{
    internal static bool IsNumeric(this byte value)
    {
        return value >> 4 <= 9 && (value & 0x0F) <= 9;
    }
}