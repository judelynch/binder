# PokéBinder

A multi-user Pokémon TCG binder collection website. See `CLAUDE.md` for the full
architecture and phase plan.

## Status

- **Phase 0**: Solution scaffold, EF Core + SQL Server, ASP.NET Core Identity with
  roles, JWT auth, and a React shell that can register/log in.
- **Phase 1**: Full card reference schema (Set/Card/VariantType/CardVariant) seeded
  from the [PokemonTCG/pokemon-tcg-data](https://github.com/PokemonTCG/pokemon-tcg-data)
  dataset — see "Card data" below. Read-only `/api/sets` and `/api/cards` endpoints.
- **Phase 2**: Binder backend — CRUD, pages/spreads, slots, bulk-assign, overlay
  tags, and a dashboard endpoint. Binder page counts are always even (pages exist
  in physical sheet-pairs, matching a real binder leaf); `DELETE .../pages/{n}`
  removes the whole sheet a page belongs to, not a single page.

No frontend for binders/cards yet (later phase).

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

## Card data (Phase 1)

The card reference schema (Set, Card, VariantType, CardVariant) is seeded from the
[PokemonTCG/pokemon-tcg-data](https://github.com/PokemonTCG/pokemon-tcg-data) GitHub
repo's `sets/en.json` and `cards/en/*.json`. The importer is idempotent — safe to
re-run any time; re-running with no upstream changes adds/updates nothing.

**Run it from the command line** (downloads the repo as a tarball by default):
```
dotnet run --project src/PokeBinder.Api -- seed
```
This can take about a minute (173 sets, ~20,300 cards) and prints a summary of
sets/cards added vs. updated. To import from a local clone instead of downloading,
set `CardData:LocalPath` (e.g. via the `CardData__LocalPath` environment variable)
to the root of a `pokemon-tcg-data` checkout.

**Or trigger it via the API** as an admin: `POST /api/admin/sync` (requires an
Admin-role bearer token), which runs the same importer and returns the summary as
JSON. There's no admin UI for this yet — that's Phase 6.

Card numbers are sorted using a scheme validated against the full real dataset
(`NumberSortKeyCalculator` in `PokeBinder.Core`) — plain numerics first ("1", "2",
"28", "28a"), then letter-prefixed groups like "RC1"/"TG12" in their own numeric
order, then pure-letter cards ("A".."Z"), then anything else, as a last-resort
fallback. See `GET /api/sets/{id}/cards` for the sorted, paginated result.

## Tests

```
dotnet test
```

Integration tests run against real SQL Server databases on the same local instance
(`PokeBinderTest`, `PokeBinderTest_CardImport`, `PokeBinderTest_Ordering`,
`PokeBinderTest_Binders`), each dropped and recreated on every run — no manual
setup needed beyond having the SQL Server instance reachable, and don't point any
`PokeBinderTest*` database at anything you care about. Card-data tests import a
small fixture dataset from `tests/PokeBinder.Tests/Fixtures/CardData` rather than
the real GitHub repo; binder tests seed a few minimal Card/CardVariant rows
directly (`CardFixture.cs`) rather than running the real importer.

## Project layout

See `CLAUDE.md` for the full solution layout and domain model. In short:

- `src/PokeBinder.Api` — ASP.NET Core Web API (controllers, auth, DI)
- `src/PokeBinder.Core` — domain entities, interfaces (no EF)
- `src/PokeBinder.Infrastructure` — EF Core DbContext, migrations, seeding
- `src/PokeBinder.Web` — React + TypeScript + Vite frontend
- `tests/PokeBinder.Tests` — xUnit tests
