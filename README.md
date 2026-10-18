# FORGOTTEN NFT Engine

A universal **Solana NFT verification and collection engine** for Discord, built
on Discord.Net. FORGOTTEN lets Discord members inspect Solana NFTs, cryptographically
prove wallet ownership, browse their collections, and expose verified ownership to
external games — without hardcoding any single NFT collection.

> **Status:** Phase 0 (bootstrap) complete. The solution builds, the host wires up
> cleanly, and the Discord/SQLite/config foundation is in place. NFT features land
> in Phases 1–6 — see the [Roadmap](#roadmap).

The engine is **collection-agnostic**: it validates and displays NFTs from any
compatible Solana collection, while game-specific logic (e.g. Dead Mint Carnival /
POW fighting) consumes it through clean integration interfaces.

---

## Tech stack

| Concern | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0 LTS |
| Discord | Discord.Net | 3.20.2 |
| Persistence | EF Core + SQLite | 10.0.13 |
| Blockchain | Solana via DAS-compatible RPC (Metaplex DAS) | — |
| Auth | Solana Ed25519 signatures | — |
| HTTP resilience | Microsoft.Extensions.Http.Resilience | 10.11.0 |
| Host / DI / Options / Logging | Microsoft.Extensions.* | 10.0.x |
| Tests | xUnit | 2.9.3 |

## Solution layout

```
Forgotten.slnx
├── src/
│   ├── Forgotten.Bot                 # Discord presentation + Generic Host (composition root)
│   ├── Forgotten.Nft.Domain          # Entities, enums, value objects, events (no external deps)
│   ├── Forgotten.Nft.Application      # Services, interfaces, DTOs, options + validation
│   ├── Forgotten.Nft.Infrastructure   # EF Core/SQLite, blockchain, metadata, caching, workers
│   ├── Forgotten.Nft.WalletWeb        # HTTPS wallet-verification site (Phase 3)
│   └── Forgotten.Nft.Integrations     # Discord roles, POW adapter (Phases 5–6)
└── tests/
    ├── Forgotten.Nft.UnitTests
    ├── Forgotten.Nft.IntegrationTests
    └── Forgotten.Nft.SecurityTests
```

Dependency direction is enforced (`Domain ← Application ← Infrastructure`, with
`Bot` as the only composition root). A unit test asserts the Domain layer never
references Discord.Net or EF Core. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.100+)
- A Discord bot application + token ([Discord Developer Portal](https://discord.com/developers/applications))
- *(Later phases)* a DAS-compatible Solana RPC endpoint (e.g. a Metaplex DAS provider)

### Build & test

```bash
git clone https://github.com/BMFer/Forgotten.git
cd Forgotten
dotnet build Forgotten.slnx
dotnet test Forgotten.slnx
```

### Configure secrets

Secrets never live in committed files. Supply the Discord token (and, later,
provider API keys) via user secrets or environment variables:

```bash
# user secrets (recommended for local dev)
dotnet user-secrets set "Discord:Token" "<your-bot-token>" --project src/Forgotten.Bot

# optional: fast, guild-scoped slash-command registration during development
dotnet user-secrets set "Discord:DevGuildId" "<your-guild-id>" --project src/Forgotten.Bot
```

Environment variables use the `Section__Key` form, e.g. `Discord__Token`.
Copy non-secret settings from [`appsettings.Example.json`](src/Forgotten.Bot/appsettings.Example.json).

### Run the bot

```bash
dotnet run --project src/Forgotten.Bot
# expect a startup banner, then: "FORGOTTEN ready as <bot>"
```

The host **fails closed**: if the Discord token (or any security-critical setting)
is missing or invalid, startup is refused with a clear validation error rather than
continuing in a degraded state.

## Configuration overview

| Section | Purpose | Notes |
|---|---|---|
| `Discord` | Bot token, optional dev guild id | Token is a **secret** |
| `Database` | SQLite connection string, busy timeout | WAL + foreign keys enforced |
| `Solana` | Network, DAS/RPC endpoints, timeouts, paging, cache TTL | API keys via secrets only |
| `NftEngine` | Feature flags (monitoring, gallery, POW, limits) | Validated at startup |

All options are strongly typed and validated on startup (DataAnnotations +
cross-field validators). Full details land in `docs/CONFIGURATION.md` as phases
complete.

## Database

SQLite via EF Core, evolved **only** through migrations — the database is never
auto-dropped or recreated. The initial (empty) migration is `InitialCreate`;
Phase 1 onwards add the tables from the spec.

```bash
dotnet ef database update \
  --project src/Forgotten.Nft.Infrastructure \
  --startup-project src/Forgotten.Nft.Infrastructure
```

## Roadmap

Each phase is a self-contained, tested vertical slice. Full guides live in
[`docs/phases/`](docs/phases/README.md).

| Phase | Deliverable | Status |
|------|-------------|--------|
| **0 — Bootstrap** | Solution, host, DI, config, Discord + SQLite foundation | ✅ Complete |
| **1 — Foundation** | `/nft inspect` against a DAS provider | ⬜ Planned |
| **2 — Verification** | `/nft verify` + collection approval registry | ⬜ Planned |
| **3 — Wallet** | Cryptographic Ed25519 wallet ownership proof | ⬜ Planned |
| **4 — Gallery** | Interactive, paginated `/nft gallery` | ⬜ Planned |
| **5 — Monitoring** | Background ownership tracking + role sync | ⬜ Planned |
| **6 — Game integration** | Ownership/eligibility interfaces for POW | ⬜ Planned |

## Security principles

- Wallet ownership is **proven cryptographically** (Ed25519) — never by submitting an address.
- Private keys / seed phrases are never requested or stored.
- NFT metadata is untrusted input: sanitized, size/time-bounded, SSRF-protected.
- Blockchain providers are external dependencies; a failed request **never** grants access.
- Security-critical configuration **fails closed** at startup.

See the spec's full security requirements and (as phases land) `docs/SECURITY.md`.

## Documentation

- [Technical specification](docs/specs.md) — the complete requirement set.
- [Architecture](docs/ARCHITECTURE.md) — layering, host, persistence baseline.
- [Phase guides](docs/phases/README.md) — the phased build plan.

## License

Not yet specified.
