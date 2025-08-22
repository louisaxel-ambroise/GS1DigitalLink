using GS1DigitalLink.Model;
using GS1DigitalLink.Processors;
using GS1DigitalLink.Services.Algorithms;
using GS1DigitalLink.Utils;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.Json;

var optimizationCodes = JsonSerializer.Deserialize<StoredOptimisationCodes>(File.OpenRead("Documents/OptimizationCodes.json"))!;
var applicationIdentifiers = JsonSerializer.Deserialize<GS1Identifiers>(File.OpenRead("Documents/ApplicationIdentifiers.json"))!;
var algorithm = new GS1AlgorithmV1(optimizationCodes.OptimizationCodes, applicationIdentifiers.ApplicationIdentifiers);
var digitalLinkParser = new DigitalLinkParser(algorithm);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddLogging(opt => opt.AddConsole());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();

var apiEndpoints = app.MapGroup("/api");
apiEndpoints.MapGet("/", () => { return "GET /api"; });
apiEndpoints.MapPost("/", () => { return "POST /api"; });
apiEndpoints.MapGet("/compress/{**link}", (string link, HttpRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation($"Compressing {link}");

    var dlUrl = request.GetEncodedUrl();
    var builder = digitalLinkParser.Parse(dlUrl);

    if(builder.Validate(applicationIdentifiers, out var digitalLink))
    {
        var entries = GetAllEntries(digitalLink!);

        return Results.Ok(new { Compressed = algorithm.Format(entries, new() { CompressionType = DLCompressionType.Full }) });
    }
    else
    {
        return Results.BadRequest("Invalid DL");
    }
});

static IEnumerable<KeyValue> GetAllEntries(DigitalLink digitalLink)
{
    yield return digitalLink.PrimaryKey;

    foreach(var qualifier in digitalLink.Qualifiers)
    {
        yield return qualifier;
    }

    foreach(var attribute in digitalLink.Attributes)
    {
        yield return attribute;
    }
}

app.MapHealthChecks("/");
app.MapGet("/{**link}", (string link, HttpRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation($"Finding matching redirections for {link}");

    var dlUrl = request.GetEncodedUrl();
    var builder = digitalLinkParser.Parse(dlUrl);

    if (builder.Validate(applicationIdentifiers, out var digitalLink))
    {
        return Results.Ok(digitalLink);
    }
    else
    {
        return Results.BadRequest(builder.GetErrorResult());
    }
});

app.Run();
