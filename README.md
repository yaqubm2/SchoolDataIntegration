# School Data Integration API

A service that ingests student data from external School Information Systems
(SIS) — via a local CSV drop folder or a REST API — and synchronizes it into
SchoolApp's own student store.

## Solution layout

```
SchoolDataIntegration.sln
src/SchoolDataIntegration.Api/
  Program.cs                  # DI wiring + the POST /student-imports endpoint. No business logic lives here.
  Config/
    AppConfig.cs               # POCO shape of config.xml
    ConfigLoader.cs             # Loads + caches config.xml
    config.xml                  # Default XML config (endpoints, credentials, db, mail)
  Models/                       # Student, DTOs, ImportRecord, ImportSummary, RetrievedData, enums
  Data/
    SchoolDbContext.cs          # EF Core (SQLite) context: Students, ImportRecords
  Retriever/                    # Where the raw bytes come from
    StudentDataRetriever.cs      # CSV-drop-folder-first, REST-API-fallback orchestration
    RestApiClient.cs             # HTTP Basic Auth call to the external SIS
    FilePersister.cs             # Caches REST-sourced data back to CSV for reuse
  Builder/                      # Turns raw bytes into StudentDto objects
    CsvParser.cs                 # Small dependency-free CSV parser (quoted fields, escapes)
    StudentBuilder.cs            # CSV or JSON -> List<StudentDto>
  Filter/                       # Business rules: de-dup + validation
    StudentFilter.cs
  Transformer/                  # Persists to the DB: create-or-update, per-record isolation
    StudentTransformer.cs
  Events/                       # In-memory pub/sub
    StudentImportCompletedEvent.cs
    InMemoryEventPublisher.cs
  Mail/                         # Sends the post-import summary email
    SmtpMailService.cs
  Processing/
    Processor.cs                 # Orchestrates: Retrieve -> Build -> Filter -> Transform -> Track -> Publish
tests/SchoolDataIntegration.Tests/
  Filter/StudentFilterTests.cs
  Builder/CsvParserTests.cs, StudentBuilderTests.cs
  Transformer/StudentTransformerTests.cs
  Processing/ProcessorTests.cs
  Config/ConfigLoaderTests.cs
samples/
  123.csv                       # Example local CSV drop file (school id "123")
  inline-import-request.json    # Example POST body exercising dedupe + validation rules
```

## Important design decision & assumption (please read)

> **If `students` is present and non-empty in the request body, it's used
> directly** (useful for manual pushes, backfills, and tests). **If it's
> omitted or empty, the `Processor` pulls the data itself** via the
> Retriever pipeline: local CSV drop folder first
> (`{LocalCsvDropFolder}\{schoolId}.csv`), then the school's REST API
> (HTTP Basic Auth) as a fallback. Data pulled from the REST API is cached
> back to the CSV drop folder so a re-run can hit the fast local path.

This is implemented in `Processor.RetrieveAndBuildAsync`. If you intended
something different (e.g. the body should always be ignored in favor of the
Retriever, or the Retriever logic belongs on a separate endpoint/scheduled
job entirely), it's an easy change — the branch point is in one place.

## Other notable decisions

- **Composite identity.** A student's `ExternalId` is only guaranteed unique
  *within* a school's own SIS, never globally, so the real identity used for
  matching/upserts everywhere (DB unique index, Filter de-dup, Transformer
  lookup) is `(SchoolId, ExternalId)`.
- **Continue-on-failure, per record.** The Transformer persists each student
  independently (its own `SaveChanges`) and catches exceptions per record,
  so one bad row can't sink or corrupt the rest of a large batch. The
  trade-off is more DB round trips than a single bulk save — a reasonable
  next optimization (batch N records per `SaveChanges`) if throughput ever
  becomes a bottleneck; noted here rather than silently traded away.
