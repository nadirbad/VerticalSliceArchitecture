using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace VerticalSliceArchitecture.Application.IntegrationTests;

/// <summary>
/// Pins the public HTTP contract to a committed snapshot.
/// </summary>
/// <remarks>
/// Renaming a DTO property, changing a status code, or dropping an endpoint all compile cleanly
/// and pass every other test in this repo, while silently breaking every client. This test is the
/// only thing that notices.
/// <para>
/// To accept an intentional contract change, regenerate the snapshot and review the diff:
/// <c>UPDATE_SNAPSHOTS=1 dotnet test tests/Application.IntegrationTests</c>.
/// </para>
/// </remarks>
public class OpenApiContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OpenApiContractTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenApiDocument_MatchesCommittedSnapshot()
    {
        // Arrange
        var snapshotPath = SnapshotPath();

        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var actual = Normalize(await response.Content.ReadAsStringAsync());

        // Assert
        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, actual);
            return;
        }

        File.Exists(snapshotPath).Should().BeTrue(
            "the OpenAPI snapshot is missing - regenerate it with UPDATE_SNAPSHOTS=1 dotnet test");

        var expected = Normalize(await File.ReadAllTextAsync(snapshotPath));

        const string Because = "the HTTP contract must not change by accident. If this change is "
            + "intended, regenerate the snapshot with UPDATE_SNAPSHOTS=1 dotnet test and review "
            + "the diff before committing it";

        actual.Should().Be(expected, Because);
    }

    // Re-serializes the document with object keys sorted and a fixed indentation so that the
    // comparison is stable: Swashbuckle does not guarantee property order between runs, and line
    // endings differ across the operating systems this repo builds on.
    private static string Normalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            WriteSorted(document.RootElement, writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan).ReplaceLineEndings("\n");
    }

    private static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSorted(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();

                foreach (var item in element.EnumerateArray())
                {
                    WriteSorted(item, writer);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    // Resolved from this source file's compile-time path so the snapshot can be rewritten in place
    // rather than in the test output directory.
    private static string SnapshotPath([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "OpenApi", "swagger.v1.snapshot.json");
}