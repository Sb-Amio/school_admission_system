// // See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:8080")
};
var className = "Concurrency Test " + Guid.NewGuid();

var createResponse = await client.PostAsJsonAsync(
    "/classes",
    new
    {
        name = className,
        totalSeats = 3
    });

createResponse.EnsureSuccessStatusCode();

var createdClass = await createResponse.Content
    .ReadFromJsonAsync<JsonElement>();

string classId = createdClass.GetProperty("id").GetString()!;

Console.WriteLine($"Created Class: {classId}");
Console.WriteLine("Sending 20 concurrent requests...");

string studentPrefix = "7" + DateTime.UtcNow.ToString("MMddHHmmss");

var tasks = Enumerable.Range(0, 20).Select(async i =>
{
    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        $"/classes/{classId}/holds");

    string studentId = studentPrefix + i.ToString("D6");

    request.Headers.Add("X-Student-Id", studentId);

    using var response = await client.SendAsync(request);

    return response.StatusCode;
});

var results = await Task.WhenAll(tasks);

int successful = results.Count(
    r => r == HttpStatusCode.Created);

int rejected = results.Count(
    r => r == HttpStatusCode.Conflict);

int unexpected = results.Length - successful - rejected;

Console.WriteLine();
Console.WriteLine("=== CONCURRENCY TEST RESULTS ===");
Console.WriteLine($"Total Requests   : {results.Length}");
Console.WriteLine($"Successful Holds : {successful}");
Console.WriteLine($"Rejected Requests: {rejected}");
Console.WriteLine($"Unexpected Status: {unexpected}");

var classResult = await client.GetFromJsonAsync<JsonElement>(
    $"/classes/{classId}");

int heldSeats = classResult.GetProperty("heldSeats").GetInt32();
int availableSeats = classResult.GetProperty("availableSeats").GetInt32();
int confirmedSeats = classResult.GetProperty("confirmedSeats").GetInt32();

Console.WriteLine($"Held Seats       : {heldSeats}");
Console.WriteLine($"Confirmed Seats  : {confirmedSeats}");
Console.WriteLine($"Available Seats  : {availableSeats}");

bool passed =
    successful == 3 &&
    rejected == 17 &&
    unexpected == 0 &&
    heldSeats == 3 &&
    confirmedSeats == 0 &&
    availableSeats == 0;

Console.WriteLine();
Console.WriteLine(passed
    ? "TEST PASSED - No overbooking detected"
    : "TEST FAILED - Check seat allocation logic");

if (!passed)
{
    Environment.ExitCode = 1;
}
