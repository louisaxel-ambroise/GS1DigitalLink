using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;
using System.Text.RegularExpressions;
using System.Web;

namespace GS1DigitalLink.Decompression;

public partial class Decompressor(DigitalLink result, GS1DigitalLinkOptions options)
{
    public void Decompress(string input)
    {
        var uri = new Uri(input);

        Decompress(uri);
    }
    public void Decompress(Uri input)
    {
        result.OnParsedScheme(input.Scheme);
        result.OnParsedHost(input.Host);

        ProcessUriPath(input.AbsolutePath);
        ProcessQueryStrings(input.Query);
    }

    private void ProcessQueryStrings(string query)
    {
        foreach (var queryPart in query.TrimStart('?').Split(['&', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var paramParts = queryPart.Split("=", 2, StringSplitOptions.RemoveEmptyEntries);
            var identifier = options.ApplicationIdentifiers.Find(paramParts[0]);

            if (identifier is not null && Regex.IsMatch(paramParts[1], identifier.Pattern))
            {
                result.OnParsedAI(paramParts[0], HttpUtility.UrlDecode(paramParts[1]));
            }
            else
            {
                result.OnParsedQuery(paramParts[0], paramParts[1]);
            }
        }
    }

    private void ProcessUriPath(string absolutePath)
    {
        var parts = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (IsPartiallyCompressedDigitalLink(parts, options))
        {
            if (parts.Length > 3)
            {
                result.OnParsedPath(string.Join('/', parts[..^3]));
            }

            result.OnParsedAI(parts[^3], parts[^2]);
            ParseCompressedValue(parts[^1]);
        }
        else if (IsUncompressedDigitalLink(parts, options))
        {
            for (var i = 0; i <= parts.Length - 2; i += 2)
            {
                result.OnParsedAI(parts[i], parts[i + 1]);
            }
        }
        else if (IsFullyCompressedDigitalLink(parts))
        {
            if (parts.Length > 1)
            {
                result.OnParsedPath(string.Join('/', parts[..^1]));
            }

            ParseCompressedValue(parts[^1]);
        }
        else
        {
            throw new InvalidOperationException("Not a DigitalLink");
        }
    }

    private void ParseCompressedValue(string compressedValue)
    {
        var algorithm = options.Algorithms.Default;
        var binaryStream = new BitStream(compressedValue);

        while (binaryStream.Position + 7 < binaryStream.Length)
        {
            binaryStream.Buffer(8);

            if (binaryStream.Current[..4] == "1101")
            {
                result.Log("GS1 Algorithm version change");
                algorithm = options.Algorithms.Find(binaryStream.Current[4..])
                    ?? throw new InvalidOperationException("Unknown algorithm version " + binaryStream.Current[4..]);
            }
            else if (binaryStream.Current[..4] == "1110")
            {
                result.Log("Proprietary Algorithm version selection");
                // TODO: allow to specify and handle proprietary algorithms

                result.OnError("Unknown proprietary compression algorithm");
                throw new InvalidOperationException("Unknown algorithm version " + binaryStream.Current[4..]);
            }
            else
            {
                algorithm.Decompress(binaryStream, result, options);
            }
        }
    }

    private static bool IsPartiallyCompressedDigitalLink(string[] parts, GS1DigitalLinkOptions options)
    {
        return parts.Length >= 3 && parts[^1].IsUriSafeBase64() && options.ApplicationIdentifiers.Validate(parts[^3], parts[^2]);
    }

    private static bool IsFullyCompressedDigitalLink(string[] parts)
    {
        return parts.Length >= 1 && parts[^1].IsUriSafeBase64();
    }

    private static bool IsUncompressedDigitalLink(string[] parts, GS1DigitalLinkOptions options)
    {
        if (parts.Length < 2)
        {
            return false;
        }

        for (var i = 1; i < parts.Length; i += 2)
        {
            if (!options.ApplicationIdentifiers.Validate(parts[^(i + 1)], parts[^i]))
            {
                return false;
            }
        }

        return true;
    }
}
