<p align="center">
  <h1 align="center">RepoPulse</h1>
  <p align="center">A full-stack developer dashboard for tracking GitHub repository dependencies — built with ASP.NET Core, Blazor WebAssembly, EF Core, and Docker.</p>
  <p align="center">
    <a href="https://github.com/D-Shah94/RepoPulse/commits/main"><img alt="Commits" src="https://img.shields.io/github/commit-activity/t/D-Shah94/RepoPulse?label=commits&color=blue&kill_cache=1"/></a>
    <a href="https://github.com/D-Shah94/RepoPulse/blob/main/LICENSE"><img alt="License" src="https://img.shields.io/badge/license-MIT-green&kill_cache=1"/></a>
    <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-purple"/>
    <img alt="Blazor WASM" src="https://img.shields.io/badge/Blazor-WebAssembly-512BD4"/>
    <img alt="Docker" src="https://img.shields.io/badge/Docker-Compose-2496ED"/>
    <img alt="Platform" src="https://img.shields.io/badge/Hosted-Oracle%20Cloud-F80000"/>
  </p>
</p>



---

## Table of Contents

- [What is RepoPulse?](#what-is-repopulse)
- [The Problem It Solves](#the-problem-it-solves)
- [Unique Selling Point](#unique-selling-point)
- [Live Demo](#live-demo)
- [Features](#features)
- [System Architecture](#system-architecture)
- [Data Flow](#data-flow)
- [Entity Relationship Diagram](#entity-relationship-diagram)
- [Snapshot Archival Flow](#snapshot-archival-flow)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Key Engineering Decisions](#key-engineering-decisions)
- [Getting Started — Local Development](#getting-started--local-development)
- [Running with Docker](#running-with-docker)
- [Production Deployment (Oracle Cloud)](#production-deployment-oracle-cloud)
- [Configuration Reference](#configuration-reference)
- [API Endpoints](#api-endpoints)
- [Health Endpoint](#health-endpoint)
- [Testing](#testing)
- [Engineering Challenges & How They Were Solved](#engineering-challenges--how-they-were-solved)
- [Roadmap](#roadmap)
- [FAQ](#faq)
- [Contributing](#contributing)
- [License](#license)

---

## What is RepoPulse?

RepoPulse is a developer-focused dependency intelligence dashboard. It lets you maintain a watchlist of GitHub repositories and, on demand, fetches and archives their dependency manifests — `package.json` (npm), `requirements.txt` (pip), and `.csproj` (NuGet). Every successful fetch is persisted as a **snapshot** rather than overwriting the previous state, creating an immutable historical audit trail suitable for future diff and vulnerability analysis.

The application consists of three tiers:

1. **Blazor WebAssembly frontend** — a single-page dashboard served by Nginx.
2. **ASP.NET Core 8 Web API backend** — a RESTful service exposing repository and dependency management endpoints, with a 60-minute in-memory TTL cache in front of the GitHub REST API.
3. **SQLite** — managed through Entity Framework Core with automatic migrations on startup.

All three tiers are packaged as Docker containers and orchestrated with Docker Compose, making the entire stack portable and runnable with a single command.

---

## The Problem It Solves

Developers who maintain production systems depend on a handful of critical open-source libraries. Keeping track of when those libraries release new versions means visiting GitHub individually for each repository — a tedious, error-prone, and time-consuming task often described as **dependency fatigue**.

RepoPulse eliminates that toil by providing a single, always-available dashboard. Register a repository once; fetch its dependencies whenever you need a snapshot. The historical record lets you see how a project's dependency tree has evolved over time without relying on third-party services or requiring repository write access.

---

## Unique Selling Point

Most dependency tracking tools (Dependabot, Renovate, Snyk) operate as CI/CD bots that open pull requests. RepoPulse is intentionally different:

- **Read-only and non-intrusive** — it never needs repository write access or a GitHub token (unauthenticated public API only).
- **Snapshot-based archival** — every fetch is preserved as an immutable record, not a live overwrite. This enables historical comparison and trend analysis.
- **Self-hosted and portable** — the entire stack runs on a free Oracle Cloud VM via Docker Compose. No SaaS subscription, no vendor lock-in.
- **Multi-ecosystem parsing in one place** — npm, pip, and NuGet manifests are parsed and stored in a single normalised schema.

---

## Live Demo

| Service | URL |
|---------|-----|
| Dashboard | `http://<YOUR_OCI_PUBLIC_IP>` |
| API Health | `http://<YOUR_OCI_PUBLIC_IP>:8080/health` |
| Swagger UI | `http://<YOUR_OCI_PUBLIC_IP>:8080/swagger` *(development only)* |

---

## Features

- **Repository watchlist** — add any public GitHub repository by `owner/repo` and manage it from the dashboard.
- **On-demand dependency fetch** — trigger a fetch from the repository detail page; the API retrieves and parses all recognised manifest files in the repository root.
- **Multi-ecosystem parsing** — supports `package.json` (npm, including `dependencies` and `devDependencies`), `requirements.txt` (pip, including version operators), and `*.csproj` (NuGet).
- **Snapshot archival** — each successful fetch creates a `DependencySnapshot` record and associated `DependencyEntry` rows; previous snapshots are never deleted.
- **60-minute TTL cache** — GitHub API responses are cached in `IMemoryCache`; the cache key is a composite of owner, repo, and file path. Repeated fetches within the TTL window return cached data without consuming rate-limit budget.
- **Structured health endpoint** — `/health` reports the status of both the database connection and the GitHub API concurrently, returning a machine-readable JSON payload suitable for external monitoring.
- **Global error handling** — all unhandled exceptions return RFC 9110 `ProblemDetails` JSON rather than HTML error pages.
- **Accessible UI** — semantic HTML, ARIA labels on interactive elements, `:focus-visible` keyboard rings, and WCAG AA-aligned contrast ratios.
- **Dockerised stack** — multi-stage Dockerfiles for both the API and the Blazor client; `docker compose up --build` starts the full application.

---

## System Architecture

![System Architecture](docs/diagrams/System%20Architecture.jpg)
> *(The diagram shows four layers: Presentation — Blazor WebAssembly served by Nginx on port 3000; Application — ASP.NET Core Web API on port 8080 with controllers, services, and a 60-minute IMemoryCache; Persistence — EF Core + SQLite; External Integration — GitHub REST API via HttpClient.)*

The architecture follows a strict layered pattern:

- **Presentation Layer** — Blazor WebAssembly compiles to static files (HTML, CSS, JavaScript, and .NET WebAssembly binaries). There is no .NET process at runtime on the client; Nginx simply serves the published `wwwroot` directory.
- **Application Layer** — The ASP.NET Core Web API owns all business logic. Controllers are kept thin; all orchestration lives in the `RepositoryService` and `GitHubService`. The `DependencyParser` is a stateless utility injected into the service layer.
- **Persistence Layer** — EF Core provides a database-agnostic abstraction. SQLite is used in development and containerised deployment for simplicity; the connection string in `appsettings.json` can be switched to MS SQL Server without any code changes.
- **External Integration** — A strongly typed `HttpClient` registered via `IHttpClientFactory` handles all outbound GitHub API calls with a `User-Agent` header and a deterministic base address.

---

## Data Flow

![Data Flow](docs/diagrams/Data%20Flow.jpg)
> *(The diagram shows the 9-step flow: Blazor UI → DependenciesController → RepositoryService → cache check → GitHub REST API (on cache miss) → IMemoryCache (TTL 60 min) → DependencyParser → RepositoryService persists snapshot → RepositoriesController returns JSON → Blazor renders results.)*

When a user clicks **Fetch Dependencies** on a repository detail page:

1. The Blazor client issues `POST /api/repositories/{id}/fetch`.
2. `DependenciesController` receives the request and delegates to `RepositoryService.FetchDependenciesAsync`.
3. `RepositoryService` calls `GitHubService` to load the root file listing of the target repository.
4. `GitHubService` checks `IMemoryCache` for each path. On a cache hit the cached content is returned immediately; on a miss the GitHub REST API is called and the response is cached with a 60-minute absolute expiry.
5. `DependencyParser` analyses the file listing and requests the content of each recognised manifest file.
6. `RepositoryService` persists a new `DependencySnapshot` record and one `DependencyEntry` row per parsed package.
7. `TrackedRepository.LastFetchedAt` is updated.
8. `RepositoriesController` returns the structured JSON response.
9. The Blazor UI renders the dependency table grouped by manifest file.

---

## Entity Relationship Diagram

![Entity Relationship Diagram](docs/diagrams/Entity%20Relationship%20Diagram.jpg)
> *(The diagram shows three entities: TrackedRepository (PK Id, Owner, RepoName, Description, LastFetchedAt, CreatedAt) → one-to-many → DependencySnapshot (PK Id, FK RepositoryId, ManifestFile, FetchedAt) → one-to-many → DependencyEntry (PK Id, FK SnapshotId, PackageName, Version, PackageType). Constraints: Owner + RepoName unique; deletes cascade.)*

The data model has three entities with a clean one-to-many chain:

| Entity | Purpose | Key Constraints |
|--------|---------|-----------------|
| `TrackedRepository` | Represents a registered GitHub repository | `Owner + RepoName` composite unique index |
| `DependencySnapshot` | A single point-in-time fetch result | FK to `TrackedRepository`; cascades on delete |
| `DependencyEntry` | An individual package within a snapshot | FK to `DependencySnapshot`; cascades on delete |

Cascade deletes are configured in the Fluent API so that removing a `TrackedRepository` automatically removes all associated snapshots and entries, preventing orphaned rows.

---

## Snapshot Archival Flow

![Snapshot Archival Flow](docs/diagrams/Snapshot%20Archival%20Flow.jpg)
> *(The diagram shows: TrackedRepository → Fetch dependencies (triggered on demand) → Create DependencySnapshot (stores manifest file and fetched timestamp) → Create DependencyEntry rows (persist package name, version, package type) → Historical dependency archive (multiple snapshots per repository). Separately, TrackedRepository.LastFetchedAt is updated.)*

RepoPulse deliberately preserves each successful fetch as a new snapshot rather than overwriting the existing dependency state. This design decision was made for two reasons:

1. **Auditability** — you can inspect how a repository's dependencies changed between two points in time without any additional tooling.
2. **Future extensibility** — a diff endpoint, vulnerability overlay, or trend chart can be built on top of the existing snapshot table without a schema migration.

---

## Tech Stack

| Layer | Technology | Why This Choice |
|-------|-----------|-----------------|
| Backend API | ASP.NET Core Web API (.NET 8) | Native performance, rich middleware pipeline, excellent DI container |
| Frontend | Blazor WebAssembly | Full-stack C# — one language, one ecosystem, no JavaScript context-switching |
| ORM | Entity Framework Core 8 | Database-agnostic migrations, Fluent API schema control, `AsNoTracking` read performance |
| Database | SQLite (dev/Docker) / MS SQL Server (optional) | SQLite requires zero infrastructure; the abstraction layer makes switching trivial |
| Caching | `IMemoryCache` (built-in .NET) | Zero dependencies, thread-safe, TTL-aware — correct for a single-instance application |
| Static hosting | Nginx (Alpine) | Sub-15 MB image; handles Blazor's client-side routing fallback via `try_files` |
| Containerisation | Docker + Docker Compose | Reproducible builds; entire stack starts with one command |
| Hosting | Oracle Cloud Infrastructure (Always Free Tier) | Free perpetual compute; demonstrates real-world deployment experience |
| Testing | xUnit + Moq + EF Core InMemory | Standard .NET testing stack; InMemory provider isolates unit tests from real DB |

---

## Project Structure

```
RepoPulse/
├── src/
│   ├── RepoPulse.Api/                  # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   ├── RepositoriesController.cs
│   │   │   └── HealthController.cs
│   │   ├── Services/
│   │   │   ├── IGitHubService.cs / GitHubService.cs
│   │   │   ├── IRepositoryService.cs / RepositoryService.cs
│   │   │   └── DependencyParser.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── DatabaseInitialiser.cs
│   │   │   └── Migrations/
│   │   ├── Models/                     # EF Core domain entities
│   │   ├── DTOs/                       # Request/response contracts
│   │   ├── Options/
│   │   │   └── GitHubApiOptions.cs
│   │   ├── Dockerfile
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   └── RepoPulse.Client/               # Blazor WebAssembly SPA
│       ├── Pages/
│       │   ├── Index.razor             # Dashboard
│       │   ├── Repositories.razor
│       │   ├── RepositoryDetail.razor
│       │   └── Health.razor
│       ├── Services/
│       │   └── ApiClient.cs
│       ├── Models/                     # Client-side DTOs
│       ├── Shared/
│       │   ├── MainLayout.razor
│       │   └── NavMenu.razor
│       ├── wwwroot/
│       │   └── css/app.css
│       ├── nginx.conf
│       ├── Dockerfile
│       └── Program.cs
│
├── tests/
│   └── RepoPulse.Tests/                # xUnit test suite
│       ├── DependencyParserTests.cs
│       ├── RepositoryServiceTests.cs
│       └── GitHubServiceTests.cs
│
├── docs/
│   └── diagrams/                       # ← Place your architecture diagrams here
│
├── docker-compose.yml
├── .dockerignore
└── README.md
```

---

## Key Engineering Decisions

### Why Blazor WebAssembly over React or Vue?

The decision to use Blazor WebAssembly was deliberate. The entire application is written in C#, which means a single mental model covers both the API and the client. There is no JavaScript context-switching, no separate npm build pipeline, and no risk of type-contract drift between the API and the consumer. Blazor's `<EditForm>` and two-way data binding provided form handling without third-party libraries. The trade-off is a larger initial download payload compared to a JavaScript SPA framework, but for a developer-focused internal tool this is acceptable.

### Why `IMemoryCache` instead of Redis or a database cache?

`IMemoryCache` is the correct choice for a single-instance application. It is built into .NET, requires zero additional infrastructure, is fully thread-safe, and supports absolute-expiry TTL natively. Redis becomes appropriate when you scale to multiple API instances that need a shared cache state. Adding Redis to a single-server MVP would be over-engineering that adds operational complexity with no measurable benefit. The cache TTL was set to 60 minutes to comfortably stay within GitHub's unauthenticated rate limit of 60 requests per hour on a per-IP basis.

### Why snapshot-based archival instead of overwrite?

The naive implementation would simply overwrite `DependencyEntry` rows on each fetch. The snapshot approach was chosen because it is strictly more informative at negligible storage cost. Each `DependencySnapshot` is an immutable record with a timestamp. This means the system already supports historical queries without any schema changes. It also means a failed or partial fetch cannot corrupt the last known-good state.

### Why DTOs instead of returning EF Core entities directly?

EF Core entities contain navigation properties that cause circular serialisation errors and leak internal schema details into the API contract. DTOs create a stable, independently versioned public surface. The API contract can evolve (e.g., adding a computed field to a DTO) without requiring a database migration, and the database schema can evolve without breaking existing clients. All entity-to-DTO mapping is centralised in the service layer, keeping controllers completely free of data-shaping logic.

### Why `POST /api/repositories/{id}/fetch` instead of `GET`?

The HTTP specification requires `GET` to be safe (no side effects) and idempotent. The fetch operation writes a new `DependencySnapshot` row to the database — it has a definite side effect. Using `GET` would violate HTTP semantics, confuse caching proxies, and mislead developers reading the API contract. `POST` is the semantically correct verb for any operation that mutates state.

### Why `AsNoTracking()` on read queries?

EF Core's change tracker monitors every loaded entity for mutations, which adds overhead on read-only code paths. `AsNoTracking()` disables this tracking for list and detail queries, making those endpoints measurably faster under load. Write operations that need change tracking (create, update, delete) use the standard tracked queries.

---

## Getting Started — Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Git
- A modern browser

### Steps

```bash
# 1. Clone the repository
git clone https://github.com/D-Shah94/RepoPulse.git
cd RepoPulse

# 2. Run the API (auto-applies EF Core migrations; uses SQLite in Development)
cd src/RepoPulse.Api
dotnet run

# 3. In a separate terminal, run the Blazor client
cd src/RepoPulse.Client
dotnet run
```

| Service | URL |
|---------|-----|
| Blazor Dashboard | `https://localhost:7017` |
| API (Swagger) | `https://localhost:7001/swagger` |
| Health Endpoint | `https://localhost:7001/health` |

In **Development** mode the API uses a local SQLite file (`repopulse.db`) created automatically in the project directory. No database setup is required.

> **Note:** The API and client run on separate ports locally. The client's `appsettings.json` points to the API base URL — ensure it matches your local API port if the defaults conflict.

---

## Running with Docker

Docker Compose is the recommended way to run the full stack. It starts the API and the Blazor client (served by Nginx) as two linked containers with a shared network and a persistent volume for the SQLite database.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows / macOS) or Docker Engine + Docker Compose Plugin (Linux)

### Start the stack

```bash
# From the repository root
docker compose up --build
```

| Service | URL |
|---------|-----|
| Blazor Dashboard | `http://localhost:3000` |
| API | `http://localhost:8080` |
| Health Endpoint | `http://localhost:8080/health` |

### Stop and clean up

```bash
# Stop containers (data is preserved in the named volume)
docker compose down

# Stop containers AND remove the database volume (full reset)
docker compose down -v
```

### How the Dockerfiles work

**API (`RepoPulse.Api/Dockerfile`)** — a multi-stage build:
- Stage 1 (`sdk:8.0`): Copies only the `.csproj` first and runs `dotnet restore`. This layer is cached by Docker; subsequent builds that change only source code skip the restore entirely.
- Stage 2 (`aspnet:8.0`): Copies only the published output (~50 MB) from Stage 1. The compiled binary runs as a non-root `appuser` to reduce the attack surface.

**Client (`RepoPulse.Client/Dockerfile`)** — another multi-stage build:
- Stage 1: Compiles Blazor WASM to static files.
- Stage 2 (`nginx:alpine`, ~15 MB total image): Serves the static files. The custom `nginx.conf` includes `try_files $uri $uri/ /index.html` — this is critical for Blazor's client-side routing. Without it, refreshing or deep-linking to a route like `/repositories/5` causes Nginx to return HTTP 404 instead of serving `index.html` and letting the Blazor router handle the URL.

**Volume persistence:** The `docker-compose.yml` mounts a named volume at `/data` inside the API container and sets the SQLite connection string to `/data/repopulse.db`. This ensures the database survives container restarts. Without this volume, every `docker compose down` would wipe all registered repositories.

**Health-check dependency:** The `client` service is configured with `depends_on: api: condition: service_healthy`. Docker waits for the API's `/health` endpoint to return 200 before starting the Nginx container. This prevents the UI from loading while the API is still applying database migrations.

---

## Production Deployment (Oracle Cloud)

RepoPulse is deployed on an Oracle Cloud Infrastructure **VM.Standard.E2.1.Micro** instance (Always Free tier): 1 OCPU, 1 GB RAM, x86-64 Ubuntu.

### 1. Provision and prepare the VM

SSH into your Ubuntu VM and run the following:

```bash
# Update packages
sudo apt-get update && sudo apt-get upgrade -y

# Remove any conflicting database services to free RAM
sudo apt-get remove --purge -y mysql-server postgresql mariadb-server
sudo apt-get autoremove -y && sudo apt-get autoclean -y

# Create and enable a 4 GB swap file (critical on a 1 GB VM)
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# Install Docker
sudo apt-get install -y docker.io docker-compose
sudo systemctl enable --now docker
sudo usermod -aG docker $USER   # allows running docker without sudo after re-login
```

### 2. Configure the firewall

```bash
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP (Nginx/Blazor)
sudo ufw allow 8080/tcp  # API
sudo ufw --force enable
```

> Also open ports 80 and 8080 in the OCI Console under **Networking → Virtual Cloud Networks → Security Lists**.

### 3. Deploy

```bash
# Clone the repository on the VM
git clone https://github.com/D-Shah94/RepoPulse.git
cd RepoPulse

# Build and start
docker compose up --build -d
```

The `-d` flag runs containers in the background. Logs can be tailed with:

```bash
docker compose logs -f
```

### 4. Keeping it running after reboots

Add `restart: unless-stopped` to each service in `docker-compose.yml` (already included). Docker automatically restarts both containers on VM reboot.

### Why Oracle Cloud (Always Free)?

Oracle's Always Free tier provides perpetual compute capacity at no cost — not a 12-month trial. The 1 GB RAM constraint required careful configuration:

- The 4 GB swap file prevents the Docker daemon from OOM-killing containers during builds.
- SQLite was chosen over MS SQL Server for the containerised deployment specifically because SQL Server requires a minimum of 2 GB RAM and cannot start reliably on a 1 GB VM without complex memory-bypass shims (`LD_PRELOAD` hacks with `libjemalloc`).
- Container memory limits in `docker-compose.yml` (`mem_limit`) prevent a single runaway container from starving the other.

---

## Configuration Reference

All configuration lives in `appsettings.json` (base values) and `appsettings.Development.json` (local overrides). Sensitive production values should be set as environment variables and never committed to source control.

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQLite Server connection string | `Data Source=repopulse.db` |
| `GitHub:BaseUrl` | GitHub REST API base URL | `https://api.github.com` |
| `GitHub:UserAgent` | Required by GitHub API | `RepoPulse/1.0` |
| `GitHub:CacheTtlMinutes` | Duration (minutes) that GitHub responses are cached | `60` |
| `ASPNETCORE_ENVIRONMENT` | Set to `Development` for Swagger + verbose logging | `Production` |
| `ASPNETCORE_URLS` | HTTP listen address inside the container | `http://+:8080` |

---

## API Endpoints

| Method | Route | Description | Success Response |
|--------|-------|-------------|-----------------|
| `GET` | `/api/repositories` | List all tracked repositories | `200 OK` |
| `GET` | `/api/repositories/{id}` | Get a single repository by ID | `200 OK` / `404 Not Found` |
| `POST` | `/api/repositories` | Register a new repository | `201 Created` |
| `DELETE` | `/api/repositories/{id}` | Remove a repository and all its snapshots | `204 No Content` |
| `POST` | `/api/repositories/{id}/fetch` | Fetch and archive current dependencies from GitHub | `200 OK` |
| `GET` | `/health` | System health check (DB + GitHub API) | `200 OK` / `503 Service Unavailable` |

### Example: Register a repository

```http
POST /api/repositories
Content-Type: application/json

{
  "owner": "dotnet",
  "repoName": "aspnetcore",
  "description": "ASP.NET Core framework"
}
```

### Example: Fetch dependencies

```http
POST /api/repositories/1/fetch
```

Returns a grouped result with one entry per detected manifest file and an array of `DependencyEntry` objects.

---

## Health Endpoint

`GET /health` runs two checks concurrently using `Task.WhenAll` and returns structured JSON:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "Database",
      "status": "Healthy",
      "description": "SQLite connection verified"
    },
    {
      "name": "GitHub API",
      "status": "Healthy",
      "rateLimitRemaining": 58,
      "rateLimitResetUtc": "2026-06-05T14:00:00Z"
    }
  ]
}
```

The endpoint returns `200 OK` when all checks pass and `503 Service Unavailable` when any check fails or is degraded. Running checks concurrently means the response time equals the duration of the slowest check, not the sum — important for a health probe that may be polled frequently.

---

## Testing

The test project (`RepoPulse.Tests`) uses xUnit, Moq, and the EF Core InMemory provider.

```bash
cd tests/RepoPulse.Tests
dotnet test
```

### Test coverage

| Test class | What it verifies |
|------------|-----------------|
| `DependencyParserTests` | Correct extraction of package names and versions from `package.json`, `requirements.txt`, and `.csproj` files; graceful handling of malformed input |
| `RepositoryServiceTests` | `FetchDependenciesAsync` correctly creates `DependencySnapshot` and `DependencyEntry` rows using a mocked `IGitHubService` and an in-memory database |
| `GitHubServiceTests` | Behavioural tests verifying that the TTL cache is respected (cached responses do not trigger additional GitHub API calls) and that missing files return `null` cleanly |

> **Why the `DependencyParser` visibility was changed:** `ParseManifest` was originally `internal`. To enable direct unit testing without reflection hacks, its visibility was changed to `public`. This is a legitimate trade-off — the method is a pure parsing function with no side effects, and the increased testability justifies the broader visibility.

---

## Engineering Challenges & How They Were Solved

### Challenge 1: Blazor WebAssembly deep-link routing with Nginx

**Problem:** When a user navigated directly to `/repositories/5` or refreshed the page, Nginx looked for a physical file at that path, found nothing, and returned HTTP 404.

**Solution:** Added `try_files $uri $uri/ /index.html;` to the Nginx server block. This tells Nginx to attempt to serve a static file first; if none exists, fall back to `index.html`, which boots the Blazor runtime and lets its client-side router handle the URL.

### Challenge 2: Blazor mounting crash — `index.html` asset fingerprinting

**Problem:** Blazor 8 publishes assets with content-hashed filenames (e.g., `app.a1b2c3.js`). A bug in the initial `index.html` template caused a crash when the Blazor loading indicator was placed outside the `#app` div.

**Solution:** Rewrapped the loading indicator inside the `#app` div and ensured the `<script>` tag for the Blazor bootstrap was correctly referenced. The `nginx.conf` also sets `expires -1; add_header Cache-Control "no-store, no-cache"` on `index.html` specifically, so clients always fetch the latest entry point — even though hashed assets are cached indefinitely.

### Challenge 3: GitHub REST API endpoint typo causing silent 500s

**Problem:** An early implementation used `/repo/` instead of `/repos/` in the GitHub API URL, causing `GitHubService` to receive a 404 from GitHub. The error was compounded by a malformed `ILogger` template string that swallowed the exception silently, making the root cause difficult to identify.

**Solution:** Corrected the endpoint path and fixed the logger template string. Added structured logging with a `try/catch` in `GitHubService` that logs the HTTP status code, path, and response body on non-success responses, making future debugging significantly faster.

### Challenge 4: EF Core `CreateRepositoryDto` two-way binding failure

**Problem:** `CreateRepositoryDto` was initially defined as a C# `record`, which is immutable by default. Blazor's `<EditForm>` requires two-way data binding via a mutable property setter. The form silently failed to update the model on user input.

**Solution:** Converted `CreateRepositoryDto` from a `record` to a standard `class` with public settable properties. This is the correct approach for form-bound models in Blazor; `record` types are appropriate for DTOs that flow in one direction (e.g., API responses), not for input models.

### Challenge 5: CORS policy blocking Blazor client requests

**Problem:** The Blazor client on `https://localhost:7017` was blocked by the browser's same-origin policy when calling the API on `https://localhost:7001`. Similarly, when containerised, the client on port 3000 was blocked from reaching the API on port 8080.

**Solution:** The CORS policy in `Program.cs` was updated in two stages: first to allow `https://localhost:7017` for local development, then to allow `http://localhost:3000` for the Docker environment. In production, `AllowAnyOrigin()` is used because the API and client are served from the same host IP, making strict origin matching unnecessary.

### Challenge 6: EF Core schema drift — `PackageName` and `PackageType` columns

**Problem:** After adding `PackageName` and `PackageType` properties to `DependencyEntry`, the local SQLite database still had the old schema (no migration had been generated). This caused runtime errors when the service tried to write to non-existent columns.

**Solution:** Generated a new EF Core migration and verified the migration was applied by `DatabaseInitialiser` on startup. Added a note to the team workflow: any change to an entity class must be followed immediately by `dotnet ef migrations add <name>` and committing the migration file.

### Challenge 7: Compiled binaries in source control

**Problem:** Early commits accidentally included `bin/` and `obj/` directories, which added hundreds of megabytes of compiled artefacts to the repository and caused confusing merge conflicts.

**Solution:** Added a comprehensive `.gitignore` (covering `bin/`, `obj/`, `*.db`, `*.user`, `appsettings.Development.json`) and purged the tracked binaries with `git rm --cached -r`. A corresponding `.dockerignore` prevents the same directories from being sent to the Docker build context, keeping build times fast.

---

## Roadmap

Planned features in priority order:

- [ ] **Background sync job** — `IHostedService` that periodically re-fetches all registered repositories on a configurable schedule (e.g., nightly), eliminating the need for manual fetch triggers.
- [ ] **Snapshot diff view** — a UI page comparing two snapshots for the same repository to show added, removed, and version-bumped dependencies.
- [ ] **GitHub Advisory Database integration** — cross-reference fetched packages against the GitHub Advisory API to surface known CVEs directly in the dependency table.
- [ ] **Per-user watchlists** — ASP.NET Core Identity with JWT authentication, so each user manages their own set of tracked repositories rather than a shared global list.
- [ ] **Webhook-triggered fetch** — accept a GitHub webhook payload to trigger an automatic dependency fetch when a new release is published.

---

## FAQ

**Q: Why Blazor WebAssembly over React or Vue?**

Full-stack C# means one language and one ecosystem. The entire application — from database entities to API contracts to UI components — is written in C#, which eliminates JavaScript context-switching and keeps the tooling surface small. It also demonstrates depth in the .NET ecosystem rather than shallow breadth across multiple languages.

**Q: What happens if the GitHub rate limit is hit?**

The 60-minute TTL cache means each unique file path costs at most one GitHub API request per hour. In practice, a typical repository requires 2–4 requests (root listing + manifest files), so the 60-requests-per-hour unauthenticated limit is extremely difficult to exhaust with normal usage. If the limit is hit, `GitHubService` receives a `429` or non-success status code, logs the failure, returns `null`, and the API returns a structured error response. The application degrades gracefully without crashing.

**Q: Why `IMemoryCache` and not Redis?**

`IMemoryCache` is correct for a single-instance application — it is built-in, zero-dependency, thread-safe, and TTL-aware. Redis is the right choice when scaling to multiple API instances that need a shared cache state. Adding Redis to a single-server deployment adds operational complexity for no measurable benefit.

**Q: Why does the dependency fetch use POST and not GET?**

HTTP `GET` must be safe (no side effects) and idempotent. The fetch operation writes a new `DependencySnapshot` row to the database, which is a definite side effect. Any operation that mutates state must use `POST`.

**Q: Why does `AsNoTracking()` appear on read queries?**

EF Core's change tracker monitors every loaded entity for mutations — overhead that is completely wasted on read-only queries. `AsNoTracking()` disables that tracking, making list endpoints measurably faster and reducing memory pressure.

**Q: Why are client-side models separate from the API DTOs?**

The Blazor client and the API are independent deployable units. A project reference would create tight coupling and prevent independent versioning. Client-side models are the client's own representation of the API contract, updated independently as the API evolves. The trade-off is a small amount of duplication; the benefit is clear deployment independence.

---

## Contributing

This is a personal learning project. Issues and pull requests are welcome.

1. Fork the repository.
2. Create a feature branch: `git checkout -b feat/your-feature-name`.
3. Commit using the [Conventional Commits](https://www.conventionalcommits.org/) format (`feat:`, `fix:`, `chore:`, `test:`, `docs:`).
4. Open a pull request with a clear description of the change and why it was made.

---

## License

This project is licensed under the [MIT License](LICENSE).

---

<p align="center">Built by <a href="https://github.com/D-Shah94">D-Shah94</a></p>