- **Two kinds of failure, handled differently.** A record that fails
  validation or persistence is a *partial* failure — the import still
  completes and comes back as `200 OK` with `failed > 0` in the summary. A
  failure to retrieve/parse the source data at all (SIS unreachable,
  malformed payload) is a *total* failure — the whole import is marked
  `Failed` and the endpoint returns `502 Bad Gateway`, since no records were
  even attempted.
- **Duplicates vs failures.** Duplicate `ExternalId`s within the same import
  batch are ignored, not counted as failures (per the "ignore duplicate
  records" rule) — they're reported separately as `duplicates` in the
  summary so nothing is silently lost from the math (`total = success +
  failed + duplicates`).
- **No external CSV/JSON packages.** `CsvParser` is a small, dependency-free,
   parser (quoted fields, embedded commas, escaped quotes). JSON
  uses `System.Text.Json`, built into .NET. This keeps the solution buildable
  without any extra NuGet surface area for something this scope.
- **XML config, separate from host config.** `Config/config.xml` holds the
  business/integration settings the spec calls for (endpoints, Basic Auth
  credentials, DB connection string, mail settings). `appsettings.json` is
  left for ASP.NET Core's own host concerns (logging, Kestrel) rather than
  overloading it with business config.
- **SQLite via `EnsureCreated()`, not migrations.** Kept deliberately simple
  for this exercise — `Program.cs` calls `EnsureCreated()` on startup rather
  than requiring `dotnet ef database update`. A production rollout would use
  EF Core migrations instead so schema changes are versioned and repeatable.
- **Mail has a `DryRun` mode** (default `true` in `config.xml`) that logs the
  summary email instead of sending it, so the service runs out-of-the-box
  without a real SMTP relay configured. A notification failure is caught and
  logged — it can never fail an import that already completed.
- **Event publisher is swappable.** `IEventPublisher` doesn't imply an
  in-process transport; `InMemoryEventPublisher` is the concrete choice made
  here, but nothing else in the codebase depends on events being in-process,
  so swapping in an SQS/RabbitMQ/Azure Service Bus-backed implementation is a
  drop-in replacement.
- **`YearLevel` is a string**, not an int — different SIS vendors express it
  differently ("10", "Year 10", "Grade 5", "K"). Normalizing it is a
  reporting-layer concern, not an ingestion-layer one.

## Running it

Requires the .NET 8 SDK.

```bash
dotnet restore
dotnet build
dotnet run --project src/SchoolDataIntegration.Api
```

On first run this creates `schoolapp.db` (SQLite) next to the executable and
copies `Config/config.xml` to the output folder. Edit that copy (or the
source `Config/config.xml`) to point at a real CSV folder / REST API /
SMTP server for your environment; the shipped defaults use safe local
placeholders (`Mail.DryRun = true`, a relative SQLite file).

### Try it — inline body (bypasses the Retriever)

```bash
curl -X POST http://localhost:5000/student-imports \
  -H "Content-Type: application/json" \
  -d @samples/inline-import-request.json
```

Expected summary: `total: 4, success: 2, failed: 1, duplicates: 1` (one
missing-ExternalId record is dropped as invalid, one is a duplicate
ExternalId within the batch).

### Try it — local CSV drop folder (exercises the Retriever)

1. Set `LocalCsvDropFolder` in `Config/config.xml` to a real folder, e.g. `./drop`.
2. Copy `samples/123.csv` into that folder.
3. `curl -X POST http://localhost:5000/student-imports -H "Content-Type: application/json" -d '{"schoolId":"123"}'`

## Testing

```bash
dotnet test
```

Covers: Filter de-dup/validation rules, the CSV parser's edge cases (quoted
fields, escaped quotes, ragged rows), the Builder's CSV/JSON parsing
(including both raw-array and `{ "students": [...] }`-wrapped JSON shapes),
the Transformer's create/update/continue-on-failure behavior (including a
`DbContext` test double that forces a simulated persistence failure on one
record to prove the rest of the batch still lands), and the Processor's
end-to-end orchestration (inline-vs-retrieved data source selection, REST
cache-back behavior, import tracking, event publication, and both failure
modes described above).
