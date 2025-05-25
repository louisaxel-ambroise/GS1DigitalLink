namespace GS1DigitalLink.Utils
{
    static class StringUtils
    {
        internal static bool IsLowerCaseHex(this string value)
            => value.All(x => (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f'));

        internal static bool IsUpperCaseHex(this string value) 
            => value.All(x => (x >= '0' && x <= '9') || (x >= 'A' && x <= 'F'));

        internal static bool IsUriSafeBase64(this string value) 
            => value.All(x => (x >= '0' && x <= 'z') || x == '-' || x == '_');

        internal static bool IsNumeric(this string value) 
            => value.All(x => x >= '0' && x <= '9');
    }
}
