# ATM App

A web ATM for one demo user: deposit, withdraw and transfer between a Checking and a Savings account, see balances and transaction history. It is an MVP and a showcase of clean boundaries, CQRS with vertical slices, validation, TDD and an ATM-like UI (on-screen keypad, simulated processing spinner). There is no authentication: the Login page is a fake "Enter" button.

Seed data (applied only when the database is empty): user `user-1`, Checking $1,500.00, Savings $5,000.00 (USD). The Login page has a **Reset demo data (demo only)** button to restore it.

## Architecture

- One ASP.NET Core minimal API (.NET 10) in `src/Atm.Api` calls three class libraries: `Atm.Accounts`, `Atm.Users`, `Atm.Transactions` (shared pieces in `Atm.Common`).
- Each library uses vertical slices (request, handler, validator, endpoint per use case) with a small hand-written CQRS dispatcher. Slices are `internal`; only a small contract is public. A module never reads another module's tables.
- One SQLite file. Each module has its own EF Core `DbContext`; a unit of work shares one connection and transaction so a transfer (debit, credit, history) commits or rolls back as one. Balances carry a concurrency token.
- FluentValidation for data and business rules, Serilog logging, `/health` endpoint, ProblemDetails error responses.
- Vue 3 + TypeScript + Vite SPA in `src/web`. In production the API serves the built site from `wwwroot`, so there is one deployable and no CORS.

## Run locally

Needs Docker. From the repository root in PowerShell:

```powershell
.\run.ps1
```

`run.ps1` creates `.env` from `.env.example` if it is missing and runs `docker compose up --build`. See the output for the URL.

Without Docker (needs the .NET 10 SDK and Node): run the API with `dotnet run --project src/Atm.Api`, and in another terminal `cd src/web; npm ci; npm run dev` (Vite proxies to the API).

## Run the tests

```powershell
dotnet test AtmApp.sln      # backend
cd src/web; npm ci; npm test  # frontend (Vitest)
```
