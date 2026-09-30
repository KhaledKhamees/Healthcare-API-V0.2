# Healthcare.LoadTests — NBomber Performance Suite

A small, representative NBomber (v6.6.0, .NET 8) load-testing project for the
Healthcare API. It does **not** test every endpoint — it tests the *performance
characteristics* that matter: reads, lists, cache hits, DB paths, writes,
complex queries, and a realistic mixed flow, under five load profiles.

> You are learning performance testing here, so every scenario and profile has a
> **WHY** comment in code. Start by reading `Scenarios/`, then come back here
> to run things.

---

## 1. What this project is (and is not)

- **Is:** a load *generator*. It fires realistic HTTP traffic at a running API
  and measures **client-observed** latency/throughput/errors (P50/P95/P99, RPS,
  error rate). Reports land in `./reports` (txt/csv/html/md).
- **Is not:** application observability. The API already ships Elastic APM +
  Elasticsearch + Kibana — *that* stack tells you **where** time goes inside
  the app (service method → EF Core → SQL/Redis). NBomber tells you **what the
  user feels**. You need both; see §11.

```
NBomber P95 latency  →  APM transaction  →  service method  →  EF Core span
        (what)              (where)              (why slow?)
```

---

## 2. Why these scenarios were selected

| # | Scenario | Endpoint | What it measures | Architecture exercised |
|---|----------|----------|------------------|------------------------|
| 1 | `simple_read_doctor_by_id` | `GET api/doctors/{id}` (anon) | Single-row read, small response | DoctorService cache-aside → Redis **or** SQL |
| 2 | `list_query_doctors_by_specialization` | `GET api/doctors?specializationId=` (anon) | Filtered multi-row + serialization cost | EF `WHERE`, **no cache**, JSON array |
| 3 | `cache_hit_clinic_by_id` | `GET api/clinics/{fixedId}` (anon) | Cached-path latency (same id → Redis hit) | ClinicService → Redis `clinic:{id}` |
| 4 | `db_read_appointment_by_id` | `GET api/appointments/{id}` (auth) | Pure DB path (never cached) — compare vs #3 | JWT → AppointmentService → SQL |
| 5 | `create_appointment` | `POST api/appointments` (auth) | Write: FK checks + conflict check + INSERT | Transaction, unique-slot constraint |
| 6 | `update_appointment` | `PUT api/appointments/{id}` (auth, TEST rows) | Write without table growth | Re-validate + conflict check + UPDATE |
| 7 | `complex_query_doctor_clinic_appointments` | `GET api/doctor-clinics/{id}/appointments` (anon) | Relationship query (JOIN/filter/list) | Uncached multi-row EF query |
| 8 | `mixed_realistic_user_flow` | all of the above, weighted | Cross-endpoint contention on shared resources | SQL pool + Redis + thread pool together |

Deliberately **excluded**: DELETE (would destroy the seed/test rows the other
scenarios need), per-endpoint duplicates, and a login-per-request pattern (see
§5 — we log in **once** and reuse the JWT, like a real client).

**For each scenario, what "bad" looks like and what to check next is written in
the XML-doc comment above its factory method** in `Scenarios/`.

---

## 3. Prerequisites

1. .NET 8 SDK (`dotnet --version` → 8.x).
2. The API running locally, e.g. `dotnet run` on the API project
   (default `http://localhost:5208` — must match `LoadTest:BaseUrl`).
3. SQL Server + Redis reachable by the **API** (the load test only talks HTTP).
4. A test user in the API database (any role) for the authenticated scenarios.
   Register one via Swagger (`POST api/auth/register/patient`), then keep its
   email/password for step 5.
5. (Optional, recommended) Elastic APM Server + Kibana running so you can
   correlate (§11).

---

## 4. Configure the API URL

In `appsettings.json` → `LoadTest:BaseUrl`, or override without editing files:

```powershell
$env:LOADTEST_BASEURL = "http://localhost:5208"
```

---

## 5. Configure test data (required — the run refuses placeholders)

The run validates config and exits with code `2` listing what's missing. Fill in
`LoadTest:TestData` with **real ids from your database**:

