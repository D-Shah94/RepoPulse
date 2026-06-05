using Microsoft.Extensions.Logging;
using Moq;
using RepoPulse.API.Services;

namespace RepoPulse.Tests.Services;

public class DependencyParserTests
{
    private readonly DependencyParser _sut;

    public DependencyParserTests()
    {
        // Fake the dependencies required by your DependencyParser constructor
        var mockGitHub = new Mock<IGitHubService>();
        var mockLogger = new Mock<ILogger<DependencyParser>>();

        _sut = new DependencyParser(mockGitHub.Object, mockLogger.Object);
    }

    [Fact]
    public void ParseManifest_WithValidPackageJson_ReturnsCorrectDependencies()
    {
        // Arrange
        var fileName = "package.json";
        var fileContent = """
        {
          "dependencies": {
            "express": "^4.18.2",
            "cors": "2.8.5"
          },
          "devDependencies": {
            "jest": "^29.0.0"
          }
        }
        """;

        // Act - Convert the result to a list so we can assert against it easily
        var result = _sut.ParseManifest(fileName, fileContent).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // Updated to use your exact DTO property names (PackageName, PackageType)
        Assert.Contains(result, d => d.PackageName == "express" && d.Version == "^4.18.2" && d.PackageType == "npm");
        Assert.Contains(result, d => d.PackageName == "cors" && d.Version == "2.8.5" && d.PackageType == "npm");
        Assert.Contains(result, d => d.PackageName == "jest" && d.Version == "^29.0.0" && d.PackageType == "npm");
    }

    [Fact]
    public void ParseManifest_WithInvalidJson_ReturnsEmptyListWithoutCrashing()
    {
        var fileName = "package.json";
        var fileContent = "{ broken_json: true, missing_quotes }";

        var result = _sut.ParseManifest(fileName, fileContent);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
