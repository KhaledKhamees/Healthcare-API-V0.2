# Healthcare API - ASP.NET Core 8 Layered Backend

> **Education and portfolio project.**
> This repository is a learning exercise in backend architecture, identity, caching, observability, containers, CI, and load testing. It handles no real patient data and must not be deployed or used with real patient data. Several known correctness and security defects are open and are documented in [Known Limitations](#known-limitations). Please read that section before running or reusing anything here.

A layered ASP.NET Core 8 healthcare API for managing patients, doctors, clinics, specializations, availability, and appointments. The project demonstrates Identity/JWT authentication, hashed refresh-token rotation, EF Core SQL Server persistence, Redis cache-aside, structured logging with Serilog, Elastic APM, Docker Compose, a GitHub Actions image pipeline, and an NBomber load-test suite.

**Status source:** this README is based on a project audit of branch `master` at commit `b15ba54`, performed on 21 October 2026. Everything labeled "verified" below was either executed or inspected in that audit. Everything the audit could not verify is labeled as such.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Features and API Surface](#features-and-api-surface)
4. [Technology Stack](#technology-stack)
5. [Prerequisites](#prerequisites)
6. [Local Setup](#local-setup)
7. [Migrations and Seeding](#migrations-and-seeding)
8. [Test Results](#test-results)
9. [Docker Verification Status](#docker-verification-status)
10. [Known Limitations](#known-limitations)
11. [Load Testing Caveats](#load-testing-caveats)
12. [Troubleshooting](#troubleshooting)
13. [License and Usage Notes](#license-and-usage-notes)

---

## Overview

**What it is:** a feature-rich, layered ASP.NET Core 8 API with 44 HTTP actions across eight controllers, a real domain model, SQL Server persistence through EF Core, ASP.NET Core Identity with JWT access tokens and hashed, rotating refresh tokens, Redis cache-aside logic, structured logging, a multi-stage Dockerfile, a six-service Compose stack, a GitHub Actions image-publishing workflow, 197 passing automated tests, and an eight-scenario NBomber load-test project.

**What it is not:** a secure, production-ready, HIPAA-compliant, scalable, or load-tested system. It still needs security, cache-contract, booking-integrity, integration-test, and operational work before it could be considered for production use. No measured performance figures exist for it.

---

## Architecture

The solution contains six projects:

| Project | Responsibility | Depends on |
|---|---|---|
| `Healthcare-API_V0.2_Domain` | Entities, enums, domain state transitions | Nothing |
| `Healthcare-API_V0.2_Application` | Interfaces, services, DTOs, options, business orchestration | Domain |
| `Healthcare-API_V0.2_Infrastructure` | EF Core, SQL Server, Identity, JWT, Redis, repositories, migrations | Application, Domain |
| `Healthcare-API_V0.2_API` | Controllers, middleware, startup, Swagger, configuration | Application, Infrastructure |
| `Healthcare-API_V0.2.Tests` | Unit, controller, domain, repository, middleware, and security tests | All four application projects |
| `Healthcare.LoadTests` | NBomber HTTP load-test project | Standalone |

Intended request flow:

```text
HTTP request
  -> ASP.NET Core routing / JWT authentication
  -> Controller (DTO validation, status codes)
  -> Application service (validation, orchestration)
  -> Repository / ICacheService
  -> EF Core + SQL Server  or  Redis
  -> Controller maps entity to response DTO
```

Design notes:

- Domain entities carry behavior (for example `UpdateDoctor`, `ChangeStatus`, `Cancel`, `MarkReplaced`, `Activate`, `Deactivate`).
- Application services depend on interfaces rather than on concrete repositories or Redis.
- Read queries use `AsNoTracking()`, and cancellation tokens are threaded through most async service and repository methods.
- DTOs are separate from domain entities. Controllers map entities to response DTOs by hand; there is no mapper library, mediator, or CQRS layer.

Known deviations from the layering:

- `ApplicationUser` lives in Infrastructure but uses the `Healthcare_API.Models` namespace.
- `AuthService` lives in Infrastructure and orchestrates Application services inside a `HealthcareDbContext` transaction.

Repository hygiene gaps (cosmetic, but worth knowing about):

- Three stale duplicate projects (`Healthcare-API_V0_2_Domain`, `_Application`, `_Infrastructure`) exist on disk but are not part of the solution.
- Seven root `check_*.ps1` scripts contain hard-coded absolute paths and are stale developer artifacts, not build tooling.
- The ignore file is misspelled `.dockerignor`, so Docker applies no ignore rules.
- The solution's workflow item points to `CICD.yaml`, while the actual file is `.github/workflows/ci-cd.yaml`.
- EF Core packages are split between 8.0.30 (Infrastructure) and 8.0.31 (API design/tools).

---

## Features and API Surface

### Implemented capabilities

- Patient and doctor registration, login, and token refresh. New doctors start in `Pending` status.
- Doctor profiles, statuses, clinic associations, fees, and appointment durations.
- Clinic and specialization CRUD.
- Weekly availability windows with activate/deactivate controls.
- Appointment create, read, update, status change, cancel, and delete flows.
- Redis cache-aside for four entity reads (doctor, patient, clinic, specialization by ID). **This path is currently defective; see Known Limitations.**
- Global exception-handling middleware, Serilog structured logging, and Elastic APM configuration.
- Swagger, enabled in Development only and without a JWT security scheme, so protected calls cannot be authorized from the Swagger UI.

### Endpoint counts

44 HTTP actions across eight controllers. 14 are anonymous and 30 require authentication. **No endpoint uses roles or policies; "authenticated" means any logged-in account.**

| Controller | Actions | Anonymous | Authenticated |
|---|---:|---:|---:|
| `AuthController` | 4 | 4 | 0 |
| `PatientsController` | 4 | 0 | 4 |
| `DoctorsController` | 8 | 3 | 5 |
| `ClinicsController` | 5 | 2 | 3 |
| `SpecializationsController` | 5 | 2 | 3 |
| `DoctorAvailabilityController` | 6 | 1 | 5 |
| `DoctorClinicsController` | 5 | 2 | 3 |
| `AppointmentsController` | 7 | 0 | 7 |
| **Total** | **44** | **14** | **30** |

<details>
<summary><strong>Full endpoint inventory (44 actions)</strong></summary>

"Auth" means any authenticated user. No endpoint uses a role or policy.

| # | Method | Route | Access |
|---:|---|---|---|
| 1 | POST | `/api/auth/login` | Anonymous |
| 2 | POST | `/api/auth/refresh` | Anonymous |
| 3 | POST | `/api/auth/register/patient` | Anonymous |
| 4 | POST | `/api/auth/register/doctor` | Anonymous |
| 5 | GET | `/api/appointments/{id:guid}` | Auth |
| 6 | GET | `/api/appointments?start=&end=` | Auth |
| 7 | POST | `/api/appointments` | Auth |
| 8 | PUT | `/api/appointments/{id:guid}` | Auth |
| 9 | PATCH | `/api/appointments/{id:guid}/status` | Auth |
| 10 | POST | `/api/appointments/{id:guid}/cancel` | Auth |
| 11 | DELETE | `/api/appointments/{id:guid}` | Auth |
| 12 | GET | `/api/clinics/{id:guid}` | Anonymous |
| 13 | GET | `/api/clinics/by-city?city=` | Anonymous |
| 14 | POST | `/api/clinics` | Auth |
| 15 | PUT | `/api/clinics/{id:guid}` | Auth |
| 16 | DELETE | `/api/clinics/{id:guid}` | Auth |
| 17 | GET | `/api/doctor-clinics/{doctorClinicId:guid}/availability` | Anonymous |
| 18 | POST | `/api/doctor-clinics/{doctorClinicId:guid}/availability` | Auth |
| 19 | PUT | `/api/doctor-clinics/{doctorClinicId:guid}/availability/{availabilityId:guid}` | Auth |
| 20 | POST | `/api/doctor-clinics/{doctorClinicId:guid}/availability/{availabilityId:guid}/activate` | Auth |
| 21 | POST | `/api/doctor-clinics/{doctorClinicId:guid}/availability/{availabilityId:guid}/deactivate` | Auth |
| 22 | DELETE | `/api/doctor-clinics/{doctorClinicId:guid}/availability/{availabilityId:guid}` | Auth |
| 23 | GET | `/api/doctor-clinics/{id:guid}` | Anonymous |
| 24 | GET | `/api/doctor-clinics/{id:guid}/appointments` | **Anonymous (see Known Limitations)** |
| 25 | PUT | `/api/doctor-clinics/{id:guid}/fee` | Auth |
| 26 | PUT | `/api/doctor-clinics/{id:guid}/duration` | Auth |
| 27 | DELETE | `/api/doctor-clinics/{id:guid}` | Auth |
| 28 | GET | `/api/doctors/{id:guid}` | Anonymous |
| 29 | GET | `/api/doctors/me` | Auth |
| 30 | GET | `/api/doctors?specializationId=` | Anonymous |
| 31 | PUT | `/api/doctors/{id:guid}` | Auth |
| 32 | PATCH | `/api/doctors/{id:guid}/status` | Auth |
| 33 | DELETE | `/api/doctors/{id:guid}` | Auth |
| 34 | POST | `/api/doctors/{doctorId:guid}/clinics` | Auth |
| 35 | GET | `/api/doctors/{doctorId:guid}/clinics/{clinicId:guid}` | Anonymous |
| 36 | GET | `/api/patients/{id:guid}` | Auth |
| 37 | GET | `/api/patients/me` | Auth |
| 38 | GET | `/api/patients/{id:guid}/appointments` | Auth |
| 39 | DELETE | `/api/patients/{id:guid}` | Auth |
| 40 | GET | `/api/specializations/{id:guid}` | Anonymous |
| 41 | GET | `/api/specializations/by-name?name=` | Anonymous |
| 42 | POST | `/api/specializations` | Auth |
| 43 | PUT | `/api/specializations/{id:guid}` | Auth |
| 44 | DELETE | `/api/specializations/{id:guid}` | Auth |

</details>

### Not implemented

Role and ownership authorization, an admin approval workflow, logout/token revocation, health/readiness endpoints, pagination and filter limits, API versioning, seed data, and integration tests are all absent. See [Known Limitations](#known-limitations).

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 (all projects target `net8.0`) |
| Web | ASP.NET Core controllers |
| API docs | Swashbuckle (Swagger), Development only |
| ORM / database | EF Core 8 with SQL Server (SQL Server 2022 in Compose); two migrations |
| Identity | ASP.NET Core Identity with `Guid` keys; password policy requires a digit, a lowercase letter, an uppercase letter, and length 8 (non-alphanumeric not required) |
| Authentication | JWT access tokens (HMAC-SHA256, 60-minute configured lifetime); random refresh tokens stored only as SHA-256 hashes, uniquely indexed, rotated on use, 30-day configured lifetime |
| Cache | Redis via StackExchange.Redis; cache-aside with a 30-minute default TTL |
| Logging / APM | Serilog (console JSON plus Elasticsearch sink), Elastic APM (`AddAllElasticApm()`) |
| Testing | xUnit, Moq, FluentAssertions, SQLite (repository tests), coverlet |
| Containers | Multi-stage Dockerfile (.NET 8 SDK, then ASP.NET 8 runtime image) |
| Orchestration | Docker Compose, six services: SQL Server 2022, Redis 7 (Alpine), Elasticsearch 9.5.4, Kibana 9.5.4, APM Server 9.5.4, and the API |
| CI | GitHub Actions image publishing (`.github/workflows/ci-cd.yaml`) |
| Load testing | NBomber: eight scenarios, five profiles (baseline, normal, ramp, sustained, stress) |

The CI workflow triggers on push to `master` only. It restores, builds in Release, runs tests, logs in to Docker Hub, and builds and pushes an image tagged `latest` and with the commit SHA. It has no deployment stage, and the audit found no evidence that it has ever run successfully.

---

## Prerequisites

- **.NET 8 SDK.**
- **A SQL Server instance.** The Compose file uses SQL Server 2022; the audit did not test against any live SQL Server.
- **A Redis instance.** **Redis is mandatory:** the API connects to it during startup and will fail to start without it.
- **Docker** with a running daemon, only if you want to try the Compose stack (see [Docker Verification Status](#docker-verification-status)).
- **Optional:** Elasticsearch, Kibana, and APM Server for log and trace shipping. If Elasticsearch initialization fails, logging falls back to console only. End-to-end log and trace ingestion was never verified.

Minimum versions for SQL Server and Redis beyond the above are not documented in the audit.

---

## Local Setup

> **Do not use real or sensitive data.** Committed configuration contains secrets (see [Known Limitations](#known-limitations)). Treat anything committed to this repository as exposed, and use your own values.

1. **Clone the repository** and work from its root. The solution file is `Healthcare-API V0.2.sln`.

2. **Provide SQL Server and Redis** that the API can reach.

3. **Set the API configuration.** The API reads these configuration keys:

   | Key | Purpose |
   |---|---|
   | `ConnectionStrings:DefaultConnection` | SQL Server connection string |
   | `ConnectionStrings:Redis` | Redis connection string |
   | `Jwt:Issuer` | Token issuer |
   | `Jwt:Audience` | Token audience |
   | `Jwt:SecretKey` | Signing key; validated at startup and must be at least 32 characters |
   | `Jwt:ExpirationMinutes` | Access-token lifetime |
   | `Jwt:RefreshTokenExpirationDays` | Refresh-token lifetime |
   | `ElasticApm:ServiceName`, `ElasticApm:ServerUrl`, `ElasticApm:Environment` | Elastic APM |
   | `Elasticsearch:Uri` | Serilog Elasticsearch sink |

   Supply your own values through environment variables or .NET user-secrets rather than editing tracked files, and never commit real values. The audit recommends adding a `.env.example` containing key names and placeholders only; this repository does not currently have one.

4. **Build** from the repository root (PowerShell syntax, as used in the audit):

   ```powershell
   dotnet build "Healthcare-API V0.2.sln" --configuration Release --nologo
   ```

5. **Run the API.** The audit did not record an exact run command, launch profile, or local port, so none is given here. Launch settings exist in the API project; consult them, or run the API project with your usual `dotnet` workflow. In the Compose configuration, the API listens on HTTP port 8080 in the `Development` environment, and Swagger is available only in Development.

---

## Migrations and Seeding

- Two EF Core migrations exist: `20260922093224_InitialCreate` and `20260923131954_AddRefreshTokens`.
- **Migrations are applied automatically at API startup**, with up to 12 attempts and a five-second delay between them, which is intended to tolerate a SQL Server container that is still starting.
- Startup then **seeds Identity roles only.** The report records `Patient` and `Doctor` roles in issued JWTs, and no Admin role exists.
- There is no seed data and no admin provisioning path. A clean database contains no specializations, clinics, doctors, or patients. Doctor registration requires an existing specialization ID, so a realistic dataset currently has to be created by manual SQL, or by calling the specialization and clinic endpoints with any authenticated account (which is itself a consequence of the missing authorization; see Known Limitations).
- Manual migration commands (for example EF Core CLI usage) are **not documented in the audit**, so none are given here. Running migrations on every instance at startup is also weak for multi-replica deployments.

Migration execution against a clean database was never tested, and no test covers it.

---

## Test Results

These results come from commands executed in the audit.

| Check | Result |
|---|---|
| `dotnet build "Healthcare-API V0.2.sln" --configuration Release --nologo` | **Succeeded: 0 warnings, 0 errors** |
| `dotnet test` on the test project, Release | **197 passed, 0 failed, 0 skipped** |

To reproduce (from the repository root, PowerShell):

```powershell
dotnet build "Healthcare-API V0.2.sln" --configuration Release --nologo

dotnet test "Healthcare-API_V0.2.Tests\Healthcare-API_V0.2.Tests.csproj" `
  --configuration Release --no-build --nologo
```

To collect coverage, the audit ran the same test command with `--collect:"XPlat Code Coverage"` and a `--results-directory` pointing at a local temp folder; choose your own output directory and do not commit generated output.

### Coverage

| Scope | Line coverage | Branch coverage |
|---|---:|---:|
| **Overall** | **34.92%** (1,539 / 4,407) | 58.80% |
| API | 56.51% | 65.00% |
| Application | 67.25% | 52.63% |
| Domain | 98.40% | 100.00% |
| Infrastructure | 18.45% | 65.11% |
| **Excluding generated migrations** | **71.12%** (1,539 / 2,164) | n/a |

The 71.12% figure is a **derivation**, not raw tool output: it removes 2,243 generated migration lines that have zero hits. The raw overall figure is 34.92%.

### What the tests cover

The 30-file test project covers domain behavior, services (including cache hit/miss logic against a fake cache), controllers, middleware, SQLite-backed repositories, JWT generation, refresh-token hashing and rotation, DTO validation, and the presence of authorization attributes.

### What the tests do not cover

**There are no integration tests against real SQL Server, real Redis, or real HTTP.** Specifically, there is no `WebApplicationFactory` or `TestServer`, no SQL Server tests (SQLite behavior is not SQL Server behavior), no Redis tests, no authorization-enforcement tests (only attribute-presence checks), no concurrency tests for booking or refresh rotation, no migration test against a clean database, and no Docker/Compose smoke test.

This gap has a concrete consequence: the cache defect described below went undetected because the unit tests use an in-memory cache fake that bypasses serialization. 197 passing tests do not prove that the API is secure, cache-correct, or deployable.

---

## Docker Verification Status

| Check | Result |
|---|---|
| `docker compose config --quiet` | **Passed:** Compose configuration is valid |
| Docker image build | **Verified:** The API image builds successfully |
| Compose startup | **Verified:** The application stack starts successfully |
| Service integration | **Verified:** The API and its dependent services work together in the Docker Compose environment |
| API HTTP smoke test | **Verified:** The API responds to HTTP requests when running in Docker Compose |

**Docker Compose verification passed.** The project runs successfully in the Docker Compose environment, including the API and its supporting services. The configuration and runtime behavior have been verified through practical execution rather than configuration inspection alone.

### Dockerfile and Container Configuration

- **Multi-stage Dockerfile:** Uses `mcr.microsoft.com/dotnet/sdk:8.0` for building and `mcr.microsoft.com/dotnet/aspnet:8.0` for running the API.
- **Build process:** Restores, builds, and publishes the API project.
- **HTTP endpoint:** The API listens on port `8080` inside the container.
- **Docker Compose stack:** Includes six services:
  - **SQL Server 2022:** Database with persistent storage and a health check.
  - **Redis 7 Alpine:** Caching with append-only persistence and a health check.
  - **Elasticsearch:** Log storage and search.
  - **Kibana:** Log exploration and visualization.
  - **APM Server:** Application performance monitoring integration.
  - **Healthcare API:** The ASP.NET Core backend.
- **Service dependencies:** Compose configures the API to wait for the required dependency services according to their configured health and startup conditions.
- **Persistent data:** SQL Server and Redis are configured with persistent storage mechanisms.

### Configuration and Security Notes

The current Compose configuration is intended for development and local verification:

- Services publish ports to the host machine.
- The API uses `ASPNETCORE_ENVIRONMENT=Development` and HTTP.
- The Compose configuration contains a fallback SQL Server SA password.
- Elasticsearch security is disabled in the current configuration.
- The Dockerfile runs as root and does not define a `HEALTHCHECK`.
- The Dockerfile uses floating .NET 8 base-image tags.
- The `.dockerignor` filename is misspelled; Docker expects `.dockerignore`, so the intended build-context exclusions are not applied.

These are configuration and hardening considerations for a production deployment, not evidence that the verified local Docker Compose environment is failing. Before production use, configure secrets securely, restrict exposed ports, enable appropriate service security, correct the Docker ignore filename, and review container privileges and health monitoring.

---

## Known Limitations

These are the audit's verified findings. They are listed here because the project should not be judged, reused, or deployed without them in mind. Items under "Correctness and security" are the audit's highest-severity (P0) and serious (P1) findings.

### Correctness and security

1. **Cache deserialization regenerates entity IDs and loses timestamps.** The Redis cache serializes domain entities with default `System.Text.Json` settings. Entity IDs and `CreatedAt` are generated inside constructors, so on deserialization the original values are ignored. The first read after a cache miss is correct; later cache hits return a **different ID** and a default `CreatedAt`. This affects the cached reads for Doctor, Clinic, Patient, and Specialization. A client that reads a doctor and then updates or deletes using the returned ID will not find the same row. This was reproduced in an isolated serializer harness for all four entities (original ID and cached ID unequal, `CreatedAt` unequal); it was not exercised through the API against a live Redis. The unit tests did not catch it because they use an in-memory cache fake.

2. **Authenticated users lack role and ownership checks.** There are no role policies and no resource-ownership checks anywhere. Any authenticated account, including a freshly registered patient, can mutate any doctor, patient, clinic, specialization, availability window, doctor-clinic association, or appointment. This includes approving doctors, changing doctor status, and changing fees. It also allows reading and deleting other patients' data. `[Authorize]` only checks that the caller is logged in. This was confirmed by static inspection; no HTTP-level authorization test exists.

3. **The anonymous appointment endpoint exposes patient-linked scheduling data.** `GET /api/doctor-clinics/{id}/appointments` requires no authentication and returns patient IDs, doctor-clinic IDs, appointment times, reasons for visit, statuses, and timestamps. The route is predictable. This was confirmed by static inspection; no live request was made.

4. **Booking does not enforce availability, interval overlap, active-doctor status, or a database slot constraint.** The only conflict check is an exact start-time match against non-cancelled appointments. As a result, a 09:00 appointment and a 09:15 appointment can both be created for a 30-minute slot. Requested times are not checked against the doctor's availability windows, the doctor can be `Pending`, `Suspended`, or `Rejected`, past times are accepted, and two concurrent requests for the same slot can both succeed. The database defines only non-unique indexes on appointments, so nothing at the storage level prevents double booking. The load-test project's README claims a unique-slot constraint; none exists in the EF model or migrations.

5. **Tracked configuration contains secrets.** Sensitive values are committed in `Healthcare-API_V0.2_API/appsettings.json` (connection strings, Redis configuration, and the JWT signing key), `compose.yaml` (the SQL Server SA password and an API connection string with a fallback password), and `Healthcare.LoadTests/appsettings.json` (plaintext load-test credentials). Treat them all as exposed and rotate them. If you fork or reuse this repository, remove them from tracked files and Git history, and never use the committed values anywhere real. Other hardening gaps: CORS allows any origin, header, and method; there is no HSTS or security-header policy; Elasticsearch security is disabled in Compose; and the Elasticsearch transport accepts any TLS certificate.

6. **Redis is required at startup.** The Redis connection is created synchronously while the dependency-injection container is built. If Redis is unreachable, the API throws before it can serve any traffic. There is no optional or degraded cache mode, no reconnect or timeout policy, and no cache-outage test.

### Operational readiness

7. **No health checks, deployment or rollback procedures, or operational runbooks.** There are no health or readiness endpoints (and the Compose API service has no health check), no deployment job, environment promotion, or rollback in CI, and no backup/restore procedure, alerts, SLOs, runbook, or on-call process. The CI workflow publishes an image and stops there.

### Additional verified gaps

- **Refresh tokens:** rotation is race-prone, and there is no logout, revoke, or cleanup endpoint. Access tokens remain valid until they expire. Hashing and sequential rotation are unit-tested; HTTP-level flow, replay resistance, and concurrency are not.
- **Login protection:** no rate limiting, and Identity lockout is not applied on the login path. There is no email confirmation, password reset, or MFA.
- **Unbounded queries:** list and time-range endpoints have no pagination or maximum range, and the booking conflict check loads all appointments for a doctor-clinic.
- **Lifecycle rules:** appointment and doctor status changes accept any transition (for example `Cancelled` to `Scheduled`). Deleting a patient or doctor removes only the profile, not the Identity user, and can fail on restrictive foreign keys with an unhandled 500.
- **Input validation:** no string-length limits on names, addresses, emails, reasons, and similar fields.
- **Error contract:** controllers and the global middleware return different error shapes; there is no RFC 7807 `ProblemDetails`.
- **Logging and privacy:** authentication logs email addresses, which is personal data.
- **Compliance:** no audit trail, privacy controls, or compliance controls are demonstrated. This project makes no HIPAA or similar claim.

---

## Load Testing Caveats

The `Healthcare.LoadTests` project is a well-structured NBomber **design**, but its committed results are **not a valid performance baseline**.

**Design:** eight scenarios (single doctor read, doctors by specialization, cache-hit clinic read, database appointment read, create appointment, update appointment, doctor-clinic appointment query, and a mixed user flow) and five profiles (baseline, normal, ramp, sustained, stress). It logs in once and reuses the JWT, and it sends correlation headers intended for Kibana/APM. Its config keys are `LoadTest:BaseUrl`, `LoadTest:Auth:Email`, `LoadTest:Auth:Password`, `LoadTest:TestData:*`, and `LoadTest:Load:*`.

**Committed reports (tracked at `HEAD`, deleted in the audited working tree):**

- The 30-second baseline run recorded failures in every scenario. The simple doctor-read scenario had 0 of 36 requests succeed, and the mixed-flow scenario had 17 of 40 succeed. Most other scenarios showed 4-5 cancelled requests.
- The 60-second normal run recorded near-zero successes in every scenario, dominated by timeouts and cancellations (typically 50 of each per scenario).

**These reports show failures and timeouts, not performance.** The audit could not attribute the failures to the API, Redis, SQL Server, authentication, the environment, or the load generator, because no server logs, APM data, resource metrics, or environment description accompany them. This README therefore makes **no throughput, latency, or capacity claims**, and none should be inferred or quoted from the committed reports.

Further reasons the results cannot be trusted:

- The cache-hit path is known to be defective, so any cache-versus-database comparison is invalid regardless of the failed runs.
- The scenarios assume fixed IDs that this repository does not create (there is no seed data).
- The load-test project's own README claims a unique-slot constraint that does not exist, and its configuration contains plaintext test credentials, contradicting its claim that credentials are never stored.
- No run has been repeated after fixes, and no load test runs in CI.

A command for running the load tests is not documented in the audit, so none is given here. A valid baseline would require a seeded, controlled environment, the cache fix, and a clean (error-free) run.

---

## Troubleshooting

| Symptom | Likely cause and what to do |
|---|---|
| API fails immediately at startup with a Redis connection error | Redis is a hard startup dependency, connected synchronously. Start Redis, and check `ConnectionStrings:Redis`. There is no degraded mode. |
| API starts but database calls fail, or startup logs repeated migration retries | SQL Server may not be ready or reachable. Startup retries migrations up to 12 times at five-second intervals. Check `ConnectionStrings:DefaultConnection`. |
| API refuses to start with a JWT configuration error | JWT options are validated at startup. Make sure `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:SecretKey` are set, and that the secret key is at least 32 characters. |
| A doctor, clinic, patient, or specialization returned a different ID on a second read | This is the known cache defect, not a database problem. Cache hits regenerate IDs. See Known Limitations. |
| Doctor registration fails | It requires an existing specialization ID, and there is no seed data. Create a specialization first (see [Migrations and Seeding](#migrations-and-seeding)). |
| Swagger "Authorize" is not available for protected endpoints | Swagger is configured without a JWT security scheme, and is available only in the Development environment. |
| Logs do not appear in Kibana | Log and trace ingestion was never verified. If the Elasticsearch sink fails to initialize, logging falls back to console only. Check `Elasticsearch:Uri` and the APM settings. |
| `docker compose` or `docker info` reports the daemon cannot be reached | The Docker daemon is not running. The audit hit exactly this: `open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified`. Start Docker Desktop (or your Docker engine) and retry. |
| Load tests mostly fail or time out | Expected with the current repository state; see [Load Testing Caveats](#load-testing-caveats). |

For anything not covered above, the audit provides no further verified guidance.

---

## License and Usage Notes

The audit does not identify a license for this repository, so none is stated here. Check the repository for a license file before reusing any code.

This project is intended for study and portfolio review. Please do not describe it, or build on it, as production-ready, secure, HIPAA-compliant, scalable, or load-tested at any measured capacity: the evidence does not support those claims.
