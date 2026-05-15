# WebTransactions

A purchase transaction management REST API built as a coding challenge for a C# position.

## Features

| Feature | Description |
|---|---|
| **REST API** | Versioned ASP.NET Core Web API (`/api/v1`) with MVC controllers for storing and retrieving transactions |
| **Currency Conversion** | Real-time conversion via the US Treasury Rates of Exchange API using the most recent rate within 6 months |
| **Blazor Server UI** | Interactive web interface for creating, listing, and retrieving transactions with currency conversion |
| **Embedded SQLite** | File-based database via EF Core requiring no external server, applied automatically on startup |
| **API Versioning** | URL-based versioning (`v1`) using `Asp.Versioning.Mvc`, with version headers reported on every response |
| **Cancellation Tokens** | All async operations support cancellation, freeing server resources when clients disconnect |
| **xUnit Functional Tests** | 11 end-to-end API tests using `WebApplicationFactory` covering happy paths, validation, and edge cases |
| **Playwright UI Tests** | 4 browser-driven tests verifying full user flows using a real Chromium instance |
| **GitHub Actions CI** | Automated build, test, and reporting pipeline running on every push and pull request |
| **CodeQL Security Scanning** | Static security analysis integrated into CI with results published to the GitHub Security tab |
| **Automated Releases** | Git tag push triggers a CI workflow that builds and publishes a self-contained Windows executable |
| **Single-File Executable** | Self-contained `.exe` with embedded static files — runs with no .NET installation required |
| **XML Documentation** | All public interfaces, controllers, and DTOs documented with XML doc comments |
| **Copy to Clipboard** | Reusable Blazor component for copying transaction IDs, with visual success/failure feedback |

## Quick Start

### Option 1 — Pre-built executable (recommended, no .NET required)

