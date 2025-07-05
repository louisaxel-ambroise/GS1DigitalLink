namespace GS1DigitalLink.Utils;

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

public static class CheckDigitHelper
{
    public static void EnsureIsValid(string input)
    {
        var weightedSum = 0;

        for (var i = 0; i < input.Length-1; i++)
        {
            var weight = i % 2 == 0 ? 3 : 1;
            weightedSum += (input[i] - '0') * weight;
        }

        var checkDigit = (10 - weightedSum % 10);

        if(checkDigit % 10 != (input[^1] - '0'))
        {
            throw new Exception($"AI Component has invalid check digit. Expected {checkDigit % 10} but got {input[^1]}");
        }
    }
}