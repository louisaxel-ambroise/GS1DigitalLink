using GS1DigitalLink.Model;

internal class ConsoleLoggerResult : DigitalLink
{
    public void OnParsedAI(string key, string value)
    {
        Console.WriteLine("[INF] AI(" + key + ", " + value + ")");
    }

    public void Log(string message)
    {
        Console.WriteLine("[LOG] " + message);
    }

    public void OnParsedScheme(string scheme)
    {
        Console.WriteLine("[INF] SCHEME(" + scheme + ")");
    }

    public void OnParsedHost(string host)
    {
        Console.WriteLine("[INF] HOST(" + host + ")");
    }

    public void OnParsedPath(string path)
    {
        Console.WriteLine("[INF] PATH(" + path + ")");
    }
    public void OnParsedQuery(string key, string? value)
    {
        Console.WriteLine("[INF] QUERY(" + key + ", " + value + ")");
    }

    public void OnError(string message)
    {
        Console.WriteLine("[ERR] " + message);
    }

    public void OnFatal(Exception exception)
    {
        Console.WriteLine("[FTL] " + exception.Message);
    }
}
