namespace RepoPulse.API.Models
{
    /// <summary>
    /// Represents a point-in-time capture of a repository's dependency manifest file.
    /// Retaining snapshots allows for historical comparison and trend analysis.
    /// </summary>
    public class DependencySnapshot
    {
        public int Id { get; set; }
        public int RepositoryId { get; set; }

        /// <summary>
        /// The specific file parsed during this snapshot (e.g., package.json, .csproj).
        /// </summary>
        public required string ManifestFile { get; set; }
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
        public virtual TrackedRepository Repository { get; set; } = null!;
        public virtual ICollection<DependencyEntry> Dependencies { get; set; } = new List<DependencyEntry>();
    }
}
