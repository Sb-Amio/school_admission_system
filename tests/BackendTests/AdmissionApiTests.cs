using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BackendTests;

public class AdmissionApiTests : IDisposable
{
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri("http://localhost:8080")
    };

    private async Task<string> CreateClassAsync(int seats)
    {
        string className = "Test-" + Guid.NewGuid().ToString("N");

        using var response = await _client.PostAsJsonAsync(
            "/classes",
            new
            {
                name = className,
                totalSeats = seats
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var data = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        return data.GetProperty("id").GetString()!;
    }

    private async Task<HttpStatusCode> ApplyAsync(
        string classId,
        string studentId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/classes/{classId}/holds");

        request.Headers.Add("X-Student-Id", studentId);

        using var response = await _client.SendAsync(request);

        return response.StatusCode;
    }

    [Fact]
    public async Task CreateClass_ShouldSaveClass()
    {
        string classId = await CreateClassAsync(10);

        var schoolClass = await _client
            .GetFromJsonAsync<JsonElement>(
                $"/classes/{classId}");

        Assert.Equal(
            classId,
            schoolClass.GetProperty("id").GetString());

        Assert.Equal(
            10,
            schoolClass.GetProperty("totalSeats").GetInt32());

        Assert.Equal(
            10,
            schoolClass.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task DuplicateApplication_ShouldReturnConflict()
    {
        string classId = await CreateClassAsync(5);
        string studentId = "91234567890123456";

        var first = await ApplyAsync(classId, studentId);
        var second = await ApplyAsync(classId, studentId);

        Assert.Equal(HttpStatusCode.Created, first);
        Assert.Equal(HttpStatusCode.Conflict, second);

        var schoolClass = await _client
            .GetFromJsonAsync<JsonElement>(
                $"/classes/{classId}");

        Assert.Equal(
            1,
            schoolClass.GetProperty("heldSeats").GetInt32());

        Assert.Equal(
            4,
            schoolClass.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task ConcurrentApplications_ShouldNotOverbook()
    {
        string classId = await CreateClassAsync(3);

        var tasks = Enumerable.Range(0, 20)
            .Select(i =>
            {
                string studentId = "8" + i.ToString("D16");
                return ApplyAsync(classId, studentId);
            });

        var results = await Task.WhenAll(tasks);

        int successful = results.Count(
            r => r == HttpStatusCode.Created);

        int rejected = results.Count(
            r => r == HttpStatusCode.Conflict);

        Assert.Equal(3, successful);
        Assert.Equal(17, rejected);

        var schoolClass = await _client
            .GetFromJsonAsync<JsonElement>(
                $"/classes/{classId}");

        Assert.Equal(
            3,
            schoolClass.GetProperty("heldSeats").GetInt32());

        Assert.Equal(
            0,
            schoolClass.GetProperty("availableSeats").GetInt32());
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
