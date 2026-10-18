# Phase 4 — NFT Gallery

**Spec refs:** §6.2, §12, §9 (`NftGallerySessions`), §15, §19, §22.4
**Status:** not started
**Deliverable (spec §22):** Members can browse their owned NFTs inside Discord
through an interactive, paginated, session-protected gallery.

---

## 1. Objective

Compose Phase 1 inspection + Phase 3 verified wallets into an interactive gallery
UI. The hard requirements are **bounded memory** (never load a whole wallet) and
**session ownership** (no one else drives your gallery).

## 2. Scope

**In scope**

- Owned-NFT retrieval across a member's linked wallets (`GetAssetsByOwnerAsync`).
- Paginated, interactive gallery components (spec §12).
- Gallery session state + expiry (`NftGallerySessions`).
- Collection filtering; artwork display; loading/error states.
- Gallery caching (spec §15).
- `/nft gallery` (spec §6.2).

**Out of scope**

- Background ownership monitoring + roles (Phase 5).
- Game eligibility (Phase 6).

## 3. Prerequisites

- [ ] Phase 3 Exit Gate passed (verified wallets exist to enumerate).
- [ ] `ISolanaAssetProvider.GetAssetsByOwnerAsync` implemented (DAS `getAssetsByOwner`).

## 4. Tasks

### 4.1 Retrieval (`Forgotten.Nft.Application`)
- [ ] `INftGalleryService`: fetch NFTs for the member's linked wallets, merged,
      with collection names resolved.
- [ ] Support multiple linked wallets (spec §6.2).
- [ ] ⚠ Require a **connected (verified) wallet**; otherwise `WALLET_NOT_CONNECTED`
      (spec §6.2, config `RequireVerifiedWalletForGallery` §20).
- [ ] ⚠ Page via the provider — **never** download the entire inventory into
      memory (spec §6.2, §19, §5.3 bounded pagination).

### 4.2 Session management
- [ ] `NftGallerySessions` entity + table (spec §9): DiscordUserId, GuildId,
      WalletAddress, CollectionFilter, CurrentPage, ExpiresAt, CreatedAt.
- [ ] Create a session on `/nft gallery`; store current page + filter.
- [ ] ⚠ Expire inactive sessions (config `GallerySessionMinutes` §20).
- [ ] Avoid unnecessary DB writes during navigation (spec §9.2) — prefer in-memory
      session state with periodic/minimal persistence.

### 4.3 Interactive UI (`Forgotten.Bot/Components/NftGalleryComponents.cs`)
- [ ] Render the §12 layout: one NFT per page by default (`DefaultGalleryPageSize`
      §20), artwork, name, owner, rarity, "NFT X OF Y".
- [ ] Buttons: Previous, Next, Inspect, Collection, Refresh (spec §6.2, §12).
- [ ] ⚠ Every button interaction validates the requesting member == session owner;
      reject others (spec §6.2, §12, §16.18).
- [ ] Loading + error states; handle deleted messages; handle missing artwork
      (placeholder); animated media degrades gracefully (spec §12, §8.3).
- [ ] Collection filtering control (spec §6.2, §12).
- [ ] ⚠ Ephemeral for private browsing; public showcase requires explicit member
      action; never leak private wallet info via public commands (spec §12, §16.13).
- [ ] **Defer** the interaction before provider calls (spec §19).

### 4.4 Caching
- [ ] Gallery results TTL 60 s; ownership *display* 30 s (spec §15).
- [ ] ⚠ Cache is for display only — it must never authorize a sensitive op
      (spec §15, §16.20).

## 5. Deliverables

- `/nft gallery` browsing owned NFTs with working pagination + filtering.
- Session-protected interactive components.

## 6. Tests required (spec §18 "Gallery")

- [ ] Empty wallet → friendly empty state.
- [ ] Single NFT → no broken pagination.
- [ ] Multiple NFTs → correct paging, correct "X OF Y".
- [ ] Pagination does not materialize the full inventory (bounded fetch asserted).
- [ ] Missing artwork → placeholder, no crash.
- [ ] Provider timeout → error state, not a false "empty wallet".
- [ ] ⚠ Unauthorized button interaction (another user) → rejected.
- [ ] Expired session → `GALLERY_SESSION_EXPIRED`, controls disabled.
- [ ] No verified wallet → `WALLET_NOT_CONNECTED`.

## 7. Exit Gate — all must be checked to start Phase 5

- [ ] Members browse owned NFTs across linked wallets with working buttons.
- [ ] Only the session owner can operate a gallery; others are rejected.
- [ ] Pagination is provably bounded (no full-wallet load).
- [ ] Sessions expire; deleted messages + missing artwork handled.
- [ ] `NftGallerySessions` migration additive and applied.
- [ ] Cached data never used to authorize anything sensitive.
- [ ] Build clean; Phase 4 tests green; `DISCORD_COMMANDS.md` updated.
