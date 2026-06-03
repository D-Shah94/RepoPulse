using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepoPulse.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedRepositories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastFetchedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedRepositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DependencySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepositoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManifestFile = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependencySnapshots_TrackedRepositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "TrackedRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DependencyEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SnapshotId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependencyEntries_DependencySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "DependencySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DependencyEntries_SnapshotId",
                table: "DependencyEntries",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DependencySnapshots_RepositoryId",
                table: "DependencySnapshots",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedRepositories_Owner_RepoName",
                table: "TrackedRepositories",
                columns: new[] { "Owner", "RepoName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DependencyEntries");

            migrationBuilder.DropTable(
                name: "DependencySnapshots");

            migrationBuilder.DropTable(
                name: "TrackedRepositories");
        }
    }
}
