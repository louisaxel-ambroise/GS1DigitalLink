namespace GS1DigitalLink.Utils
{
    public static class Characters
    {
        public const string Empty = "000000";
        public static string GetBinary(char input) => Convert.ToString(Base64UrlSafe.IndexOf(input, StringComparison.Ordinal), 2).PadLeft(6, '0');
        public static char GetChar(string input) => Base64UrlSafe.ElementAt(Convert.ToInt32(input, 2));
        public static char GetAlpha(string input) => Alpha.ElementAt(Convert.ToInt32(input, 2));
        public static string GetAlphaBinary(char input) => Convert.ToString(Alpha.IndexOf(input, StringComparison.OrdinalIgnoreCase), 2).PadLeft(4, '0');

        private static readonly string Alpha = "0123456789ABCDEF";
        private static readonly string Base64UrlSafe = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    }
}
