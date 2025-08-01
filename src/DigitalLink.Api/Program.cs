using GS1DigitalLink.Model;
using GS1DigitalLink.Processors;
using GS1DigitalLink.Services.Algorithms;
using GS1DigitalLink.Utils;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.Json;
using static GS1DigitalLink.Model.KeyValue;

var optimizationCodes = JsonSerializer.Deserialize<StoredOptimisationCodes>(File.OpenRead("Documents/OptimizationCodes.json"))!;
var applicationIdentifiers = JsonSerializer.Deserialize<GS1Identifiers>(File.OpenRead("Documents/ApplicationIdentifiers.json"))!;
var algorithm = new GS1AlgorithmV1(optimizationCodes.OptimizationCodes, applicationIdentifiers.ApplicationIdentifiers);
var digitalLinkParser = new DigitalLinkParser(algorithm);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();

var apiEndpoints = app.MapGroup("/api");
apiEndpoints.MapGet("/", () => { return "GET /api"; });
apiEndpoints.MapPost("/", () => { return "POST /api"; });
apiEndpoints.MapPost("/compress", () =>
{
    var segments = new KeyValue[] { PrimaryKey("01", "07320582208002"), Qualifier("22", "2A"), Qualifier("10", "1234567890"), Qualifier("21", "2A") };

    return Results.Redirect(algorithm.Format(segments, new() { CompressionType = DLCompressionType.Full }), false, false);
});

app.MapHealthChecks("/");
app.MapGet("/{**_}", (HttpRequest request) =>
{
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
