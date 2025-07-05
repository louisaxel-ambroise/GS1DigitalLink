using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;
using System.Text.RegularExpressions;
using System.Web;

namespace GS1DigitalLink.Processors;

public sealed class DigitalLinkParser(ParserOptions options)
{
    public void Parse(string input, DigitalLink result) => Parse(new Uri(input), result);

    public void Parse(Uri input, DigitalLink result)
    {
        result.OnParsedScheme(input.Scheme);
        result.OnParsedHost(input.Host);

        ProcessUriPath(input.AbsolutePath, result);
        ProcessQueryString(input.Query, result);
    }

    private void ProcessQueryString(string query, DigitalLink result)
    {
        var keyValuePair = HttpUtility.ParseQueryString(query);

        foreach (var key in keyValuePair.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
        {
            var value = keyValuePair.Get(key);

            if (!string.IsNullOrEmpty(value) && options.DefaultAlgorithm.TryGetAI(key!, out var ai) && Regex.IsMatch(value, ai.Pattern))
            {
                result.OnParsedAI(key!, HttpUtility.UrlDecode(value));
            }
            else
            {
                result.OnParsedQuery(key!, value);
            }
        }
    }

    private void ProcessUriPath(string absolutePath, DigitalLink result)
    {
        var parts = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (MayBePartiallyCompressedDigitalLink(parts, options))
        {
            if (parts.Length > 3)
            {
                result.OnParsedPath(string.Join('/', parts[..^3]));
            }

            result.OnParsedAI(parts[^3], parts[^2]);
            ParseCompressedValue(parts[^1], result);
        }
        else if (MayBeUncompressedDigitalLink(parts, options, out var startIndex))
        {
            if(startIndex > 0)
            {
                result.OnParsedPath(string.Join('/', parts[..startIndex]));
            }

            for (var i = startIndex; i <= parts.Length - 2; i += 2)
            {
                result.OnParsedAI(parts[i], parts[i + 1]);
            }
        }
        else if (MayBeFullyCompressedDigitalLink(parts))
        {
            if (parts.Length > 1)
            {
                result.OnParsedPath(string.Join('/', parts[..^1]));
            }

            ParseCompressedValue(parts[^1], result);
        }
        else
        {
            result.OnError("Not a valid DigitalLink");
        }
    }

    private void ParseCompressedValue(string compressedValue, DigitalLink result)
    {
        var algorithm = options.DefaultAlgorithm;
        var binaryStream = new BitStream(compressedValue);

        while (binaryStream.Position + 7 < binaryStream.Length)
        {
            binaryStream.Buffer(8);

            if (binaryStream.Current[..4] == "1101")
            {
                result.Log("GS1 Algorithm version change");
                algorithm = options.FindAlgorithm(binaryStream.Current[4..]);
            }
            else if (binaryStream.Current[..4] == "1110")
            {
                result.Log("Proprietary Algorithm version selection: " + binaryStream.Current[4..]);

                algorithm = options.FindAlgorithm(binaryStream.Current[4..])
                    ?? throw new InvalidOperationException("Unknown algorithm version " + binaryStream.Current[4..]);
            }
            else
            {
                try
                {
                    algorithm.Parse(binaryStream, result, options);
                }
                catch (Exception ex)
                {
                    result.OnFatal(ex);
                    return;
                }
            }
        }
    }

    private static bool MayBePartiallyCompressedDigitalLink(string[] parts, ParserOptions options)
    {
        if (parts.Length >= 3 && parts[^1].IsUriSafeBase64())
        {
            return options.DefaultAlgorithm.TryGetAI(parts[^3], out var ai) && ai.IsPrimaryKey && ai.Validate(parts[^2]);
        }

        return false;
    }

    private static bool MayBeFullyCompressedDigitalLink(string[] parts)
    {
        return parts.Length >= 1 && parts[^1].IsUriSafeBase64();
    }

    private static bool MayBeUncompressedDigitalLink(string[] parts, ParserOptions options, out int index)
    {
        for (index = parts.Length - 2; index > 0; index -= 2)
        {
            if (!options.DefaultAlgorithm.TryGetAI(parts[index], out var ai))
            {
                return false;
            }
            if (!ai.Validate(parts[index+1]))
            {
                return false;
            }
            if (ai.IsPrimaryKey)
            {
                return true;
            }
        }

        return false;
    }
}
