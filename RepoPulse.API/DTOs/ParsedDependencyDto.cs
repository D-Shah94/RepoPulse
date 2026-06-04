namespace RepoPulse.API.DTOs
{
    // Used to replace the raw DependencyEntry, decouples parser from the database
    public record ParsedDependencyDto(
        string PackageName,
        string Version,
        string PackageType);

    // bundles the filename with its parsed depedencies
    public record ParsedManifest(string ManifestFile, IReadOnlyList<ParsedDependencyDto> Dependencies);
}
