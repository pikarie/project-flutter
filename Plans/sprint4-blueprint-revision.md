# Sprint 4 Blueprint — Revision Notes

**Reviewer context:** I've worked with Karianne through Sprints 1–3, reviewed the current codebase (GameManager, TimeManager, GardenGrid, SpawnSystem, Insect, EventBus, CellState, InsectRegistry, HUD, PhotoFocusController, JournalUI, etc.), and have the full GDD. The other chat that produced this blueprint did **not** have the Sprint 1–3 implementation context — it only had the GDD and the sprint4-context document. Below are all conflicts, errors, and corrections.

---

## Critical Conflicts (Must Fix)

### 1. Tile size mismatch: 64px vs 128px

The blueprint uses `CellSize = 64` everywhere — camera bounds, SeedCursor mouse-to-grid math, drawing offsets. **The existing GardenGrid uses `TileSize = 128`.**

**Affected code:**
- Camera bounds table (Section 1): all LimitRight/LimitBottom/Center values are half what they should be
- `SeedCursor._Process()`: `int gx = (int)(mouse.X / 64)` → should be `/ 128` (or better, reference the active zone's TileSize)
- `SeedCursor._Draw()`: circle radius 14f is way too small at 128px tiles — should be ~28-30f
- `DrawWaterTile()` (Section 6): fine internally, just make sure the Rect2 is 128px

**Corrected camera bounds** (TileSize=128, padding=32):

| Zone | Grid | LimitRight | LimitBottom | Center position |
|------|------|------------|-------------|-----------------|
| Starter | 4×4 | 544 | 544 | (256, 256) |
| Meadow | 6×6 | 800 | 800 | (384, 384) |
| Pond | 5×5 | 672 | 672 | (320, 320) |

Additionally, GardenGrid currently centers itself: `Position = -(GridSize * TileSize / 2)`. The camera bounds logic needs to account for this offset or the zones need to share a consistent origin strategy. Recommend: keep the current centering approach per zone, adjust camera bounds accordingly.

### 2. CellState conflict — existing class vs blueprint's CellData

The blueprint introduces **new classes** (`CellData`, `CellState` enum, `ZoneData`) that conflict with the existing `CellState` class:

**Existing CellState.cs:**
```csharp
public class CellState
{
    public enum State { Empty, Tilled, Planted, Watered, Growing, Blooming }
    public State CurrentState { get; set; }
    public string PlantType { get; set; }
    public int GrowthStage { get; set; }
    public bool IsWatered { get; set; }
    public Node2D PlantNode { get; set; }
    public int MaxInsectSlots { get; set; } = 2;
    public bool CanPlant();
    public bool HasAvailableSlot();
    public void OccupySlot(Node2D insect);
    public void VacateSlot(Node2D insect);
}
```

**Blueprint's replacement:**
```csharp
public enum CellState { Empty, Tilled, Planted, Growing, Mature, Harvestable }
public class CellData { ... }
```

**Problems:**
- Renames `Blooming` → `Mature`/`Harvestable` (splitting one state into two — adds complexity, not clear benefit)
- Drops `Watered` state (but the game has a watering mechanic!)
- Drops `PlantNode` reference (needed for plant visual rendering)
- Drops slot tracking methods (`OccupySlot`, `VacateSlot`, `HasAvailableSlot`) that SpawnSystem actively uses
- Replaces the working slot system (`List<Node2D>` of insects) with `List<string> InsectSlots` (strings instead of node refs)
- Introduces a second `CellType` enum that doesn't exist in the current code

**Recommendation:** Don't replace CellState — **extend it**. Add `CellType` as a new field on the existing CellState class for water tile support. The existing plant lifecycle states and insect slot tracking work correctly and SpawnSystem depends on them.

```csharp
// Add to existing CellState.cs:
public enum CellType { Normal, Water }
public CellType Type { get; set; } = CellType.Normal;
```

### 3. EventBus API mismatch

The blueprint uses a **different EventBus API** than what exists:

**Blueprint uses:** `EventBus.OnZoneChanged += e => ...` (field-style event)
**Existing code uses:** `EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged)` / `EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged)`

All event handler registrations in the blueprint need to be rewritten to use the `Subscribe<T>` / `Unsubscribe<T>` pattern. The existing pattern also requires storing the delegate as a field and unsubscribing in `_ExitTree()`.

### 4. Event signature conflicts

Several proposed events conflict with events that **already exist** in Events.cs:

| Event | Existing Signature | Blueprint Signature | Conflict |
|-------|-------------------|---------------------|----------|
| NectarChangedEvent | `(int NewAmount)` | `(int OldAmount, int NewAmount)` | Different arity |
| PlantHarvestedEvent | `(string PlantType, Vector2I GridPos)` | `(string PlantId, int NectarEarned, int X, int Y)` | Different fields, int X/Y vs Vector2I |

**Recommendation:** Keep the existing signatures. If OldAmount is needed for NectarChanged, extend it: `(int OldAmount, int NewAmount)` — but update all existing subscribers (HUD). For PlantHarvested, add NectarEarned as a third field: `(string PlantType, Vector2I GridPos, int NectarEarned)`.

### 5. Scene tree structure doesn't match reality

**Existing tree:**
```
Main (Node2D)
├── Garden (Node2D)
│   ├── DayNightVisuals (CanvasModulate)
│   ├── GardenGrid (Node2D) — draws grid, handles input
│   ├── InsectContainer (Node2D) — insects spawned here
│   └── SpawnSystem (Node)
├── GardenCamera (Camera2D)
└── UILayer (CanvasLayer, layer=10)
    ├── HUD, PhotoFocusController, ScreenFlash, etc.
```

**Blueprint's proposed tree:**
```
Root
├── GameWorld (Node2D)
│   ├── StarterZone (Garden)
│   ├── MeadowZone (Garden)
│   └── PondZone (Garden)
├── SpawnSystem (Node) ← WRONG: moved outside zones
├── Camera2D
└── UILayer
```

**Issues:**
- SpawnSystem is currently a child of Garden and references sibling InsectContainer. Moving it to a sibling of GameWorld breaks these references.
- DayNightVisuals (CanvasModulate) is a child of Garden. With 3 Gardens, you'd get 3 CanvasModulates — only the active one should apply. Better: move CanvasModulate up to Main level (one shared instance).
- Each zone needs its own InsectContainer (insects belong to a zone).

**Recommended tree (preserving existing structure):**
```
Main (Node2D)
├── DayNightVisuals (CanvasModulate) ← moved up, shared
├── GameWorld (Node2D)
│   ├── StarterZone (Node2D) ← replaces old "Garden"
│   │   ├── GardenGrid (Node2D) — 4×4, draws grid
│   │   ├── InsectContainer (Node2D)
│   │   └── SpawnSystem (Node) — or shared, see below
│   ├── MeadowZone (Node2D)
│   │   ├── GardenGrid (Node2D) — 6×6
│   │   ├── InsectContainer (Node2D)
│   │   └── SpawnSystem (Node)
│   └── PondZone (Node2D)
│       ├── GardenGrid (Node2D) — 5×5
│       ├── InsectContainer (Node2D)
│       └── SpawnSystem (Node)
├── GardenCamera (Camera2D)
└── UILayer (CanvasLayer, layer=10)
    ├── HUD
    ├── SeedToolbar (NEW)
    ├── ShopPanel (NEW)
    ├── ZoneSelector (NEW)
    ├── PhotoFocusController
    ├── ScreenFlash
    ├── StarRatingPopup
    ├── JournalUI
    └── DiscoveryNotification
```

**Alternative:** One shared SpawnSystem at the GameWorld level that queries the active zone's data. This avoids duplicating SpawnSystem 3 times. The SpawnSystem already filters by `GameManager.Instance.CurrentZone`, so it just needs to know which InsectContainer to spawn into — this can be resolved via ZoneManager.

### 6. PlantData type conflict

**Existing `PlantData.cs`** is a Godot Resource class:
```csharp
[GlobalClass]
public partial class PlantData : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string DisplayName { get; set; }
    // ... etc with [Export] attributes
}
```

**Blueprint proposes** a C# record:
```csharp
public record PlantData(string Id, string DisplayName, PlantZone Zone, ...);
```

These are completely incompatible types. The Sprint 4 context doc mentions: *"GDD says will migrate to Resources in Sprint 5 but Sprint 4 needs plant data NOW. Simplest: static PlantRegistry class (like InsectRegistry) for now."*

**Recommendation:** Use the existing Resource class for the type definition (keep PlantData as-is). Create a static `PlantRegistry` class that instantiates PlantData objects programmatically (like InsectRegistry does with InsectData). Don't introduce a record type that conflicts with the existing class.

---

## Moderate Issues (Should Fix)

### 7. Plant rarity mismatches with GDD

The blueprint changed several plant rarities from the GDD without explanation:

| Plant | GDD Rarity | Blueprint Rarity | Blueprint Cost |
|-------|-----------|-----------------|----------------|
| Dill | Uncommon | Common | 10 |
| Goldenrod | Uncommon | Common | 12 |
| Wildflower Mix | Common | Uncommon | 30 |
| Black-Eyed Susan | Uncommon | Common | 15 |
| Evening Primrose | Uncommon | Common | 8 |

Some of these changes make economic sense (Dill and Goldenrod as cheap Meadow entry points), but they should be explicitly noted as intentional balance changes rather than silently differing from the GDD. **If Karianne wants to follow the GDD strictly, revert these. If the blueprint's economy math depends on these costs, keep them but update the GDD.**

### 8. ZoneManager vs GameManager boundary unclear

GameManager already has:
- `CurrentZone` (ZoneType)
- `Nectar` + `AddNectar()` + `SpendNectar()`
- `CurrentState` (GameState enum)

The blueprint's ZoneManager duplicates zone tracking and adds unlock logic. This creates two sources of truth for "which zone is active."

**Recommendation:** Either:
- (A) Keep zone switching in ZoneManager, but have ZoneManager update `GameManager.Instance.CurrentZone` on switch (so SpawnSystem's existing zone filter still works), OR
- (B) Add zone data and unlock logic directly to GameManager (simpler, avoids a new autoload)

I'd go with (A) — ZoneManager owns zone state, but syncs `GameManager.CurrentZone` on every switch so existing code doesn't break.

### 9. GameState.Shopping missing from existing enum

The blueprint correctly identifies the need for a Shopping state but doesn't show the enum update:

```csharp
// Current:
public enum GameState { Playing, Paused, PhotoMode, Journal }
// Needed:
public enum GameState { Playing, Paused, PhotoMode, Journal, Shopping }
```

All `_UnhandledInput()` handlers (GardenGrid, HUD) that check `CurrentState == GameState.Playing` will automatically block input during Shopping — this is correct.

### 10. SeedToolbar doesn't track inventory

The blueprint's SeedToolbar shows all plants available for the current zone but has **no concept of seed inventory**. The GDD says "buy seeds with nectar" — implying you buy a seed, then plant it. The blueprint's shop instead acts as "buy and immediately add to toolbar."

Two valid models:
- **(A) Infinite seeds after purchase** (simpler — Neko Atsume model): Once you can afford to plant a type, you can plant as many as you want for the seed cost each time. No inventory tracking needed.
- **(B) Buy seed packets** (Stardew model): Buy a seed → it goes to inventory → consume on planting. Requires inventory tracking.

The blueprint implicitly uses model (A) based on the shop code (`PurchaseSeed` deducts nectar per purchase, no inventory counter). This is fine for scope, but should be stated explicitly. The SeedToolbar should show ALL unlocked seed types (not just "owned"), and planting costs nectar per placement.

### 11. WaterTilePlacedEvent is orphaned

The blueprint lists `WaterTilePlacedEvent(int X, int Y)` in the events section but then says water tiles are **pre-placed** (fixed layout). No code ever publishes this event. Remove it from the events list to avoid confusion.

### 12. SpawnSystem needs active zone awareness

The blueprint says SpawnSystem re-evaluates on `ZoneChangedEvent`, but the existing SpawnSystem filter is:
```csharp
i.Zone == GameManager.Instance.CurrentZone || i.Zone == ZoneType.Starter
```

This means Starter insects can appear in any zone. The blueprint's `ActiveZoneData.Cells` iteration is correct for checking plants, but the insect zone filter logic needs to be preserved. The blueprint doesn't address this — make sure the Starter fallback behavior is kept.

### 13. Camera bounds don't account for GardenGrid centering

The existing GardenGrid uses centered positioning: `Position = -(GridSize * TileSize / 2)`. The blueprint's camera bounds assume a (0,0) origin. These are incompatible.

**Either:**
- Change all zones to use (0,0) origin (simpler math, but requires updating GardenGrid), OR
- Calculate camera bounds relative to the centered offset

---

## Minor Issues (Nice to Fix)

### 14. Missing `_ExitTree()` unsubscribe in SeedCursor

The SeedCursor subscribes to `OnSeedSelected` in `_Ready()` but shows no `_ExitTree()` cleanup. With static EventBus delegates, this will leak. Add proper unsubscription.

### 15. ShopPanel.PopulateList() uses undefined method

`ZoneIdFromPlantZone(p.Zone)` is called but never defined. This is a helper to convert `PlantZone` → `ZoneId` — needs implementation, or use the same enum for both.

### 16. PlantZone vs ZoneType vs ZoneId — three enums for the same concept

The existing code uses `ZoneType { Starter, Meadow, Pond }`. The blueprint introduces `ZoneId { Starter, Meadow, Pond }` (identical values) and `PlantZone` (referenced in PlantData record). These should all be the same enum — `ZoneType` — since it already exists.

### 17. InsectRegistry expansion (Section 5) is thin

The blueprint says "expands to 25 species" and adds 3 new movement patterns (Dart, Skim, Pulse), but doesn't provide the actual 25-entry registry data. Compare this to the GDD which lists all 25 insects with zones, rarities, times, plants, and difficulties. The implementation will need the full InsectRegistry population — the blueprint should reference the GDD table or include it inline.

### 18. Multi-tile White Birch validation

The `TryPlantMultiTile` method is good conceptually but uses the blueprint's `CellData` type. Needs adaptation to use the existing `CellState` class and the existing `GardenGrid._cells` dictionary (`Dictionary<Vector2I, CellState>`).

---

## What the Blueprint Gets Right

Despite the conflicts, the core **architectural decisions** are solid:

1. **Multiple Garden instances with visibility swap** — correct approach for 3 small zones. Memory is negligible. ProcessMode.Disabled cascading is the right tool.

2. **ZoneManager as single authority** — clean separation. Zone data, unlock logic, and switching all in one place.

3. **Static PlantRegistry mirroring InsectRegistry** — correct pattern for Sprint 4. Migrate to Resources in Sprint 5.

4. **Bottom hotbar for seed selection** — good UX research. Matches the cozy farming game conventions.

5. **Shop as modal overlay with GameState.Shopping** — prevents accidental clicks, clean state management.

6. **Water as CellType, not CellState** — immutable tile type vs mutable plant lifecycle. Correct modeling.

7. **Pre-placed water tiles** — simpler than purchasable infrastructure, gives Pond zone instant identity.

8. **Economy math (2× ROI, 15-min Meadow)** — well-researched targets with Stardew/Neko Atsume precedent.

9. **EventBus records for new events** — clean, typed, follows established pattern.

10. **Spawn conditions (MinInsectsPresent, WaterRequired, MultiPlantCombo)** — excellent emergent complexity from simple composable rules.

11. **ProcessMode guard pattern for EventBus handlers** — critical insight about static delegates bypassing Godot processing.

---

## Recommended Action Plan

1. **Use the blueprint as design reference** — the architecture, economy math, UX decisions, and spawn conditions are all solid.

2. **Don't copy-paste the code** — every code sample needs adaptation for the existing codebase (128px tiles, existing CellState, existing EventBus API, existing scene tree).

3. **Extend, don't replace** — add CellType to existing CellState, add ZoneManager alongside GameManager, add GameState.Shopping to existing enum.

4. **Unify enums** — use `ZoneType` everywhere (not ZoneId or PlantZone).

5. **Move DayNightVisuals up** — single CanvasModulate shared across all zones.

6. **Keep SpawnSystem behavior** — preserve the Starter-zone fallback filter, active zone InsectContainer reference.

7. **Validate GDD alignment** — confirm with Karianne whether the rarity/cost changes are intentional rebalancing or drift.
