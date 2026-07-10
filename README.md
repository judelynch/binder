# PokéBinder

A multi-user Pokémon TCG binder collection website. See `CLAUDE.md` for the full
architecture and phase plan.

## Phase 0 status

Solution scaffold, EF Core + SQL Server, ASP.NET Core Identity with roles, JWT auth,
and a React shell that can register/log in. No card or binder features yet.

## Prerequisites

- .NET 8 SDK (or newer SDK with the net8.0 targeting pack — this repo targets `net8.0`)
- Node.js 18+ and npm
- A local SQL Server instance. This repo's dev connection string points at the
  named instance `localhost\SQLEXPRESS` (see `src/PokeBinder.Api/appsettings.Development.json`).
  Update `ConnectionStrings:Default` if your instance is named differently.
- The `dotnet-ef` global tool (`dotnet tool install --global dotnet-ef`) if you need
  to add/apply migrations yourself.
- Trust the local HTTPS dev certificate once per machine, so the React app's
  `https://localhost:7253` API calls succeed:
  ```
  dotnet dev-certs https --trust
  ```

## First-time setup

1. Restore and apply the database schema:
   ```
   dotnet ef database update --project src/PokeBinder.Infrastructure --startup-project src/PokeBinder.Api
   ```
   This creates the `PokeBinder` database (Identity tables) on your local SQL Server
   instance and seeds the `Admin`/`User` roles plus one admin account on next API
   startup, using the `Admin:Email` / `Admin:Password` values in
   `src/PokeBinder.Api/appsettings.Development.json`. **Change those values** before
   relying on them for anything beyond local dev.
2. Install frontend dependencies:
   ```
   cd src/PokeBinder.Web
   npm install
   ```

## Running in dev (two terminals)

**Terminal 1 — API** (from repo root):
```
dotnet run --project src/PokeBinder.Api
```
Starts on `https://localhost:7253` (and `http://localhost:5279`). Swagger UI is at
`/swagger`. On startup (Development environment only) it seeds the `Admin`/`User`
roles and the configured admin account if they don't already exist.

**Terminal 2 — Frontend** (from repo root):
```
cd src/PokeBinder.Web
npm run dev
```
Starts on `http://localhost:5173`. Its API base URL is set in
`src/PokeBinder.Web/.env.development` (`VITE_API_BASE_URL`) and must match the
API's HTTPS URL above.

Open `http://localhost:5173`, register a new account, or log in with the seeded
admin credentials to see the Admin sidebar link.

## Tests

```
dotnet test
```

The integration smoke test (`tests/PokeBinder.Tests/AuthFlowTests.cs`) exercises
register → login → `/api/auth/me` against a real SQL Server database named
`PokeBinderTest` on the same local instance. It drops and recreates that database
on every run, so no manual setup is needed beyond having the SQL Server instance
reachable — just don't point `PokeBinderTest` at anything you care about.

## Project layout

See `CLAUDE.md` for the full solution layout and domain model. In short:

- `src/PokeBinder.Api` — ASP.NET Core Web API (controllers, auth, DI)
- `src/PokeBinder.Core` — domain entities, interfaces (no EF)
- `src/PokeBinder.Infrastructure` — EF Core DbContext, migrations, seeding
- `src/PokeBinder.Web` — React + TypeScript + Vite frontend
- `tests/PokeBinder.Tests` — xUnit tests
