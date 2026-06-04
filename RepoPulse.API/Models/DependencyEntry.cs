namespace RepoPulse.API.Models
{
    /// <summary>
    /// Represents a single dependency package found within a manifest file during a snapshot fetch.
    /// </summary>
    public class DependencyEntry
    {
        public int Id { get; set; }
        public int SnapshotId { get; set; }
        public required string PackageName { get; set; }
        public required string Version { get; set; }
        public required string PackageType { get; set; }
        public virtual DependencySnapshot DependencySnapshot { get; set; } = null!;
    }
}
