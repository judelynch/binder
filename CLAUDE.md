# Project: PokéBinder

A multi-user Pokémon TCG binder collection website. Users create virtual binders
that render like real physical binders, fill slots with cards from the official
card database, track ownership, and get smart suggestions. Designed to evolve
into a fuller Pokémon TCG website later — keep the architecture open to that.

## Tech stack (fixed — do not substitute)
- Backend: C# / .NET 8, ASP.NET Core Web API
- ORM: EF Core with SQL Server provider (Microsoft.EntityFrameworkCore.SqlServer)
- Database: SQL Server, local instance (connection string in appsettings.Development.json,
  Trusted_Connection=True, TrustServerCertificate=True). Managed via SSMS by the owner.
- Auth: ASP.NET Core Identity with roles: "Admin", "User". JWT bearer tokens for the API.
- Frontend: React 19 + TypeScript + Vite. React Router. TanStack Query for server state.
- Styling: Tailwind CSS (v4, CSS-first `@theme` tokens in index.css). Fully responsive
  (mobile through desktop). Single dark theme by deliberate design (no light mode) —
  see src/PokeBinder.Web/src/index.css for the current tokens ("Case Display": a
  charcoal ground, shades of #2B2D2F, with a brass accent).
- Card data source: https://github.com/PokemonTCG/pokemon-tcg-data (JSON files:
  /sets/en.json for sets, /cards/en/<setId>.json for cards).
- Card images: HOTLINKED from the URLs in the card JSON (images.pokemontcg.io).
  Do not download or mirror images. Always lazy-load, always show a placeholder
  while loading and a graceful fallback on 404.

## Solution layout
/PokeBinder.sln
/src/PokeBinder.Api          — ASP.NET Core Web API (controllers, auth, DI)
/src/PokeBinder.Core         — domain entities, interfaces, business logic (no EF)
/src/PokeBinder.Infrastructure — EF Core DbContext, migrations, repositories, seed/sync services
/src/PokeBinder.Web          — React + TS + Vite frontend
/tests/PokeBinder.Tests      — xUnit tests for Core + Infrastructure + API integration tests

## Domain model — core concepts (see Phase 1 for full schema)
- Card: seeded from the GitHub dataset. Immutable-ish reference data. Key fields:
  external id (e.g. "base1-4"), name, supertype, subtypes, hp, types, evolvesFrom,
  rarity, artist, nationalPokedexNumbers, set id, number (string! e.g. "4", "TG12"),
  numberSortKey (computed int for ordering), regulationMark, image urls,
  weaknesses/resistances/retreatCost/attacks stored as JSON columns.
- Set: id, name, series, printedTotal, total, releaseDate, symbol/logo urls.
- VariantType: lookup managed by Admin (seed with: Normal, Reverse Holo, Holo,
  1st Edition, Shadowless, Promo Stamp). Extensible.
- CardVariant: (CardId, VariantTypeId) — which variants exist for a card.
  Every card gets a "Normal" variant on import automatically.
- Binder: owner (userId), name, colour (hex), rows, columns, createdAt.
  Slot layouts supported: 2x2, 3x3, 3x4 (rows x columns), but store rows/cols
  generically. Pages can be added/removed after creation.
- BinderPage: binder id, pageNumber (1-based). Each page = ONE SIDE of a sheet
  holding rows*columns slots.
- BinderSlot: page id, position (0-based, left-to-right top-to-bottom),
  nullable CardVariantId, owned (bool), nullable quantity (int), nullable condition
  (enum: NM, LP, MP, HP, DMG), nullable OverlayTagId.
- OverlayTag: per-user: name (e.g. "Ordered"), colour (hex). Applied to slots,
  toggleable in the binder view.
- CardOwnership: per-user, per-CardVariant fact ("I own this card variant"),
  independent of BinderSlot — a card can be owned in your collection before
  (or without) ever being placed in a binder. Fields: userId, cardVariantId,
  quantity (int, always >=1 — unmarking deletes the row rather than zeroing
  it), nullable condition (same enum as BinderSlot). Deliberately never
  synced with BinderSlot.Owned; the two ownership concepts are separate by
  design, not an oversight.

