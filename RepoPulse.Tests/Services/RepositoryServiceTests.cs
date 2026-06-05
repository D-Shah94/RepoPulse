using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RepoPulse.API.Data;
using RepoPulse.API.DTOs;
using RepoPulse.API.Services;

namespace RepoPulse.Tests.Services;

public class RepositoryServiceTests
{
    private readonly AppDbContext _context;
    private readonly RepositoryService _sut;

    public RepositoryServiceTests()
    {
        // 1. Setup a fresh, empty In-Memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // 2. Setup standard mocks
        var mockGitHub = new Mock<IGitHubService>();
        var mockParserLogger = new Mock<ILogger<DependencyParser>>();
        var parser = new DependencyParser(mockGitHub.Object, mockParserLogger.Object);
        var mockRepoLogger = new Mock<ILogger<RepositoryService>>();

        // 3. Inject them into our service using EXACTLY the 3 arguments your code asks for!
        _sut = new RepositoryService(_context, parser, mockRepoLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_SavesToDatabase()
    {
        // Arrange - Pass the arguments into the parentheses based on your DTO constructor
        var dto = new CreateRepositoryDto("facebook", "react", "A declarative UI library");

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("facebook", result.Owner);
        Assert.Equal("react", result.RepoName);

        // Assert: Verify it saved to the database correctly
        var dbRecord = await _context.TrackedRepositories.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal("facebook", dbRecord.Owner);
        Assert.Equal("react", dbRecord.RepoName);
    }
}
