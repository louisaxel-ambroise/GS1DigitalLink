using GS1DigitalLink.Compression;
using GS1DigitalLink.Model;
using GS1DigitalLink.Utils;
using System.Text.Json;

var optimizationCodes = JsonSerializer.Deserialize<StoredOptimisationCodes>(File.OpenRead("Documents/OptimizationCodes.json"))!;
var applicationIdentifiers = JsonSerializer.Deserialize<GS1Identifiers>(File.OpenRead("Documents/ApplicationIdentifiers.json"))!;

var gs1CompressionOptions = new GS1CompressionOptions
{
    OptimizationCodes = optimizationCodes,
    ApplicationIdentifiers = applicationIdentifiers
};

var compressor = new Compressor(gs1CompressionOptions);

var c = compressor.Compress([new("414", "0124585421606"), new("254", "502305"), new("3322", "502305")]);
Console.WriteLine("Compressing: 414/0124585421606/254/502305/3322/502305");
Console.WriteLine(c);
Console.WriteLine("----");