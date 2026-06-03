using Microsoft.EntityFrameworkCore;
using RepoPulse.API.Models;


namespace RepoPulse.API.Data
{
    /// <summary>
    /// Represents the database session and context for the application.
    /// Manages the entity objects during runtime and coordinates all database operations.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        /// <summary>
        /// Gets or sets the collection of repositories registered for dependency tracking.
        /// </summary>
        public DbSet<TrackedRepository> TrackedRepositories => Set<TrackedRepository>();

        /// <summary>
        /// Gets or sets the point-in-time snapshots of repository dependency manifests.
        /// </summary>
        public DbSet<DependencySnapshot> DependencySnapshots => Set<DependencySnapshot>();

        /// <summary>
        /// Gets or sets the individual package dependencies found across all snapshots.
        /// </summary>
        public DbSet<DependencyEntry> DependencyEntries => Set<DependencyEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // TrackedRepository configuration
            modelBuilder.Entity<TrackedRepository>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Owner)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.RepoName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.Description)
                    .HasMaxLength(500);

                entity.Property(r => r.CreatedAt)
                    .IsRequired();

                // Composite unique index prevents duplicate registration of the same repository
                entity.HasIndex(r => new { r.Owner, r.RepoName })
                    .IsUnique();
            });

            // DependencySnapshot configuration
            modelBuilder.Entity<DependencySnapshot>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.ManifestFile)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(s => s.Repository)
                    .WithMany(r => r.Snapshots)
                    .HasForeignKey(s => s.RepositoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DependencyEntry configuration
            modelBuilder.Entity<DependencyEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(e => e.Snapshot)
                    .WithMany(s => s.Dependencies)
                    .HasForeignKey(e => e.SnapshotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