```sql
SELECT TOP 5 DoctorId FROM Doctors;
SELECT TOP 5 ClinicId FROM Clinics;
SELECT TOP 5 SpecializationId FROM Specializations;
SELECT TOP 25 AppointmentId FROM Appointments;
SELECT TOP 25 DoctorClinicId FROM DoctorClinics;
SELECT TOP 1 City FROM Clinics;  -- pick a city that exists
```

Rules:

- `DoctorIds` / `AppointmentIds` / `DoctorClinicIds` / `UpdateAppointmentIds`:
  pools of **20+ ids** are ideal so concurrent users spread across rows.
- `CacheHitClinicId`: exactly **one** stable clinic (same id every request =
  Redis hit).
- `PatientId` + `DoctorClinicId`: a **dedicated TEST patient and TEST
  doctor-clinic** used only for created appointments (creates insert real rows).
- `UpdateAppointmentIds`: **dedicated TEST appointments** safe to reschedule.
  Never point this at real patient bookings (concurrent updates are
  last-writer-wins — no concurrency token in this API).
- `City`: a city that exists in `Clinics`, otherwise `by-city` returns `[]`
  (fast but unrealistic — tiny responses hide serialization cost).

Credentials are **never** stored in the file — use the environment:

```powershell
$env:LOADTEST_EMAIL = "loadtest@example.com"
$env:LOADTEST_PASSWORD = "Test1234"
```

Without credentials the run still works, but authenticated scenarios report
`NO_AUTH_TOKEN` failures instead of real results.

### Cleaning up created appointments

`create_appointment` inserts real rows (by design — there is no safe
auto-cleanup). Purge test-created rows periodically:

```sql
-- Delete load-test bookings for your dedicated TEST patient:
DELETE FROM Appointments WHERE PatientId = '<TestData.PatientId>';
```

---

## 6–9. Running the profiles

```powershell
cd Healthcare.LoadTests
dotnet run -- --profile baseline    # 5 users, 30 s — correctness + baseline P50/P95/P99
dotnet run -- --profile normal      # 50 users, 60 s — a realistic day
dotnet run -- --profile ramp        # 10 → 25 → 50 → 100 users — find the knee
dotnet run -- --profile sustained   # 50 users for 5 min — detect slow degradation
dotnet run -- --profile stress      # ramp to 200 + hold — the ceiling (tune first!)
```

Run them **in order**. Each profile gates the next: don't run `stress` while
`ramp` already shows errors. All tunables (`NormalUsers`, `RampSteps`,
`SustainedMinutes`, `StressUsers`, …) live under `LoadTest:Load`.

> **Stress-testing safely:** start with `StressUsers: 100` on a dev machine
> (local SQL Express + Redis share your CPU). Raise it only while watching
> machine resources — you want the *API's* limit, not your laptop's.

---

## 10. Reading NBomber results

Reports are written to `./reports` (`healthcare-api-<profile>.*` in
txt/csv/html/md). Per scenario you get:

| Metric | Meaning | How to read it |
|--------|---------|----------------|
| RPS / requests | Throughput + volume | Should scale with users until the knee; flat RPS + rising latency = saturated |
| ok / fail count, error rate | Success vs failure | Baseline must be ~0% errors. Any 5xx = bug under load. 409s on create = slot collisions (expected, see below) |
| mean / min / max | Average + extremes | Mean hides outliers; min shows floor (network+framework); max shows worst case |
| **P50** (median) | Typical user experience | Half of requests were faster than this |
| **P95** | Near-worst case most users feel | **The key metric.** If P95 is 10× P50, a minority suffers badly — investigate |
| **P99** | Worst 1% | GC pauses, lock contention, cold cache, connection-pool waits live here |
| data transfer | Response sizes | Growing sizeBytes + growing latency = payload/serialization cost |

**Healthy pattern:** P50 ≈ P95, RPS grows linearly with users, errors ≈ 0.
**Knee pattern (ramp):** at some step P95 bends upward while RPS flattens —
that step is your current capacity. **Degradation pattern (sustained):** P95
drifts up over minutes at constant users — leak, pool exhaustion, or cache
churn; check APM + machine metrics over the same window.

