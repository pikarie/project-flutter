# Project Flutter — Sprint 4 Deep Search Context

## Project Overview
Cozy top-down garden game (Godot 4.5, C# 12 / .NET 8). Grow plants → attract insects → photograph them → fill a field journal. No GDScript, C# only. All visuals use `_Draw()` placeholders (no real art yet).

## Sprint 4 Goal: Economy & Zones
From GDD:
- Nectar currency system (harvest flowers, earn nectar)
- Seed shop UI (buy seeds with nectar)
- Zone unlock system (nectar cost + journal entry requirements)
- Build all 3 zones with proper backgrounds
- Zone switching UI/navigation
- Pond zone water tiles (special infrastructure)
- **Deliverable:** Full progression loop from starter to all zones

---

## Current Architecture

### Scene Tree (Main.tscn)
```
Main (Node2D)
├── Garden (Node2D)
│   ├── DayNightVisuals (CanvasModulate) — color tweens per time period
│   ├── GardenGrid (Node2D) — 4x4 grid, 128px tiles, _Draw() placeholders
│   │   ├── GroundLayer (TileMapLayer)
│   │   └── [Plants are drawn directly by GardenGrid._Draw()]
│   ├── InsectContainer (Node2D) — spawned insects live here (group: "insect_container")
│   └── SpawnSystem (Node) — timer-based spawn on blooming plants
├── GardenCamera (Camera2D) — WASD pan, mouse wheel zoom
└── UILayer (CanvasLayer, layer=10) — all UI above CanvasModulate
    ├── HUD (Control) — time, nectar, speed/photo/journal buttons
    ├── PhotoFocusController (Control) — focus circle overlay
    ├── ScreenFlash (ColorRect) — white flash on photo
    ├── StarRatingPopup (Control) — floating star rating
    ├── JournalUI (Control) — programmatic grid+detail journal
    └── DiscoveryNotification (Control) — toast notifications
```

### Autoloads (Godot Node singletons, registered in project.godot)
- **GameManager** — `static Instance`, GameState enum, CurrentZone, Nectar
- **TimeManager** — `static Instance`, day/night cycle, SpeedMultiplier
- **JournalManager** — `static Instance`, Dictionary<string,int> discovered species

### EventBus (Pure static C# class, NOT an autoload)
```csharp
public static class EventBus
{
    public static void Subscribe<T>(Action<T> callback);
    public static void Unsubscribe<T>(Action<T> callback);
    public static void Publish<T>(T evt);
}
```

### All Event Records (Events.cs)
```csharp
namespace ProjectFlutter;

// Time
public record HourPassedEvent(int Hour);
public record TimeOfDayChangedEvent(string OldPeriod, string NewPeriod);

// Game state
public record GameStateChangedEvent(GameManager.GameState NewState);
public record NectarChangedEvent(int NewAmount);
public record PauseToggledEvent(bool IsPaused);

// Plants
public record PlantPlantedEvent(string PlantType, Vector2I GridPos);
public record PlantHarvestedEvent(string PlantType, Vector2I GridPos);
public record PlantBloomingEvent(Vector2I GridPos);
public record PlantRemovedEvent(Vector2I GridPos);

// Journal
public record SpeciesDiscoveredEvent(string InsectId);
public record JournalUpdatedEvent(string InsectId, int StarRating);

// Insects
public record InsectArrivedEvent(string InsectId, Vector2 Position);
public record InsectDepartingEvent(string InsectId, Node2D Insect);
public record InsectDepartedEvent(string InsectId, Vector2 Position);
public record InsectClickedEvent(string InsectId, Node2D Insect, Vector2 Position);

// Photography
public record PhotoTakenEvent(string InsectId, string DisplayName, int StarRating, Vector2 WorldPosition);
public record PhotoMissedEvent(Vector2 WorldPosition);
```

---

## Key Source Files (Complete Current State)

### GameManager.cs (Autoload)
```csharp
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    public enum GameState { Playing, Paused, PhotoMode, Journal }
    public GameState CurrentState { get; private set; } = GameState.Playing;
    public ZoneType CurrentZone { get; set; } = ZoneType.Starter;
    public int Nectar { get; private set; }

    public override void _Ready() => Instance = this;

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        EventBus.Publish(new GameStateChangedEvent(newState));
    }

    public void AddNectar(int amount)
    {
        Nectar += amount;
        EventBus.Publish(new NectarChangedEvent(Nectar));
    }

    public bool SpendNectar(int amount)
    {
        if (Nectar < amount) return false;
        Nectar -= amount;
        EventBus.Publish(new NectarChangedEvent(Nectar));
        return true;
    }
}
```

### TimeManager.cs (Autoload)
```csharp
public partial class TimeManager : Node
{
    public static TimeManager Instance { get; private set; }
    public const float DayCycleDuration = 300.0f;
    public float SpeedMultiplier { get; private set; } = 1.0f;
    public float CurrentTimeNormalized { get; private set; } = 0.25f;  // 0-1, starts at 6:00
    public string CurrentPeriod { get; private set; }
    public bool Paused { get; set; }

    // Periods: night (<5.5h), dawn (5.5-7h), morning (7-10h), noon (10-14h), golden_hour (14-17h), dusk (17-19.5h)
    // Publishes HourPassedEvent and TimeOfDayChangedEvent
    public void SetSpeed(float multiplier);
    public bool IsDaytime();   // dawn, morning, noon, golden_hour
    public bool IsNighttime(); // dusk, night
}
```

### JournalManager.cs (Autoload)
```csharp
public partial class JournalManager : Node
{
    public static JournalManager Instance { get; private set; }
    private readonly Dictionary<string, int> _discoveredSpecies = new();

    public void DiscoverSpecies(string insectId, int starRating);
    // Publishes SpeciesDiscoveredEvent (new) or JournalUpdatedEvent (better stars)
    public bool IsDiscovered(string insectId);
    public int GetStarRating(string insectId);
    public int GetDiscoveredCount();
}
```

### GardenGrid.cs (Node2D — the garden's grid system)
```csharp
public partial class GardenGrid : Node2D
{
    [Export] public Vector2I GridSize { get; set; } = new(4, 4);
    [Export] public int TileSize { get; set; } = 128;

    private Dictionary<Vector2I, CellState> _cells = new();

    // Centered at origin: Position = -(GridSize * TileSize / 2)
    // All cells start as Tilled
    // Uses _Draw() for: grass background, soil tiles, plant placeholders, hover highlight
    // _UnhandledInput: left click = plant/water/grow/harvest cycle, right click = remove
    // Harvesting blooming plant: AddNectar(3), reverts to Growing, publishes PlantHarvestedEvent

    public Dictionary<Vector2I, CellState> GetCells();
    public CellState GetCell(Vector2I pos);
    public Vector2 GridToWorld(Vector2I gridPos);  // returns center of tile in local coords
}
```

**Current limitation:** All cells start as Tilled (no Empty state in practice). Planting always uses `PlantType = "placeholder"`. No seed type selection exists yet. Harvesting always gives 3 nectar.

### CellState.cs
```csharp
public class CellState
{
    public enum State { Empty, Tilled, Planted, Watered, Growing, Blooming }
    public State CurrentState { get; set; } = State.Empty;
    public string PlantType { get; set; } = "";
    public int GrowthStage { get; set; }
    public bool IsWatered { get; set; }
    public Node2D PlantNode { get; set; }
    public int MaxInsectSlots { get; set; } = 2;

    public bool CanPlant();
    public bool HasAvailableSlot();
    public int OccupiedSlotCount { get; }
    public void OccupySlot(Node2D insect);
    public void VacateSlot(Node2D insect);
    public void ClearSlots();
}
```

### InsectRegistry.cs (Static shared species data)
```csharp
public static class InsectRegistry
{
    private static List<InsectData> _allSpecies;
    public static IReadOnlyList<InsectData> AllSpecies => _allSpecies ??= CreateSpeciesList();
    public static int TotalSpeciesCount => AllSpecies.Count;
    public static InsectData GetById(string id);
}
```
Currently holds 4 test species (all ZoneType.Starter):
- honeybee (Hover, day, weight 1.0)
- cabbage_white (Flutter, day, weight 0.8)
- ladybug (Crawl, day, weight 0.7)
- sphinx_moth (Erratic, night, weight 0.5)

### InsectData.cs (Resource class)
```csharp
[GlobalClass]
public partial class InsectData : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public ZoneType Zone { get; set; }
    [Export] public string Rarity { get; set; }
    [Export] public string TimeOfDay { get; set; }              // "day" / "night" / "both"
    [Export] public string[] RequiredPlants { get; set; }
    [Export] public float SpawnWeight { get; set; } = 1.0f;
    [Export] public float VisitDurationMin { get; set; }
    [Export] public float VisitDurationMax { get; set; }
    [Export] public string PhotoDifficulty { get; set; }
    [Export] public MovementPattern MovementPattern { get; set; }
    [Export] public float MovementSpeed { get; set; }
    [Export] public float PauseFrequency { get; set; }
    [Export] public float PauseDuration { get; set; }
    [Export] public SpriteFrames GardenSprite { get; set; }
    [Export] public Texture2D JournalIllustration { get; set; }
    [Export] public Texture2D JournalSilhouette { get; set; }
    [Export] public string JournalText { get; set; }
    [Export] public string HintText { get; set; }
    [Export] public AudioStream AmbientSound { get; set; }
}
```

### SpawnSystem.cs (Insect spawning)
```csharp
public partial class SpawnSystem : Node
{
    [Export] public int MaxInsectsPerZone { get; set; } = 10;
    [Export] public float SpawnCheckInterval { get; set; } = 5.0f;

    // Timer-based: checks every SpawnCheckInterval (game-time, scaled by TimeManager.SpeedMultiplier)
    // Finds blooming cells with available slots
    // 40% quiet ticks for anticipation pacing
    // Filters by: MatchesTimeOfDay, Zone matches CurrentZone (or Starter), MeetsPlantRequirements
    // Weighted random selection from eligible insects
    // Registers slot via cell.OccupySlot(), auto-vacate via TreeExiting signal
}
```

**Key zone filter in SpawnSystem:** `i.Zone == GameManager.Instance.CurrentZone || i.Zone == ZoneType.Starter`

### Insect.cs (Area2D — the insect entity)
```csharp
public partial class Insect : Area2D
{
    public enum InsectState { Arriving, Visiting, Departing, Freed }

    public InsectData Data { get; }
    public InsectState CurrentState { get; }
    public bool IsPhotographable => _state == InsectState.Visiting && !_isFrozen && IsInsideTree();

    public void Initialize(InsectData data, Vector2 plantPosition, Vector2 entryPosition, Vector2I hostCellPos);
    public void Freeze(float duration = 0.5f);  // Photography freeze effect

    // Lifecycle: Arriving (tween 1.5s) → Visiting (movement behavior) → Departing (tween 1.2s) → QueueFree
    // Movement via IMovementBehavior strategy pattern
    // Soft clamp to 50px wander distance from anchor plant
    // Responds to TimeOfDayChangedEvent (day insects leave at night, etc.)
    // Responds to PlantRemovedEvent (only for own host cell)
    // _Draw() placeholder: colored circle per MovementPattern + idle bob
}
```

### Enums
```csharp
public enum ZoneType { Starter, Meadow, Pond }
public enum MovementPattern { Hover, Flutter, Crawl, Erratic }
```

### Movement Behaviors (IMovementBehavior)
```csharp
public interface IMovementBehavior
{
    Vector2 CalculatePosition(float delta);
    void Reset(Vector2 anchor);
}
// Implementations: HoverBehavior (FastNoiseLite), FlutterBehavior (sine wave),
//                  CrawlBehavior (elliptical), ErraticBehavior (random + tether)
// Factory: MovementBehaviorFactory.Create(MovementPattern, InsectData, RNG)
```

### HUD.cs (User interface)
```csharp
public partial class HUD : Control
{
    // Child nodes: TimeLabel, NectarLabel, SpeedButton, PhotoButton, JournalButton, PhotoModeLabel
    // Subscribes: HourPassedEvent, TimeOfDayChangedEvent, NectarChangedEvent, GameStateChangedEvent
    // _UnhandledInput: C (toggle photo mode), J (toggle journal), Escape (back to Playing)
    // Fluid navigation: can switch directly between any states (PhotoMode↔Journal, etc.)
    // Speed: cycles x1 → x2 → x3 → x10
    // Time display: "HH:MM (period)"
    // Nectar display: "Nectar: N"
}
```

### Photography System
- **PhotoFocusController**: Click & hold to focus (1.5s), quality zones based on fixed world radius (80px), live color feedback (green→orange→red→grey), visual frame + crosshairs + corner brackets
- **ScreenFlash**: White overlay that fades on PhotoTakenEvent
- **StarRatingPopup**: Floating stars that drift up and fade
- **PhotoFocusController constants**: `FocusDuration = 1.5f`, `WorldRadius = 80f`, `ThreeStarPct = 0.15f`, `TwoStarPct = 0.40f`

### Journal System
- **JournalUI**: Programmatic UI (no .tscn). Grid view (4 columns) + detail view. Discovered = colored cell + name + stars. Undiscovered = grey "???".
- **DiscoveryNotification**: FIFO queue toast system. Green banner for new discoveries, gold for star upgrades. Slide down → display → fade out.

---

## GDD Specifications for Sprint 4

### 4.6 Currency & Economy
- **Currency: Nectar** (universal)
- Earned by: harvesting blooming flowers (click → collect → plant reverts to Growing)
- **Core tension:** Harvesting = nectar to buy seeds/zones, BUT plant stops blooming temporarily = fewer insects
- Nectar costs:
  - Common seed packet: 5-10 nectar
  - Uncommon seed packet: 20-30 nectar
  - Rare seed packet: 50-75 nectar
  - Zone unlock: 100-200 nectar
  - Infrastructure (pond, lamp): 50-100 nectar
- Economy should feel **generous, not grindy**

### 4.8 Zone Progression
- **Zone 1 — Starter Garden (4x4 grid)** — Unlocked from start
  - Plants: lavender, sunflower, daisy, coneflower, marigold
  - Insects: honeybee, bumblebee, cabbage white, ladybug, garden spider
  - Night: moonflower, evening primrose → sphinx moth, owl moth

- **Zone 2 — Meadow (6x6 grid)** — Unlock: 100 nectar + 5 journal entries
  - Plants: milkweed, dill, goldenrod, wildflower mix, black-eyed susan
  - Insects: monarch, swallowtail, hoverfly, grasshopper, painted lady, praying mantis
  - Night: night-blooming jasmine, white birch → luna moth, atlas moth

- **Zone 3 — Pond Edge (5x5 grid + water tiles)** — Unlock: 150 nectar + 12 journal entries
  - Plants: water lily, cattail, iris, lotus, passionflower
  - Insects: dragonfly, damselfly, water strider, pond skater, gulf fritillary, emperor dragonfly
  - Night: switchgrass → firefly, cricket

- Night-blooming plants and nocturnal insects appear in any zone
- Player switches between zones freely once unlocked
- Each zone has unique background art and ambient sounds
- Some insects can ONLY appear in specific zones

### Plant Data (GDD Section 5.1 — 20 plants total)
| Plant | Zone | Rarity | Night? | Attracts |
|-------|------|--------|--------|----------|
| Lavender | Starter | Common | No | Honeybee, Bumblebee |
| Sunflower | Starter | Common | No | Bees, Ladybug |
| Daisy | Starter | Common | No | Cabbage White |
| Coneflower | Starter | Common | No | Butterflies, bees |
| Marigold | Starter | Common | No | Ladybug, Hoverfly |
| Milkweed | Meadow | Uncommon | No | Monarch (exclusive) |
| Dill | Meadow | Uncommon | No | Swallowtail, Ladybug |
| Goldenrod | Meadow | Uncommon | No | Many species |
| Wildflower Mix | Meadow | Common | No | Grasshopper, butterflies |
| Black-Eyed Susan | Meadow | Uncommon | No | Hoverfly, bees |
| Water Lily | Pond | Uncommon | No | Dragonfly, Damselfly |
| Cattail | Pond | Common | No | Dragonfly, Water Strider |
| Iris | Pond | Uncommon | No | Damselfly, butterflies |
| Lotus | Pond | Rare | No | Dragonfly (rare variant) |
| Passionflower | Pond | Rare | No | Gulf Fritillary (exclusive) |
| Moonflower | Starter | Uncommon | Yes | Sphinx Moth (exclusive) |
| Evening Primrose | Starter | Uncommon | Yes | Moths, Fireflies |
| Night-Blooming Jasmine | Meadow | Rare | Yes | Luna Moth |
| White Birch (2x2) | Meadow | Rare | Yes | Luna Moth (pair with Jasmine) |
| Switchgrass | Pond | Common | Yes | Crickets, Fireflies |

### Insect Data (GDD Section 5.2 — 25 insects total)
| Insect | Zone | Rarity | Time | Required Plants | Difficulty |
|--------|------|--------|------|----------------|-----------|
| Honeybee | Starter | Common | Day | Lavender, Sunflower, any | Easy |
| Bumblebee | Starter | Common | Day | Lavender, Coneflower | Easy |
| Cabbage White | Starter | Common | Day | Daisy, any | Easy |
| Ladybug | Starter | Common | Day | Sunflower, Dill, Marigold | Easy |
| Garden Spider | Starter | Uncommon | Day | Any 3+ blooming | Medium |
| Monarch | Meadow | Uncommon | Day | Milkweed (exclusive) | Medium |
| Swallowtail | Meadow | Uncommon | Day | Dill, Coneflower | Medium |
| Hoverfly | Meadow | Common | Day | Goldenrod, Black-Eyed Susan | Easy |
| Grasshopper | Meadow | Common | Day | Wildflower Mix, Switchgrass | Medium |
| Painted Lady | Meadow | Uncommon | Day | Goldenrod, Coneflower | Medium |
| Praying Mantis | Meadow | Rare | Day | 5+ insects present | Hard |
| Dragonfly | Pond | Uncommon | Day | Water Lily, Cattail | Hard |
| Damselfly | Pond | Uncommon | Day | Iris, Water Lily | Medium |
| Water Strider | Pond | Common | Day | Cattail + water tiles | Medium |
| Pond Skater | Pond | Common | Both | Water tiles present | Easy |
| Gulf Fritillary | Pond | Rare | Day | Passionflower (exclusive) | Hard |
| Emperor Dragonfly | Pond | Rare | Day | Lotus + 2 water tiles | Hard |
| Sphinx Moth | Starter | Uncommon | Night | Moonflower (exclusive) | Medium |
| Firefly | Pond | Uncommon | Night | Evening Primrose + Switchgrass | Medium |
| Cricket | Pond | Common | Night | Switchgrass, any grass | Easy |
| Luna Moth | Meadow | Rare | Night | Jasmine + White Birch | Hard |
| Owl Moth | Starter | Uncommon | Night | Any night-blooming | Medium |
| Atlas Moth | Meadow | Very Rare | Night | All 4 night plants | Very Hard |
| Jewel Beetle | Meadow | Rare | Day | Sunflower + Goldenrod | Hard |
| Monarch Migration | Meadow | Very Rare | Day | 3+ Milkweed + Goldenrod | Special |

---

## What Sprint 4 Needs to Implement

### 1. Plant Type System
**Current state:** GardenGrid uses `PlantType = "placeholder"` for all plants. No seed selection exists.
**Needed:**
- A `PlantRegistry` (similar to `InsectRegistry`) with all 20 plant species data
- PlantData class already exists but isn't populated with real data
- Seed selection: when clicking an empty/tilled cell, choose which seed to plant
- Different plants should have different visual placeholders (color-coded by rarity/zone)
- Night-blooming plants need special behavior (only attract at night)
- Different plants = different insect slots count, growth speed, nectar yield

### 2. Nectar Economy
**Current state:** `GameManager.AddNectar(3)` called on harvest. `GameManager.SpendNectar(amount)` exists but is never called.
**Needed:**
- Variable nectar yield per plant type (PlantData.NectarYield)
- Spending nectar in seed shop and zone unlocks
- Economy balancing (generous, not grindy — per GDD)
- NectarChangedEvent is already published and HUD listens to it

### 3. Seed Shop UI
**Current state:** Nothing exists.
**Needed:**
- New UI accessible via HUD button (like Journal)
- Grid of available seeds with costs
- Seeds unlocked by zone progression (Starter seeds always available, Meadow/Pond seeds after zone unlock)
- Purchase flow: select seed → spend nectar → seed becomes "active" for planting
- Could add a new GameState (e.g., `GameState.Shop`) or use a simpler popup approach
- Player should be able to select seed type, THEN click grid cells to plant

### 4. Zone System
**Current state:** `GameManager.CurrentZone = ZoneType.Starter`. `ZoneType` enum has {Starter, Meadow, Pond}. SpawnSystem already filters by zone. Only one Garden scene exists (4x4 grid).
**Needed:**
- Multiple garden grids: Starter (4x4), Meadow (6x6), Pond (5x5 + water tiles)
- Zone switching UI (buttons, map, or similar navigation)
- Zone unlock conditions: nectar cost + journal entry count
- Each zone persists independently (CellState saved per zone)
- Pond zone needs special water tiles that aren't plantable but are infrastructure
- Camera may need to adjust bounds per zone (different grid sizes)
- SpawnSystem needs to work with the active zone's grid

### 5. Pond Infrastructure
**Current state:** Nothing exists.
**Needed:**
- Water tiles in Pond zone (non-plantable, visual water effect)
- Some insects require water tiles present (Water Strider, Pond Skater)
- Potentially purchasable with nectar (50-100 per GDD)

### 6. Update InsectRegistry
**Current state:** 4 test species, all ZoneType.Starter.
**Needed:**
- Expand to all 25 species from GDD with proper zones, rarities, required plants, spawn weights, visit durations, difficulties
- Movement patterns for new species (dragonfly = hard/fast, firefly = timing-based, etc.)
- RequiredPlants arrays need to match PlantRegistry IDs

---

## Key Architectural Questions for Sprint 4

1. **Zone architecture**: Should each zone be a separate Garden.tscn scene instance, or one Garden with swappable data? Options:
   - Multiple Garden scene instances (one per zone), swap visibility
   - Single Garden that reconfigures its grid size and cells on zone switch
   - Separate scenes loaded via `PackedScene.Instantiate()` on zone switch

2. **Seed selection flow**: How should the player select which seed to plant?
   - Toolbar/hotbar at bottom of screen with owned seeds
   - Shop popup where you buy + immediately place
   - Cursor mode: select seed from shop → cursor changes → click grid to plant

3. **PlantRegistry design**: Should it mirror InsectRegistry (static C# class) or use Godot Resources (.tres files)?
   - GDD says "will migrate to Resources in Sprint 5" but Sprint 4 needs plant data NOW
   - Simplest: static PlantRegistry class (like InsectRegistry) for now, migrate later

4. **Zone persistence**: When switching zones, do we keep all zones in memory or serialize/deserialize?
   - Simplest: all 3 zones in memory (small game, max 6x6 = 36 cells per zone)
   - Alternative: save zone state to dictionary, recreate on switch

5. **Garden scene structure**: Does Main.tscn have 3 Garden children (one per zone), or does it dynamically load them?

6. **Pond water tiles**: Are they part of the grid (special CellState) or separate non-grid Node2D?
   - Option A: CellState gets a new state like `Water` (non-plantable)
   - Option B: Pond grid has predefined water cells at fixed positions

---

## Patterns to Follow (Established in Sprints 1-3)

- All cross-system communication via `EventBus.Publish<T>()` / `Subscribe<T>()`
- All subscribers unsubscribe in `_ExitTree()`
- Store `Action<T>` delegates as fields for proper unsubscription
- Use `TimeManager.Instance.SpeedMultiplier` for game-time scaling
- Use `_Draw()` for all placeholder visuals
- Use `_UnhandledInput()` for game-world clicks (prevents UI click-through)
- UI lives on `CanvasLayer layer=10` (above CanvasModulate)
- `GameManager.Instance.ChangeState()` for mode switching
- Programmatic UI construction (like JournalUI) — no .tscn for complex UI
- Static registries for data (InsectRegistry pattern)
- GameState enum manages modes: `Playing, Paused, PhotoMode, Journal`
- Keyboard shortcuts centralized in HUD._UnhandledInput

## Config Variables (from GDD)
```csharp
// NectarEconomy
HarvestNectarBase = 3;          // nectar per common plant harvest
RegrowCycles = 1;               // cycles to re-bloom after harvest

// Seed costs
CommonSeed = 5-10;
UncommonSeed = 20-30;
RareSeed = 50-75;
ZoneUnlock = 100-200;
Infrastructure = 50-100;
```

---

## File System (Current)
```
project-flutter/src/
├── scenes/
│   ├── main/Main.tscn
│   ├── garden/
│   │   ├── Garden.tscn
│   │   ├── GardenGrid.cs
│   │   ├── GardenCamera.cs
│   │   └── DayNightVisuals.cs
│   ├── insects/
│   │   ├── Insect.tscn
│   │   └── Insect.cs
│   ├── photography/
│   │   ├── PhotoFocusController.cs
│   │   ├── ScreenFlash.cs
│   │   └── StarRatingPopup.cs
│   ├── journal/
│   │   ├── JournalUI.cs
│   │   └── DiscoveryNotification.cs
│   └── ui/
│       ├── HUD.tscn
│       └── HUD.cs
├── scripts/
│   ├── autoload/
│   │   ├── GameManager.cs
│   │   ├── TimeManager.cs
│   │   ├── JournalManager.cs
│   │   ├── EventBus.cs
│   │   └── Events.cs
│   ├── data/
│   │   ├── PlantData.cs
│   │   ├── InsectData.cs
│   │   ├── InsectRegistry.cs
│   │   ├── ZoneType.cs
│   │   ├── MovementPattern.cs
│   │   └── CellState.cs
│   └── systems/
│       ├── SpawnSystem.cs
│       ├── IMovementBehavior.cs
│       ├── HoverBehavior.cs
│       ├── FlutterBehavior.cs
│       ├── CrawlBehavior.cs
│       └── ErraticBehavior.cs
```

### Files That Will Need Changes in Sprint 4
- **GameManager.cs** — May need new GameState for Shop, zone unlock tracking
- **GardenGrid.cs** — Major: configurable grid sizes, seed type selection, plant type visuals, water tiles
- **CellState.cs** — May need Water state for pond tiles
- **HUD.cs** — Shop button, zone selector UI, possibly seed hotbar
- **SpawnSystem.cs** — Already zone-aware, may need minor adjustments
- **InsectRegistry.cs** — Expand from 4 to 25 species
- **Events.cs** — New events for zone changes, shop, seed selection
- **Main.tscn** — Multi-zone garden structure

### New Files Expected in Sprint 4
- **PlantRegistry.cs** — Static plant species data (mirrors InsectRegistry)
- **SeedShopUI.cs** — Seed purchasing interface
- **ZoneManager.cs** or zone logic in GameManager — Zone unlock tracking, switching
- Possibly per-zone Garden scenes or a ZoneData configuration
