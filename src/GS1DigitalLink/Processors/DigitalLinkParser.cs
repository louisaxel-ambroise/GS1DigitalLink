using GS1DigitalLink.Model;
using GS1DigitalLink.Model.Algorithms;
using GS1DigitalLink.Utils;
using System.Web;

namespace GS1DigitalLink.Processors;

public sealed class DigitalLinkParser(IDLAlgorithm algorithm)
{
    const char PathDelimiter = '/';

    public DigitalLink Parse(string input) => Parse(new Uri(input));

    public DigitalLink Parse(Uri input)
    {
        var builder = new DigitalLinkBuilder();

        ProcessUriPath(input.LocalPath.Trim(PathDelimiter), builder);
        ProcessQueryString(input.Query, builder);

        return builder.Build();
    }

    private void ProcessQueryString(string query, DigitalLinkBuilder result)
    {
        var keyValuePair = HttpUtility.ParseQueryString(query);

        foreach (var key in keyValuePair.AllKeys)
        {
            var value = keyValuePair.Get(key) ?? string.Empty;

            if (algorithm.TryGetDataAttribute(key, out var ai) && ai.Validate(value))
            {
                result.Set(ai!, HttpUtility.UrlDecode(value), IdentifierType.Attribute);
            }
        }
    }

    private void ProcessUriPath(string absolutePath, DigitalLinkBuilder result)
    {
        var parts = absolutePath.Split(PathDelimiter);

        if (MayBePartiallyCompressedDigitalLink(parts) && algorithm.TryGetQualifier(parts[^3], out var ai) && ai.IsPrimaryKey && ai.Validate(parts[^2]))
        {
            result.Set(ai, parts[^2], IdentifierType.Qualifier);
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

    /// <summary>
    /// Verifies if 
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    private static bool MayBeFullyCompressedDigitalLink(string[] parts)
    {
        return parts.Length >= 1 && parts[^1].IsUriSafeBase64();
    }

    /// <summary>
    /// Recursive method to find a potential uncompressed DigitalLink.
    /// The method scans through pair of query path elements and verifies if it is a potential valid AI key/value pair.
    /// It stops as soon as it encounters an invalid pair or a PrimaryKey AI.
    /// </summary>
    /// <param name="parts">The parts retrieved from the URL being parsed</param>
    /// <param name="algorithm">The DL algorithm (GS1 or proprietary) to use for parsing</param>
    /// <param name="result">An action that registers all successfully parsed AIs to the DigitalLinkBuilder instance</param>
    /// <returns>If the specified parts might be part of an uncompressed GS1 DL URI</returns>
    private static bool MayBeUncompressedDigitalLink(string[] parts, IDLAlgorithm algorithm, out Action<DigitalLinkBuilder> result)
    {
        var parsedAIs = new List<(ApplicationIdentifier, string)>();
        result = b => parsedAIs.ForEach((a) => b.Set(a.Item1, a.Item2, IdentifierType.Qualifier));

        Func<string[], bool> matchAI = _ => false;
        
        matchAI = parts =>
        {
            if (parts.Length < 2 || !algorithm.TryGetQualifier(parts[^2], out var ai) || !ai.Validate(parts[^1]))
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