**About 409s on `create_appointment`:** concurrent users occasionally pick the
same slot for the same doctor-clinic and the API correctly returns
`409 Conflict`. A *low* 409 rate is expected business behavior, not a bug — but
it is still counted as Fail so the rate stays visible. If it dominates, widen
the slot window in `TestDataProvider` or lower create concurrency.

---

## 11. Correlating with Kibana / Elastic APM

Every request carries `X-Load-Test: true` plus `X-Load-Test-Scenario:
<scenario-name>`, so you can filter load traffic in Kibana (`http.request.headers.*
or labels`) and match it to APM transactions by timestamp.

Practical workflow when `P95 suddenly increases`:

```
1. NBomber report → which scenario + at which load step did P95 bend?
2. Kibana APM → filter service "healthcare-api" to that time window.
3. APM transaction list → is the slow transaction the same endpoint?
4. Transaction waterfall → which SPAN dominates: redis? mssql? jwt/auth?
   - redis slow  → Redis latency/evictions (compare cache_hit vs db_read).
   - mssql slow  → the EF query: check its SQL in the span, then the
                   execution plan / missing index in SQL Server.
   - no slow span, but high transaction time → thread-pool starvation / GC
                   (check machine CPU + GC metrics, not the database).
5. If APM looks fine but NBomber is slow → suspect the LOAD GENERATOR
   (client CPU saturated) or the network — NBomber measures client-observed
   time, which includes everything.
```

### Whose bottleneck is it? (do not guess — measure)

| Symptom | Likely culprit | Confirm with |
|---------|----------------|--------------|
| `cache_hit` slow, `db_read` fine | Redis | APM redis spans, Redis latency |
| `db_read`/writes slow, `cache_hit` fine | SQL Server | APM mssql spans, SQL wait stats, pool usage |
| ALL scenarios slow incl. cache hits | API host (CPU/GC/thread pool) | Machine metrics, APM breakdown with no slow span |
| Load-test machine CPU ~100% | Load generator itself | Task Manager + NBomber RPS flat while API idle |
| Slow only in `mixed`, fine isolated | Shared resource contention | SQL connection pool (default 100!), Redis, thread pool |

---

## 12. Project layout

```
Healthcare.LoadTests/
├── Program.cs                  -- profile selection, login-once, run
├── appsettings.json            -- BaseUrl, test-data ids, load tunables (no secrets)
├── Configuration/LoadTestSettings.cs
├── Helpers/
│   ├── HttpClientProvider.cs   -- ONE shared HttpClient (socket reuse)
│   ├── AuthHelper.cs           -- login once, reuse JWT
│   ├── TestDataProvider.cs     -- id pools + unique-slot generation + validation
│   └── ApiClient.cs            -- HTTP → NBomber Response (never hides failures)
├── Models/RequestModels.cs     -- minimal request/response DTOs (camelCase JSON)
└── Scenarios/
    ├── ReadScenarios.cs        -- simple read + list query
    ├── CacheScenarios.cs       -- cache hit vs DB path
    ├── WriteScenarios.cs       -- create + update (DELETE excluded on purpose)
    ├── ComplexQueryScenarios.cs
    ├── MixedWorkloadScenarios.cs -- weighted realistic flow (weights documented)
    └── LoadProfiles.cs         -- baseline/normal/ramp/sustained/stress
```

Stack: `NBomber 6.6.0` + `Microsoft.Extensions.Configuration 9.x`
(NBomber 6.6 requires Config 9.x — do not downgrade to 8.x, restore will fail).

## 13. Learning checklist

- [ ] Run `baseline`, get 0 errors, note P50/P95 per scenario.
- [ ] Compare `cache_hit_clinic_by_id` vs `db_read_appointment_by_id` — the gap
      is what your Redis cache buys you.
- [ ] Run `ramp`, find the step where P95 bends — that's today's capacity.
- [ ] Pick the worst scenario at the knee, follow the §11 workflow in Kibana,
      and identify the dominating span (redis / mssql / other).
- [ ] Run `sustained` and check whether P95 at minute 5 > minute 1.