## Binder rendering rules (IMPORTANT — this is the heart of the app)
- The binder view always shows a two-page spread, like a real open binder.
- Spread 1: LEFT = inside front cover (rendered as a plain binder-coloured panel,
  no slots), RIGHT = page 1.
- Spread 2: LEFT = page 2, RIGHT = page 3. Spread 3: pages 4-5. And so on.
- Final spread: LEFT = last page, RIGHT = inside back cover (plain panel).
  (If the page count is even, the last spread is: last page LEFT, back cover RIGHT.
  If odd, the spread before shows the final two pages and the last spread is
  final page LEFT + back cover RIGHT — i.e. standard book pagination where
  page 1 always sits alone on the right.)
- Navigation: previous/next buttons + keyboard arrows. Instant swap, NO page-flip
  animation.
- Cards the user owns render full colour. Cards assigned but not owned render
  greyscale (CSS filter). A global toggle switches the greyscale treatment on/off.
- Overlay tags render as a translucent colour wash + small label chip on the slot,
  with a legend, and a global toggle to show/hide overlays.
- Moving a card: drag-and-drop within the loaded spread swaps two slots directly
  (POST /slots/{id}/move). Moving further than the two pages on screen is done by
  dragging toward the left/right edge of the viewport and holding — a visible
  arrow panel lights up there (BinderFrame's EdgeTurnPanel) and the spread keeps
  turning one page per ~650ms for as long as the pointer stays in that zone, until
  released, dropped, or the binder's start/end is hit. This works because dnd-kit's
  DragOverlay keeps the dragged card's visual independent of the underlying slot,
  which is free to unmount/remount as pages turn.
- Select mode + drag: selecting several cards then dragging any ONE of the
  selected slots moves the WHOLE selection together (POST /slots/bulk-move) —
  the earliest-positioned selected card lands exactly on the drop point, the
  rest follow in their original relative order. Dragging a card that isn't part
  of the current selection (even with select mode on) just moves that one card
  normally. Every placement is a full swap — nothing on the destination pages is
  ever lost, displaced cards swap back into the vacated source slots — and the
  binder auto-adds pages if the selection runs past the last existing slot.

## Set & card browsing, collection tracking
- /sets: grid of every set (logo, series, card count, completion %),
  filtered/sorted client-side — the catalog is small enough that a server
  round-trip for this is unnecessary.
- /sets/:setId: every card in the set, ordered by numberSortKey, one tile
  per (card, variant) — same visual/interaction pattern as card search
  (search/ResultsGrid + CardResultTile). Tiles are select-then-bulk-act
  (see the bulk-endpoint rule below), not click-to-toggle-immediately.
- /cards/:cardId: full card reference stats (attacks, abilities,
  weaknesses, resistances, retreat cost, etc. — previously imported but
  never exposed via the API) plus the current user's own
  ownership/quantity/condition per variant. Two tabs: Overview (the stats
  above) and Market price (per-variant price summary + trend chart + full
  newest-first sale history — see Pricing module below). Reached either by
  navigating directly or by double-clicking a card tile on the set page or
  the advanced search page.
- Set page and advanced search tiles show a best-available price badge when
  price data exists (OwnershipVariantTile / CardResultTile), and support a
  double-click to open the card detail page (single click stays a plain
  selection toggle for the existing bulk-act flows).
- Set completion rule: a card counts as "complete" once the user owns
  every variant of it EXCEPT variants whose VariantType.Name contains
  "Stamp" (e.g. a "Promo Stamp" variant doesn't block completion). This
  rule is implemented twice — SQL/LINQ on the backend
  (SetsController.GetSets) and TypeScript on the frontend
  (lib/setCompletion.ts) — keep both in sync if it ever changes, and
  always match case-insensitively (don't rely on the database's default
  collation for that).

## Pricing module
- Goal: a confidence-aware eBay UK sold-price pipeline, built end-to-end
  against a fixture-fed IPriceSourceProvider (MockPriceSourceProvider) —
  deliberately NOT a live eBay scraper (that would violate eBay's ToS).
  A real provider drops in later behind the same interface with zero
  pipeline changes.
- Pipeline: PricingScrapeOrchestrator (Hangfire job body) pulls listings
  per card-variant → IListingClassifier scores each one (identity/variant/
  grading/condition/language match) into AutoAccepted / Quarantined /
  Rejected → IPriceAggregator buckets AutoAccepted listings by
  (GradedStatus[/Grader/Grade or Condition] × 30/60/90-day window) and
  takes a robust (outlier-trimmed) median → PricePoint rows.
  "Best-available price" = the cheapest published raw (non-graded) bucket
  for a variant — this single rule is reused everywhere a price is shown
  (binder cost-to-buy badges, binder/dashboard totals, set/search tile
  badges, card detail's headline price).
- Scope is global (every CardVariantId referenced by any BinderSlot across
  all users), not per-user — price is a shared fact about a physical card.
- Scheduling: Hangfire + SQL Server storage. Nightly recurring job, a
  login-triggered catch-up (AuthController, admin logins only, only if
  nothing completed in the last 24h), and manual "Run now" / "Scrape this
  card now" from the admin Pricing runs tab. Dashboard at /hangfire is
  restricted to localhost requests only (this API is JWT-only, no cookie
  auth for Hangfire's own dashboard auth to check).
- Quarantined listings go to an admin review queue (Admin → Pricing queue):
  approve as-is, reclassify (correct the grading/condition guess), reject,
  or bulk-act across a multi-select. Every action writes a
  ClassificationFeedback audit row and re-triggers aggregation for the
  affected variant immediately.
- Dev-only tuning: appsettings.Development.json has a Pricing:Scrape
  override (near-zero BatchPauseSeconds/RequestDelaySeconds/
  RequestJitterMaxSeconds) so local runs finish in seconds — production
  defaults in PricingScrapeOptions are deliberately paced like a real,
  rate-limit-conscious client even though the mock provider doesn't need
  it, so that timing behaviour doesn't need to change later.

## Smart suggestions
- SuggestionEngine (PokeBinder.Core.Binders) looks at the WHOLE binder
  first to decide what it's "about" — one dominant set, one dominant
  Pokémon name, or one dominant rarity+type / rarity+supertype combo —
  and generates every suggestion from that single story, rather than
  running independent heuristics per placed card (which read as random).
  The dominant category is whichever has the single largest group of
  placed cards sharing that key, no fixed threshold (plurality wins); ties
  break set > name > rarity+type > rarity+supertype (the narrower
  type-matched grouping beats the broader supertype one when they tie,
  which happens whenever every placed card of a shared rarity also shares
  one element type).
- Set theme: suggests the immediate missing neighbour on BOTH sides of
  every placed card in that set (122 and 124 for a placed 123), not just
  the next one.
- The lightbulb affordance only ever renders on EMPTY slots (frontend
  remaps each filled slot's backend-computed suggestion onto the nearest
  following empty slot in the loaded spread — see BinderDetailPage's
  suggestionsBySlot) since that's where "Add" actually places the card.

## Non-negotiable engineering rules
- Any "select many, then act" UI flow (bulk mark owned/unowned, bulk
  assign variants, etc.) gets a dedicated bulk API endpoint, not N
  sequential requests from the frontend. See /api/collection/ownership/bulk
  and /api/binders/{id}/slots/bulk-* for the pattern to follow.
- EF Core migrations for ALL schema changes. Never hand-edit the database.
- All API endpoints require auth except register/login. Users can only access
  their own binders; Admin role gates all /api/admin/* endpoints.
- Card search endpoints must be paginated, indexed, and return in <300ms locally.
- The card "number" field is a STRING in the source data (e.g. "TG12", "SWSH001").
  Never parse it as an int for storage; compute a numeric sort key alongside it.
- Frontend: all server state through TanStack Query; debounce search inputs 250ms;
  virtualise any list that can exceed ~100 items; lazy-load all card images.
- Write tests for: seed importer, binder pagination/spread logic, search filters,
  suggestion engine. Run `dotnet test` and ensure green before declaring a phase done.
- Do not copy visual design, artwork, or branding from pokemon.com — its search
  page is a behavioural reference only. The site's own design must be original.

## Workflow
- Before implementing any phase prompt, interrogate requirements via /grill.
- Work in small commits with clear messages, one feature per commit.
- At the end of each phase, print a checklist of the phase's acceptance criteria
  with pass/fail against what you actually built.