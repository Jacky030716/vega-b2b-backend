using System.Net;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Infrastructure.Persistence.Services.AI;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Audit;

public class GoogleAiServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public Queue<HttpResponseMessage> Responses { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (Responses.Count > 0)
            {
                return Task.FromResult(Responses.Dequeue());
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Mocked response\"}]}}]}")
            });
        }
    }

    [Fact]
    public async Task GenerateJsonAsync_FallsBackCorrectly_ForGeminiFlashLite()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        // First request (gemini-3.1-flash-lite) fails
        handler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        // Second request (gemini-3.5-flash fallback) succeeds
        handler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Fallback success\"}]}}]}")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/") };
        var options = Options.Create(new GoogleAiOptions { ApiKey = "fake-api-key", ModelId = "gemini-3.1-flash-lite" });
        var service = new GoogleAiService(httpClient, options, NullLogger<GoogleAiService>.Instance);

        var request = new ChallengeGenerationRequest(
            Model: "gemini-3.1-flash-lite",
            SystemPrompt: "System prompt",
            UserPrompt: "User prompt",
            Temperature: 0.7,
            JsonMode: true
        );

        // Act
        var result = await service.GenerateJsonAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Fallback success", result.Result.RawResponse);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("models/gemini-3.1-flash-lite:generateContent", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("models/gemini-3.5-flash:generateContent", handler.Requests[1].RequestUri!.ToString());
    }
}
