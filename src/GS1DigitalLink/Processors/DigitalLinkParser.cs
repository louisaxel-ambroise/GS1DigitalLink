using GS1DigitalLink.Model;
using GS1DigitalLink.Services;
using GS1DigitalLink.Services.Algorithms;
using GS1DigitalLink.Utils;
using System.Web;

namespace GS1DigitalLink.Processors;

public sealed class DigitalLinkParser(IDLAlgorithm algorithm)
{
    const char PathDelimiter = '/';

    public DigitalLinkBuilder Parse(string input) => Parse(new Uri(input));

    public DigitalLinkBuilder Parse(Uri input)
    {
        return DigitalLinkBuilder.Create()
            .AddRange(ProcessUriPath(input.LocalPath.Trim(PathDelimiter), algorithm))
            .AddRange(ProcessQueryString(input.Query, algorithm));
    }

    private static IEnumerable<KeyValue> ProcessQueryString(string query, IDLAlgorithm algorithm)
    {
        var keyValuePair = HttpUtility.ParseQueryString(query);

        return keyValuePair.AllKeys.Where(x => !string.IsNullOrEmpty(x)).Select(key =>
        {
            var value = keyValuePair.Get(key) ?? string.Empty;

            return algorithm.TryGetDataAttribute(key, out var ai) && ai.Validate(value)
                ? KeyValue.Attribute(key, value)
                : KeyValue.QueryElement(key, value);
        });
    }

    private static IEnumerable<KeyValue> ProcessUriPath(string absolutePath, IDLAlgorithm algorithm)
    {
        var result = new List<KeyValue>();
        var parts = absolutePath.Split(PathDelimiter);
        var lastPartMayBeCompressed = parts[^1].IsUriSafeBase64();

        if (lastPartMayBeCompressed && parts.Length >= 3 && algorithm.TryGetQualifier(parts[^3], out var ai) && ai.IsPrimaryKey && ai.Validate(parts[^2]))
        {
            result.Add(KeyValue.PrimaryKey(parts[^3], parts[^2]));
            result.AddRange(ParseCompressedValue(parts[^1], algorithm));
        }
        else if (MayBeUncompressedDigitalLink(parts, algorithm, out var registerAIs))
        {
            registerAIs(result);
        }
        else if (lastPartMayBeCompressed)
        {
            result.AddRange(ParseCompressedValue(parts[^1], algorithm));
        }
        else
        {
            throw new Exception("Not a valid DigitalLink");
        }

        return result;
    }

    private static IEnumerable<KeyValue> ParseCompressedValue(string compressedValue, IDLAlgorithm algorithm)
    {
        var result = new List<KeyValue>();
        var binaryStream = new BitStream(compressedValue);

        while (binaryStream.Remaining > 7)
        {
            binaryStream.Buffer(8);

            if (binaryStream.Current[..4] == "1101")
            {
                var version = binaryStream.Current[4..].ToString();
                algorithm.UseAlgorithm(version, AlgorithmType.GS1);
            }
            else if (binaryStream.Current[..4] == "1110")
            {
                var version = binaryStream.Current[4..].ToString();
                algorithm.UseAlgorithm(version, AlgorithmType.Proprietary);
            }
            else
            {
                result.AddRange(algorithm.Parse(binaryStream));
            }
        }

        return result;
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
    private static bool MayBeUncompressedDigitalLink(string[] parts, IDLAlgorithm algorithm, out Action<List<KeyValue>> result)
    {
        var parsedAIs = new List<KeyValue>();
        result = b => b.AddRange(parsedAIs);

        Func<string[], bool> matchAI = _ => false;
        
        matchAI = parts =>
        {
            if (parts.Length < 2 || !algorithm.TryGetQualifier(parts[^2], out var ai) || !ai.Validate(parts[^1]))
            {
                return false;
            }
            if(ai.IsPrimaryKey || matchAI(parts[..^2]))
            {
                parsedAIs.Add(ai.IsPrimaryKey
                    ? KeyValue.PrimaryKey(parts[^2], parts[^1])
                    : KeyValue.Qualifier(parts[^2], parts[^1]));
            }

            return parsedAIs.Count > 0;
        };

        return matchAI(parts);
    }
}
