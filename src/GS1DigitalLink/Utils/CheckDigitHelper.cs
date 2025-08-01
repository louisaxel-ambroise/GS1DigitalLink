namespace GS1DigitalLink.Utils;

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