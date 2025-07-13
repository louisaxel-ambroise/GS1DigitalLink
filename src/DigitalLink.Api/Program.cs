using GS1DigitalLink.Model.Algorithms;
using GS1DigitalLink.Processors;
using GS1DigitalLink.Utils;
using System.Text.Json;

namespace DigitalLink.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();


            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var optimizationCodes = JsonSerializer.Deserialize<StoredOptimisationCodes>(File.OpenRead("Documents/OptimizationCodes.json"))!;
            var applicationIdentifiers = JsonSerializer.Deserialize<GS1Identifiers>(File.OpenRead("Documents/ApplicationIdentifiers.json"))!;
            var algorithm = new GS1AlgorithmV1(optimizationCodes.OptimizationCodes, applicationIdentifiers.ApplicationIdentifiers);
            var digitalLinkParser = new DigitalLinkParser(algorithm);

            var apiEndpoints = app.MapGroup("/api");
            apiEndpoints.MapGet("/", () => { return "GET all"; });

            app.MapGet("/{*digitalLink:regex(^(?!api\\/)+)}", (string digitalLink) =>
            {
                var parsed = digitalLinkParser.Parse($"https://id.fastnt.be/{digitalLink}");

                return parsed.Result;
            });

            app.Run();
        }
    }
}
