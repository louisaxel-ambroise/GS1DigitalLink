namespace GS1DigitalLink.Utils;

public static class Extensions
{
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T value)
    {
        return source.Where(x => !Equals(x, value));
    }
}