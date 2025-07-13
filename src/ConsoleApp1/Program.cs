using GS1DigitalLink.Model;
using GS1DigitalLink.Model.Algorithms;
using GS1DigitalLink.Processors;
using GS1DigitalLink.Utils;
using System.Text.Json;

var optimizationCodes = JsonSerializer.Deserialize<StoredOptimisationCodes>(File.OpenRead("Documents/OptimizationCodes.json"))!;
var applicationIdentifiers = JsonSerializer.Deserialize<GS1Identifiers>(File.OpenRead("Documents/ApplicationIdentifiers.json"))!;
var algorithm = new GS1AlgorithmV1(optimizationCodes.OptimizationCodes, applicationIdentifiers.ApplicationIdentifiers);

var digitalLinkParser = new DigitalLinkParser(algorithm);
var entries = new Entry[] { new("01", "07320582208001"), new("22", "2A"), new("10", "1234567890") };

var uncompressed = string.Join('/', entries.Select(e => string.Join('/', e.Key, e.Value)));
var compressed = algorithm.Format(entries, new() { CompressionType = DLCompressionType.Full });
Console.WriteLine("Compressing: " + uncompressed);
Console.WriteLine("Result       " + compressed);
Console.WriteLine("Compression: " + (100.0 - (compressed.Length * 100.0) / (uncompressed.Length * 1.0)) + "%");
Console.WriteLine("----");

var input = "http://id.fastnt.eu/" + compressed + "?lang=fr-BE&pageType=pip";
var parsed = digitalLinkParser.Parse(input);
var pparsed = "http://id.fastnt.eu/test/ai/with/path/01/07320582208001/22/2A/10/1234567890?lang=fr-BE&pageType=pip";
var reparsed = digitalLinkParser.Parse(pparsed);

Console.WriteLine(input);
Console.WriteLine(parsed.Result);
Console.WriteLine(reparsed.Result);

Console.ReadLine();