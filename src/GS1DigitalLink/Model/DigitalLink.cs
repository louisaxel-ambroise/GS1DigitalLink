namespace GS1DigitalLink.Model;

public interface DigitalLink
{
    public void OnParsedAI(string key, string value);
    public void Log(string message);
    public void OnParsedScheme(string scheme);
    public void OnParsedHost(string host);
    public void OnParsedPath(string path);
    public void OnParsedQuery(string key, string? value);
    public void OnError(string message);
    public void OnFatal(Exception exception);
}
