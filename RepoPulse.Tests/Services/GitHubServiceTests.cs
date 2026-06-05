using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using RepoPulse.API.Options;
using RepoPulse.API.Services;
using System.Net;

namespace RepoPulse.Tests.Services;

public class GitHubServiceTests
{
    [Fact]
    public async Task GetApiHealthStatusAsync_WhenApiReachable_ReturnsHealthyStatus()
    {
        // 1. Arrange: Fake the HTTP response from GitHub
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                // Simulate GitHub's rate limit headers
                Headers = {
                    { "X-RateLimit-Remaining", "59" },
                    { "X-RateLimit-Limit", "60" }
                }
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com")
        };

        // 2. Arrange: Setup standard dependencies
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new GitHubApiOptions { CacheTtlMinutes = 60 });
        var loggerMock = new Mock<ILogger<GitHubService>>();

        var sut = new GitHubService(httpClient, cache, options, loggerMock.Object);

        // 3. Act
        var result = await sut.GetApiHealthStatusAsync();

        // 4. Assert
        Assert.NotNull(result);
        Assert.True(result.IsReachable);
        Assert.Equal(59, result.RateLimitRemaining);
        Assert.Equal(60, result.RateLimitTotal);
    }
}
