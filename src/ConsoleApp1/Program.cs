using GS1DigitalLink.Compression;
using GS1DigitalLink.Model;
using GS1DigitalLink.Model.Algorithms;
using GS1DigitalLink.Processors;
using GS1DigitalLink.Utils;
using System.Text.Json;

var optimizationCodes = JsonSerializer.Deserialize<StoredOptimisationCodes>(File.OpenRead("Documents/OptimizationCodes.json"))!;
var applicationIdentifiers = JsonSerializer.Deserialize<GS1Identifiers>(File.OpenRead("Documents/ApplicationIdentifiers.json"))!;
var algorithms = new[] { new GS1AlgorithmV1(optimizationCodes.OptimizationCodes, applicationIdentifiers.ApplicationIdentifiers) };

var gs1CompressionOptions = new ParserOptions(algorithms);

var compressor = new Compressor();
var digitalLinkParser = new DigitalLinkParser(gs1CompressionOptions);

var c = compressor.Compress([new("414", "0124585421602"), new("254", "502305"), new("3322", "502305")], gs1CompressionOptions.DefaultAlgorithm);
Console.WriteLine("Compressing: (414)0124585421606(254)502305(3322)502305");
Console.WriteLine(c);
Console.WriteLine("----");

//decompressor.Decompress("http://id.fastnt.eu/gs1/dl/" + c);
digitalLinkParser.Parse("http://id.fastnt.eu/gs1/dl/nQHQHeqyIGeqITMieqIQ?lang=fr-BE&pageType=pip", new ConsoleLoggerResult());