1. Go to the [latest release](https://github.com/Brannach/WebTransactions/releases/latest)
2. Download `WebTransactions.Api.exe`
3. Place it in the root of the repository (alongside `run.bat`)
4. Double-click `run.bat`

### Option 2 — Run from source

Double-click `run.bat` — it will install .NET 9 automatically if not present and launch the application.

Or manually:

```bash
dotnet run --project src/WebTransactions.Api
```

The application will be available at **http://localhost:5107**

### Running the Tests

```bash
dotnet test
```

---

## Tech Stack

### Language & Runtime — C# / .NET 9

C# was chosen as the implementation language to match the requirements of the position being applied for. Beyond that requirement, C# is a mature, strongly-typed, object-oriented language with first-class support for modern patterns such as async/await, LINQ, records, and nullable reference types. These features make it particularly well-suited for building reliable backend services where correctness and maintainability matter.

.NET 9 specifically was selected because it is the latest stable release of the runtime at the time of development. It brings continued performance improvements to the HTTP pipeline, better AOT (Ahead-Of-Time) compilation support, and improvements to the garbage collector. Using the latest version also signals familiarity with the current ecosystem, which is relevant in a professional context.

The .NET ecosystem has the significant advantage of being fully cross-platform — the application runs identically on Windows, Linux, and macOS, which is important for production deployability. The tooling (`dotnet CLI`, Rider, Visual Studio) is mature and well-integrated with modern CI/CD pipelines.

C# also has a rich type system that allows us to express domain concepts clearly — for example, using `record` types for immutable DTOs, `decimal` for precise monetary arithmetic (critical for financial applications), and `DateOnly` for date-only fields like transaction dates. These are not incidental choices: using `double` or `float` for money would introduce floating-point rounding errors, and using `DateTime` for a date-only concept would introduce unnecessary timezone complexity.

---

### Framework — ASP.NET Core Web API with MVC Controllers

ASP.NET Core is Microsoft's open-source, cross-platform web framework for building HTTP APIs. It was chosen over alternatives (like Nancy or ServiceStack) because it is the standard, most widely adopted framework in the .NET ecosystem, with the largest community, best documentation, and deepest tooling support.

Within ASP.NET Core, the **MVC controller pattern** was chosen over the newer **minimal APIs** introduced in .NET 6. While minimal APIs reduce boilerplate and are well-suited for small microservices or simple proxy layers, controllers offer a better foundation for a project that may grow over time. Controllers provide a clean, class-based structure where each controller groups related endpoints under a shared route prefix, making the codebase easier to navigate as the number of endpoints increases.

Controllers also integrate naturally with ASP.NET Core's full middleware pipeline, including model validation via `[ApiController]`, action filters, and dependency injection. The `[ApiController]` attribute in particular provides automatic HTTP 400 responses when model validation fails, eliminating boilerplate validation code in each action method.

From an interview perspective, controllers are also the more recognizable and widely understood pattern — most .NET developers are familiar with them, and demonstrating clean controller design (thin controllers delegating to services) is a well-established best practice that communicates architectural awareness.

The separation of concerns follows a layered approach: controllers handle HTTP concerns (request parsing, response formatting), services contain business logic (validation rules, orchestration), and repositories handle data access — making each layer independently testable.

---

### Persistence — SQLite with Entity Framework Core 9

SQLite was chosen as the database engine because it is an embedded, serverless database that runs entirely within the application process. This directly satisfies the project requirement of running standalone without an external database server. The database is stored as a single `.db` file on disk, which means the application has zero infrastructure dependencies — it can be cloned and run immediately with `dotnet run`.

Despite being lightweight, SQLite is production-grade software used in billions of devices and applications worldwide. For a service managing purchase transactions at the scale implied by this challenge, SQLite's performance is more than sufficient. It supports ACID transactions, foreign keys, and full SQL, making it a serious choice rather than a toy.

Entity Framework Core 9 was chosen as the ORM (Object-Relational Mapper) to interact with SQLite. EF Core provides several important benefits: it eliminates raw SQL for standard CRUD operations, provides a strongly-typed query API via LINQ, manages database schema through migrations (so the schema is versioned alongside the code), and handles connection lifecycle automatically. This significantly reduces the surface area for bugs like SQL injection or connection leaks.

EF Core also makes it easy to swap the underlying database in the future — replacing SQLite with PostgreSQL or SQL Server requires changing one line of configuration and rerunning migrations, without touching any business logic or repository code. This is a meaningful architectural advantage if the application were to scale beyond what SQLite can handle.

The `decimal` type is used for monetary amounts throughout, which maps to `TEXT` in SQLite (since SQLite has no native decimal type) but is handled transparently by EF Core's value converters, ensuring no precision is lost during storage or retrieval.

---

### Testing — xUnit with Microsoft.AspNetCore.Mvc.Testing

xUnit was chosen as the test framework because it is the default framework recommended by the ASP.NET Core team and used throughout Microsoft's own open-source projects. It integrates seamlessly with `dotnet test`, supports parallel test execution by default (which speeds up large test suites), and has a clean, minimal API — test classes are instantiated fresh for each test, avoiding shared state bugs that plague other frameworks.

Compared to MSTest, xUnit is more modern and idiomatic in the current .NET ecosystem. MSTest carries legacy design decisions from its origins as a Visual Studio-only tool, whereas xUnit was designed from the ground up as a community-first, cross-platform framework. NUnit is also a solid alternative, but xUnit has become the de facto standard for new .NET projects.

`Microsoft.AspNetCore.Mvc.Testing` is the key package that enables true functional testing. It provides `WebApplicationFactory<T>`, which boots the entire application — middleware, routing, dependency injection, database — in memory during the test run, without needing a real HTTP server. Tests send real `HttpClient` requests and receive real HTTP responses, exercising every layer of the application from the controller down to the database.

This approach satisfies the brief's requirement for functional automated testing. Unlike unit tests that mock dependencies, these tests verify that the full request/response cycle works correctly, including validation, serialization, database persistence, and external API integration. The test database is configured as an in-memory SQLite instance (or a temporary file) so tests are isolated and do not affect production data.

This testing strategy gives high confidence that the application behaves correctly end-to-end, which is exactly the kind of assurance that matters in a production-ready service.

---

### UI — Blazor Server

Blazor Server was chosen as the UI framework because it allows the entire application — backend and frontend — to be written in C#, with no JavaScript required. This keeps the technology stack consistent and reduces context-switching between languages.

Blazor Server renders components on the server and pushes UI updates to the browser over a persistent SignalR connection. This means the full application state and business logic remain on the server, the browser receives rendered HTML, and interactions feel real-time without writing a single line of JavaScript. Compared to Blazor WebAssembly, the Server model has faster initial load times and full access to server-side resources (the database, services, configuration) directly from UI components.

The alternatives considered were Razor Pages (simpler but less interactive, better suited for mostly static pages) and a separate frontend framework like React or Angular. A separate frontend would require maintaining two codebases and a cross-origin API setup, which adds unnecessary complexity for a project of this scope. Blazor Server keeps everything in one deployable unit.

---

### UI Testing — Playwright

Playwright was chosen for end-to-end UI testing because it drives a real browser (Chromium, Firefox, or WebKit) and interacts with the application exactly as a user would — clicking buttons, filling forms, reading text, and asserting page state. This makes it the most realistic form of functional UI testing available.

Compared to the two main alternatives in the .NET ecosystem, Playwright is the strongest fit for this project. **Selenium WebDriver** is the older standard but requires manual browser driver management, is more verbose, and is significantly slower. **bUnit** is a Blazor-specific testing library that renders components in memory without a real browser — it is fast and useful for isolated component tests, but it cannot exercise real HTTP calls, navigation, or full-page flows, which is what functional testing requires.

Playwright tests are run as a separate test project and require the application to be listening on a real HTTP port. This is handled programmatically in the test setup using `WebApplicationFactory` configured with a real port, so no manual steps are needed — `dotnet test` runs everything end-to-end automatically. This does not violate the standalone requirement of the assignment, as Playwright is test tooling rather than an application dependency.

---

### CI/CD — GitHub Actions

GitHub Actions was chosen for continuous integration because it is natively integrated with GitHub — no external CI service (Jenkins, CircleCI, TeamCity) needs to be configured or paid for. Workflows are defined as YAML files committed alongside the code, making the CI configuration versioned and reviewable like any other change.

The workflow triggers on every push and pull request to the `master` branch, running three steps: dependency restore, Release build, and the full test suite. Using the Release configuration for CI (rather than Debug) ensures that the build artifact tested in CI matches what would be deployed to production — catching any issues that only manifest in optimized builds.

GitHub Actions also provides a hosted `ubuntu-latest` runner with .NET 9 pre-installed via the `actions/setup-dotnet` action, meaning there is no infrastructure to maintain. The workflow will be extended to include Playwright UI tests as part of the same pipeline once the UI layer is complete.

---

## Project Generation

The solution was scaffolded entirely using the `dotnet` CLI rather than a Visual Studio wizard or third-party scaffolding tool. This approach was chosen deliberately: it produces a minimal, well-understood starting point with no hidden files or IDE-specific artifacts, and it works identically across Windows, macOS, and Linux — which matters for a project that others may clone and run.

The commands used and the reasoning behind each flag:

```bash
# Create the solution file
dotnet new sln -n WebTransactions

# Create the API project
dotnet new webapi -n WebTransactions.Api -o src/WebTransactions.Api --no-openapi -f net9.0

# Create the test project
dotnet new xunit -n WebTransactions.Api.Tests -o tests/WebTransactions.Api.Tests -f net9.0

# Register both projects in the solution
dotnet sln WebTransactions.sln add src/WebTransactions.Api/WebTransactions.Api.csproj
dotnet sln WebTransactions.sln add tests/WebTransactions.Api.Tests/WebTransactions.Api.Tests.csproj

# Add persistence packages (pinned to 9.x to match net9.0 target)
dotnet add src/WebTransactions.Api/WebTransactions.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.5
dotnet add src/WebTransactions.Api/WebTransactions.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.5

# Add testing packages
dotnet add tests/WebTransactions.Api.Tests/WebTransactions.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/WebTransactions.Api.Tests/WebTransactions.Api.Tests.csproj reference src/WebTransactions.Api/WebTransactions.Api.csproj
```

Key decisions made during scaffolding:

- **`--no-openapi`** — omits Swagger/OpenAPI generation. The challenge does not require interactive API documentation, and excluding it keeps the project lean and the `Program.cs` startup code uncluttered.
- **`webapi` template without `--use-minimal-apis`** — the default `webapi` template generates a controller-based project, which was the chosen pattern (see Framework section above).
- **`-f net9.0` on all projects** — pins both the API and test projects to the same target framework, avoiding version mismatch issues during test runs.
- **EF Core packages pinned to `9.0.5`** — NuGet resolved to version 10.x by default (targeting net10.0), which is incompatible with net9.0. Explicit version pinning was required to get a compatible package.
- **Solution structure with `src/` and `tests/` folders** — separates production code from test code at the filesystem level, a convention common in enterprise .NET repositories that makes it immediately clear what ships and what does not.

---

## Project Structure

```
WebTransactions/
├── src/
│   └── WebTransactions.Api/        # Main API project
└── tests/
    └── WebTransactions.Api.Tests/  # Functional test project
```

## Requirements

### Store a Purchase Transaction
`POST /transactions`

Accepts a transaction with description (max 50 chars), transaction date, and purchase amount in USD (positive, rounded to nearest cent). Returns a unique identifier.

### Retrieve a Purchase Transaction in a Target Currency
`GET /transactions/{id}?currency={country:currency}`

Returns the transaction converted to the target currency using the [Treasury Reporting Rates of Exchange API](https://fiscaldata.treasury.gov/api-documentation/). The exchange rate used must be from within 6 months of the purchase date. Returns an error if no valid rate is available.

## Deployment

### Local

The application runs out of the box with no configuration. SQLite creates the `.db` file automatically on first run. This is the primary target for this challenge.

### Docker

The application can be containerized using the official .NET 9 base images. The key consideration is SQLite persistence: the `.db` file lives inside the container filesystem and will be lost if the container is removed or restarted. A volume mount is required to persist data across restarts:

```bash
docker run -v /host/data:/app/data -e ConnectionStrings__DefaultConnection="Data Source=/app/data/WebTransactions.db" WebTransactions
```

Without the volume, the application still runs correctly — data simply resets on each container start, which is acceptable for demo or testing purposes.

### Cloud (without a container)

The application can be deployed directly to PaaS platforms that support .NET 9:

- **Azure App Service** — native .NET 9 support, no Docker required. Deploy via `dotnet publish` and the Azure CLI or GitHub Actions
- **AWS Elastic Beanstalk** — .NET platform available, similar deployment flow
- **Fly.io / Railway / Render** — smaller platforms with simpler setup, also support .NET

The same SQLite caveat applies to all cloud deployments: most platforms provide an ephemeral filesystem that does not survive redeployments or restarts. Attaching persistent storage (Azure File Share, AWS EFS) resolves this.

### Scaling beyond SQLite

If the application were to be deployed in a multi-instance setup (more than one running process), SQLite would become a bottleneck as it does not support concurrent writes from multiple processes. The migration path is straightforward — EF Core is database-agnostic, so switching to PostgreSQL or SQL Server requires changing one line in `Program.cs` and rerunning migrations, with no changes to business logic or repositories.

---

## Running the Application

### Option 1 — Pre-built executable (recommended, no .NET required)

1. Go to the [latest release](https://github.com/Brannach/WebTransactions/releases/latest)
2. Download `WebTransactions.Api.exe`
3. Run it directly — no installation needed

The application will start at `http://localhost:5107`.

### Option 2 — Run from source (requires .NET 9 SDK)

If you prefer to run from source, double-click `run.bat`. It will automatically install .NET 9 if not present and launch the application.

Alternatively, run manually:

```bash
dotnet run --project src/WebTransactions.Api
```

The application will start at `http://localhost:5107`.

## Running the Tests

```bash
dotnet test
```

For full test documentation including test cases and infrastructure details, see [TESTING.md](TESTING.md).
