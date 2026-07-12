# CreditFlow

**Enterprise-grade loan origination & underwriting API**, built with ASP.NET Core (.NET 10) as a demonstration of Clean Architecture, Domain-Driven Design, and CQRS applied to a domain with genuine business complexity — not a CRUD showcase.

CreditFlow models the full lifecycle of a loan application: submission, automated credit underwriting (debt-to-income evaluation, credit-score risk tiering, tier-based interest rates and lending limits), approval, and disbursement. Every underwriting decision — approval or rejection — carries an explicit, human-readable set of reasons, mirroring a real regulatory/trust expectation in lending systems.

A companion frontend client, [`CreditFlow.App`](../CreditFlow.App), consumes this API as the working console for bank branch loan officers.

---

## Table of contents

- [Architecture](#architecture)
- [Domain model](#domain-model)
- [Underwriting logic](#underwriting-logic)
- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
  - [Running with Docker Compose](#running-with-docker-compose)
  - [Running locally (without Docker)](#running-locally-without-docker)
- [Configuration](#configuration)
- [API overview](#api-overview)
- [Known limitations & deliberate simplifications](#known-limitations--deliberate-simplifications)

---

## Architecture

CreditFlow follows **Clean Architecture**, with a strict dependency rule: dependencies only point inward, toward the Domain.

```
CreditFlow.Domain          → pure business logic, zero external dependencies
       ↑
CreditFlow.Application      → use cases (CQRS: Commands & Queries), depends only on Domain
       ↑
CreditFlow.Infrastructure   → EF Core, external services — implements Application's interfaces
CreditFlow.Api              → controllers, middleware, composition root
```

- **Domain** never references EF Core, ASP.NET Core, or any framework — it's plain C#. Its only external dependency is MediatR's `INotification` marker interface, used for domain events.
- **Application** defines everything Infrastructure must provide (`ILoanApplicationRepository`, `ICreditBureauService`, `IUnitOfWork`, etc.) as interfaces, and implements all business workflows as CQRS commands/queries via MediatR.
- **Infrastructure** implements those interfaces with EF Core + PostgreSQL, and hosts the (currently simulated) credit bureau integration.
- **Api** wires everything together, exposes REST endpoints, and translates domain/application failures into HTTP responses.

### Why CQRS

Every write operation (submit an application, run underwriting, approve, reject, disburse, cancel) is a small, single-purpose **Command** with its own handler and validator. Every read operation (`GetLoanApplicationById`, `GetLoanApplications`) is a separate **Query**. Read and write paths are fully decoupled: `GetLoanApplicationsQuery` doesn't load the `LoanApplication` aggregate at all — it projects directly to a DTO shape via EF Core's `.Select()`, translated straight to SQL, so a paginated list endpoint never pays the cost of materializing full aggregates just to discard most of their data.

### Cross-cutting concerns

Every command/query passes through a MediatR pipeline:

```
Request → UnhandledExceptionBehavior → LoggingBehavior → ValidationBehavior → Handler
```

- **ValidationBehavior** runs FluentValidation rules before a handler ever executes — syntactic validation (required fields, ranges, formats) is centralized here, separate from business rules, which live in the Domain.
- **LoggingBehavior** logs the start/duration of every request by name only (never full payloads, to avoid leaking sensitive applicant data into logs).
- **UnhandledExceptionBehavior** logs genuinely unexpected failures, while letting expected outcomes (validation errors, not-found, domain rule violations) pass through without being misclassified as system errors.

### Error handling

A single `ExceptionHandlingMiddleware` maps every exception type to an HTTP status and a consistent JSON error shape:

| Exception | HTTP Status | Meaning |
|---|---|---|
| `RequestValidationException` | 400 | Input failed FluentValidation rules |
| `NotFoundException` | 404 | Referenced entity doesn't exist |
| `DomainException` (and subtypes) | 409 | Request conflicts with a business rule or current state |
| Anything else | 500 | Genuinely unexpected failure |

---

## Domain model

### Aggregates & entities

- **`LoanApplication`** (aggregate root) — owns and enforces its own state machine. No external code can set its status directly; every transition goes through a named method (`Submit()`, `StartReview()`, `Approve()`, `Reject()`, `Disburse()`, `Cancel()`) that validates the current state first.
- **`Applicant`** (entity) — holds identifying, employment, and financial-obligation data. Has a stable identity even as employment or debt changes over time.

### Value objects

| Value object | Purpose |
|---|---|
| `Money` | Amount + currency; prevents cross-currency arithmetic errors at the type level |
| `Percentage` | A 0–100 bounded ratio, used for DTI and rate components |
| `InterestRate` | Annual rate, wraps `Percentage` with lending-specific behavior |
| `LoanTerms` | Principal, rate, term, and a self-computed monthly payment (amortization formula) |
| `PersonalInfo` | Name, date of birth, national ID (always displayed masked, never logged in full) |
| `EmploymentInfo` | Employer, income, tenure |
| `FinancialObligations` | Existing debt, with a `LastCheckedAtUtc` timestamp tracking staleness against the credit bureau |
| `CreditAssessment` | An immutable, point-in-time snapshot of an underwriting outcome — score, risk tier, DTI, reasons |

### State machine

```
Draft → Submitted → UnderReview → Approved → Disbursed
                          │
                          └──────→ Rejected

Draft / Submitted → Cancelled
```

Every transition is enforced inside `LoanApplication` itself. An invalid transition (e.g. disbursing a loan that isn't `Approved`) throws `InvalidLoanStateTransitionException`, mapped to `409 Conflict`.

### Domain events

Raised by the aggregate and dispatched after a successful `SaveChangesAsync`: `LoanApplicationSubmitted`, `CreditAssessmentCompleted`, `LoanApplicationApproved`, `LoanApplicationRejected`, `LoanDisbursed`.

---

## Underwriting logic

`UnderwritingPolicy` (a domain service) evaluates every loan request against three configurable thresholds:

1. **Credit score risk tiering** — score is bucketed into `Prime`, `NearPrime`, `SubPrime`, or `Declined`. Below the minimum acceptable score, the request is auto-declined before any further calculation runs.
2. **Tier-based lending limits & interest rate** — each risk tier has its own maximum loan-to-income multiplier and annual interest rate.
3. **Debt-to-income (DTI) ratio** — `(existing monthly debt + proposed new payment) ÷ monthly income`. Above the configured maximum, the request is declined even if the credit score tier would otherwise qualify.

Every decision — approved or declined — returns a full list of human-readable reasons, not just a boolean.

**All thresholds are externalized in configuration** (`UnderwritingPolicy` section in `appsettings.json`), not hardcoded — risk parameters can be tuned without a code change or rebuild:

```jsonc
"UnderwritingPolicy": {
  "MinAcceptableCreditScore": 580,
  "NearPrimeMinScore": 650,
  "PrimeMinScore": 750,
  "MaxAllowedDtiPercentage": 45,
  "Prime": { "AnnualInterestRate": 5.5, "IncomeMultiplier": 10 },
  "NearPrime": { "AnnualInterestRate": 8.9, "IncomeMultiplier": 6 },
  "SubPrime": { "AnnualInterestRate": 14.9, "IncomeMultiplier": 3 }
}
```

---

## Tech stack

- **.NET 10** / ASP.NET Core Web API
- **EF Core** + **PostgreSQL** (Npgsql provider)
- **MediatR** — CQRS command/query dispatch and pipeline behaviors
- **FluentValidation** — request validation
- **Docker** & **Docker Compose** — containerized API + database, with health-checked startup ordering
- **Scalar** — interactive OpenAPI documentation (Development environment only)

---

## Project structure

```
CreditFlow/
├── CreditFlow.Domain/            Pure business logic — entities, value objects, domain services, events
├── CreditFlow.Application/       CQRS commands/queries, validators, pipeline behaviors, interfaces
├── CreditFlow.Infrastructure/    EF Core DbContext, repositories, migrations, external service implementations
├── CreditFlow.Api/               Controllers, middleware, Program.cs, appsettings
├── docker-compose.yml
├── .env.example
└── CreditFlow.slnx
```

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- (Optional, for local EF CLI commands) `dotnet tool install --global dotnet-ef`

### Running with Docker Compose

This is the fastest way to run the full system (API + PostgreSQL) exactly as it would run in a deployed environment.

1. Copy the environment template and fill in local values:

   ```bash
   cp .env.example .env
   ```

2. From the repository root:

   ```bash
   docker compose up --build
   ```

3. The API is available at `http://localhost:8080`. In `Development` mode, interactive API docs are at:

   ```
   http://localhost:8080/scalar/v1
   ```

4. Database migrations are applied automatically on startup (`Database.Migrate()` in `Program.cs`) — no manual migration step is required for a fresh database.

5. To stop everything:

   ```bash
   docker compose down
   ```

   Add `-v` to also delete the database volume (full reset).

### Running locally (without Docker)

Useful for debugging directly in Visual Studio / your IDE of choice.

1. Start only the database:

   ```bash
   docker compose up db -d
   ```

2. Set the connection string via .NET User Secrets (never committed to source control):

   ```bash
   cd CreditFlow.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=CreditFlow;Username=<your-user>;Password=<your-password>"
   ```

   (Values must match whatever you set in your `.env` file for the `db` service.)

3. Run the API from your IDE, or:

   ```bash
   dotnet run --project CreditFlow.Api
   ```

4. Applying/creating migrations locally, from the repository root:

   ```bash
   dotnet ef migrations add <MigrationName> --project CreditFlow.Infrastructure --startup-project CreditFlow.Api
   ```

---

## Configuration

Configuration follows the standard ASP.NET Core layering (`appsettings.json` → `appsettings.{Environment}.json` → User Secrets → environment variables), with a deliberate separation between what's safe to commit and what isn't:

| Source | Contains | Committed to Git? |
|---|---|---|
| `appsettings.json` | Structure, non-secret defaults (underwriting thresholds) | Yes |
| `appsettings.Development.json` / `appsettings.Production.json` | Environment-specific logging levels | Yes |
| .NET User Secrets | Local connection string | No (lives outside the repo) |
| `.env` (Docker Compose) | Local Postgres credentials | No (`.env.example` is committed instead) |
| Container/orchestrator environment variables | Production connection string, CORS origins | No |

No credential or connection string is ever hardcoded or committed in plain text.

---

## API overview

Base path: `/api/v1`

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/applicants` | Register a new applicant |
| `POST` | `/loan-applications` | Submit a new loan application |
| `GET` | `/loan-applications/{id}` | Full detail for one application |
| `GET` | `/loan-applications` | Paginated, filterable list (status, applicant, date range) |
| `POST` | `/loan-applications/{id}/underwrite` | Run automated underwriting |
| `POST` | `/loan-applications/{id}/approve` | Manual approval / override |
| `POST` | `/loan-applications/{id}/reject` | Manual rejection |
| `POST` | `/loan-applications/{id}/disburse` | Mark an approved loan as disbursed |
| `POST` | `/loan-applications/{id}/cancel` | Cancel a draft/submitted application |

Full request/response schemas are available via Scalar (`/scalar/v1`) when running in `Development`.

---

## Known limitations & deliberate simplifications

This is a portfolio project, and a few tradeoffs were made consciously rather than left as oversights:

- **Credit bureau integration is simulated.** `SimulatedCreditBureauService` generates a deterministic (per-applicant) score and debt figure locally, rather than calling a real (paid) third-party bureau API. It's marked as temporary in code, with a real HTTP-based implementation planned as a follow-up.
- **Migrations run automatically on API startup.** Convenient for local development and demos; a real production deployment would more likely run migrations as an explicit, separate release step.
- **No authentication/authorization layer yet.** All endpoints are currently open — this would be a required addition before any real deployment.
- **List query performance is not fully benchmarked at scale.** Filtering/sorting works correctly via EF Core projections, but hasn't been load-tested against large datasets.

---

## License

This project is available for review as part of a professional portfolio. Contact the author for reuse terms.
