using GS1DigitalLink.Model;
using GS1DigitalLink.Model.Algorithms;
using GS1DigitalLink.Utils;
using System.Text.RegularExpressions;
using System.Web;

namespace GS1DigitalLink.Processors;

public sealed class DigitalLinkParser(IGS1Algorithm algorithm)
{
    public DigitalLink Parse(string input) => Parse(new Uri(input));

    public DigitalLink Parse(Uri input)
    {
        var builder = new DigitalLinkBuilder();

        ProcessUriPath(input.LocalPath, builder);
        ProcessQueryString(input.Query, builder);

        return builder.Build();
    }

    private void ProcessQueryString(string query, DigitalLinkBuilder result)
    {
        var keyValuePair = HttpUtility.ParseQueryString(query);

        foreach (var key in keyValuePair.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
        {
            var value = keyValuePair.Get(key);

            if (!string.IsNullOrEmpty(value) && algorithm.TryGetAI(key!, out var ai) && Regex.IsMatch(value, ai.Pattern))
            {
                result.Add(ai!, HttpUtility.UrlDecode(value));
            }
        }
    }

    private void ProcessUriPath(string absolutePath, DigitalLinkBuilder result)
    {
        var parts = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (MayBePartiallyCompressedDigitalLink(parts) && algorithm.TryGetAI(parts[^3], out var ai) && ai.IsPrimaryKey && ai.Validate(parts[^2]))
        {
            result.Add(ai, parts[^2]);
            ParseCompressedValue(parts[^1], result);
        }
        else if (MayBeUncompressedDigitalLink(parts, algorithm, out var registerAIs))
        {
            registerAIs(result);
        }
        else if (MayBeFullyCompressedDigitalLink(parts))
        {
            ParseCompressedValue(parts[^1], result);
        }
        else
        {
            throw new Exception("Not a valid DigitalLink");
        }
    }

    private void ParseCompressedValue(string compressedValue, DigitalLinkBuilder result)
    {
        var binaryStream = new BitStream(compressedValue);

        while (binaryStream.Remaining > 7)
        {
            binaryStream.Buffer(8);

            if (binaryStream.Current[..4] == "1101")
            {
                //algorithm = options.FindAlgorithm(binaryStream.Current[4..]);
            }
            else if (binaryStream.Current[..4] == "1110")
            {
                //algorithm = options.FindAlgorithm(binaryStream.Current[4..]) ?? 
                    throw new InvalidOperationException($"Unknown algorithm version {binaryStream.Current[4..]}");
            }
            else
            {
                algorithm.Parse(binaryStream, result);
            }
        }
    }

    private static bool MayBePartiallyCompressedDigitalLink(string[] parts)
    {
        return parts.Length >= 3 && parts[^1].IsUriSafeBase64();
    }

    private static bool MayBeFullyCompressedDigitalLink(string[] parts)
    {
        return parts.Length >= 1 && parts[^1].IsUriSafeBase64();
    }

    private static bool MayBeUncompressedDigitalLink(string[] parts, IGS1Algorithm algorithm, out Action<DigitalLinkBuilder> result)
    {
        var parsedAIs = new List<(ApplicationIdentifier, string)>();
        result = b => parsedAIs.ForEach((a) => b.Add(a.Item1, a.Item2));

        Func<string[], bool> matchAI = _ => false;
        
        matchAI = parts =>
        {
            if (parts.Length < 2 || !algorithm.TryGetAI(parts[^2], out var ai) || !ai.Validate(parts[^1]))
            {
                return false;
            }
            if(ai.IsPrimaryKey || matchAI(parts[..^2]))
            {
                parsedAIs.Add((ai, parts[^1]));
            }

            return parsedAIs.Count > 0;
        };

        return matchAI(parts);
    }
}
