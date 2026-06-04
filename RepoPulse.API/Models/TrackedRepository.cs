namespace RepoPulse.API.Models
{
    /// <summary>
    /// Represents a GitHub repository registered for dependency tracking.
    /// </summary>
    public class TrackedRepository
    {
        public int Id { get; set; }
        public required string Owner { get; set; }

        public required string RepoName { get; set; }

        public string? Description { get; set; }
        
        /// <summary>
        /// The timestamp of the most recent successful dependency fetch. 
        /// Null indicates the repository is registered but has not yet been scanned.
        /// </summary>
        public DateTime? LastFetchedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<DependencySnapshot> DependencySnapshots { get; set; } = new List<DependencySnapshot>();
    }

}
