# Project Flutter — Sprint 5 Deep Search Context

## Project Overview
Cozy top-down garden game (Godot 4.5, C# 12 / .NET 8). Grow plants → attract insects → photograph them → fill a field journal. No GDScript, C# only. All visuals currently use `_Draw()` placeholders (no real art yet). Sprints 1–4 complete.

## Sprint 5 Goal: Content & Art
From GDD:
- Final art for all 20 plants (80 growth sprites)
- Final art for all 25 insects (garden sprites + journal illustrations)
- All insect data resources configured and balanced
- Discovery hints for all species
- Journal text (EN + FR) for all entries
- Localization system setup (CSV-based)
- **Deliverable:** All content in-game, playable start to finish

---

## Current Architecture

### Scene Tree (Main.tscn)
```
Main (Node2D)
├── DayNightVisuals (CanvasModulate) — color tweens per time period
├── GameWorld (Node2D) — manages zone visibility/ProcessMode
│   ├── StarterZone (Garden instance) — 4x4 grid
│   │   ├── GardenGrid (Node2D)
│   │   │   └── GroundLayer (TileMapLayer)
│   │   ├── InsectContainer (Node2D)
│   │   └── SpawnSystem (Node)
│   ├── MeadowZone (Garden instance) — 6x6 grid (starts disabled)
│   │   └── ...
│   └── PondZone (Garden instance) — 5x5 grid + water tiles (starts disabled)
│       └── GardenGrid (WaterTileData = PackedInt32Array(4,1,3,2,4,2,3,3,4,3))
├── GardenCamera (Camera2D) — WASD pan, mouse wheel zoom, middle-click drag
└── UILayer (CanvasLayer, layer=10) — all UI above CanvasModulate
    ├── HUD (Control) — time, nectar, speed/photo/journal buttons, F1 debug hint
    ├── ZoneSelector (Control) — tab navigation + unlock panel
    ├── SeedToolbar (Control) — seed purchasing (keys 1-9)
    ├── PhotoFocusController (Control) — focus circle overlay
    ├── ScreenFlash (ColorRect) — white flash on photo
    ├── StarRatingPopup (Control) — floating star rating
    ├── JournalUI (Control) — programmatic grid+detail journal
    ├── HarvestPopup (Control) — floating "+N nectar" golden popup
    └── DiscoveryNotification (Control) — toast notifications
```

### Autoloads (Godot Node singletons, registered in project.godot)
- **GameManager** — `static Instance`, GameState enum {Playing, Paused, PhotoMode, Journal}, CurrentZone, Nectar (starts at 25)
- **TimeManager** — `static Instance`, day/night cycle (300s default), SpeedMultiplier, periods: night/dawn/morning/noon/golden_hour/dusk
- **JournalManager** — `static Instance`, Dictionary<string,int> discovered species, star ratings
- **ZoneManager** — `static Instance`, zone unlock/switching, DebugUnlockAll (F1)

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
public record PlantHarvestedEvent(string PlantType, Vector2I GridPos, int NectarYield, Vector2 WorldPosition);
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

// Seeds
public record SeedSelectedEvent(string PlantId);

// Zones
public record ZoneChangedEvent(ZoneType From, ZoneType To);
public record ZoneUnlockedEvent(ZoneType Zone);
```

---

## Key Source Files (Complete Current State)

### PlantData.cs (Resource class)
```csharp
[GlobalClass]
public partial class PlantData : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public ZoneType Zone { get; set; }
    [Export] public string Rarity { get; set; }
    [Export] public int SeedCost { get; set; }
    [Export] public int NectarYield { get; set; }
    [Export] public int GrowthCycles { get; set; }
    [Export] public int InsectSlots { get; set; }
    [Export] public bool NightBlooming { get; set; }
    [Export] public Texture2D[] GrowthSprites { get; set; }    // Currently null — placeholder _Draw() used
    [Export] public string[] AttractedInsects { get; set; }
    [Export] public Color DrawColor { get; set; }               // Placeholder color per plant
}
```

### PlantRegistry.cs (Static — 20 plants fully defined)
All 20 plants configured with IDs, costs, yields, zone, rarity, DrawColor, AttractedInsects:
- **Starter (7):** lavender, sunflower, daisy, coneflower, marigold, moonflower (night), evening_primrose (night)
- **Meadow (7):** wildflower_mix, milkweed, dill, goldenrod, black_eyed_susan, night_jasmine (night/rare), white_birch (night/rare)
- **Pond (6):** cattail, switchgrass (night), water_lily, iris, lotus (rare), passionflower (rare)

Each plant has: Id, DisplayName, Zone, Rarity, SeedCost (5-75), NectarYield (3-10), GrowthCycles (2-5), InsectSlots (2-3), NightBlooming, AttractedInsects[], DrawColor.

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
    [Export] public int RequiredWaterTiles { get; set; }
    [Export] public float VisitDurationMin { get; set; } = 60.0f;
    [Export] public float VisitDurationMax { get; set; } = 180.0f;
    [Export] public string PhotoDifficulty { get; set; }
    [Export] public MovementPattern MovementPattern { get; set; } = MovementPattern.Hover;
    [Export] public float MovementSpeed { get; set; } = 30.0f;
    [Export] public float PauseFrequency { get; set; } = 0.4f;
    [Export] public float PauseDuration { get; set; } = 2.0f;
    [Export] public SpriteFrames GardenSprite { get; set; }     // Currently null — placeholder _Draw() used
    [Export] public Texture2D JournalIllustration { get; set; } // Currently null — colored rect placeholder
    [Export] public Texture2D JournalSilhouette { get; set; }   // Currently null — grey rect placeholder
    [Export] public string JournalText { get; set; }            // Populated with English text
    [Export] public string HintText { get; set; }               // Populated with English hint
    [Export] public AudioStream AmbientSound { get; set; }      // Currently null
}
```

### InsectRegistry.cs (Static — all 25 species fully defined)
All 25 insects configured with movement patterns, spawn weights, visit durations, required plants, journal text, hint text:
- **Starter Day (5):** honeybee, bumblebee, cabbage_white, ladybug, garden_spider
- **Starter Night (2):** sphinx_moth, owl_moth
- **Meadow Day (6):** monarch_butterfly, swallowtail, hoverfly, grasshopper, painted_lady, praying_mantis, jewel_beetle
- **Meadow Night (2):** luna_moth, atlas_moth
- **Meadow Special (1):** monarch_migration
- **Pond Day (5):** dragonfly, damselfly, water_strider (requiredWaterTiles:1), pond_skater (requiredWaterTiles:1), gulf_fritillary, emperor_dragonfly (requiredWaterTiles:2)
- **Pond Night (2):** firefly, cricket

### Insect.cs (Area2D — the insect entity)
```csharp
public partial class Insect : Area2D
{
    // States: Arriving → Visiting → Departing → Freed
    // Movement via IMovementBehavior strategy pattern
    // Placeholder _Draw(): unique color per species via ID hash + movement-specific details
    // Wings for Flutter, spots for Crawl, idle bob indicator
    // GardenSprite (SpriteFrames) field exists but is currently null for all insects
    // If GardenSprite were loaded, would need AnimatedSprite2D child instead of _Draw()

    // Freeze(duration) for photography
    // Responds to TimeOfDayChangedEvent and PlantRemovedEvent
    // CanProcess() guard for EventBus handlers when zone is disabled
}
```

### GardenGrid.cs (Node2D — garden grid system)
```csharp
public partial class GardenGrid : Node2D
{
    [Export] public Vector2I GridSize { get; set; } = new(4, 4);
    [Export] public int TileSize { get; set; } = 128;
    [Export] public int[] WaterTileData { get; set; } = Array.Empty<int>();

    // Centered at origin: Position = -(GridSize * TileSize / 2)
    // All non-water cells start as Tilled
    // Placeholder _Draw() for: grass background, soil tiles, water tiles (blue + wave accents),
    //   plant growth stages (seed circle, sprout stem+leaves, bloom flower with plant-specific DrawColor)
    //   hover highlight, seed preview
    // GrowthSprites (Texture2D[]) field exists on PlantData but is currently null
    // If loaded, would need to draw sprites instead of colored circles/shapes

    public Dictionary<Vector2I, CellState> GetCells();
    public CellState GetCell(Vector2I pos);
    public Vector2 GridToWorld(Vector2I gridPos);  // returns center of tile in local coords
    public int CountWaterTiles();
}
```

### CellState.cs
```csharp
public class CellState
{
    public enum State { Empty, Tilled, Planted, Watered, Growing, Blooming }
    public State CurrentState { get; set; } = State.Empty;
    public string PlantType { get; set; } = "";
    public int GrowthStage { get; set; }
    public bool IsWatered { get; set; }
    public bool IsWater { get; set; }
    public Node2D PlantNode { get; set; }
    public int MaxInsectSlots { get; set; } = 2;

    public bool HasAvailableSlot();
    public void OccupySlot(Node2D insect);
    public void VacateSlot(Node2D insect);
    public void ClearSlots();
}
```

### JournalUI.cs (Programmatic UI — no .tscn)
```csharp
public partial class JournalUI : Control
{
    // Grid view: 4 columns of species cells (Button + StyleBoxFlat)
    // Discovered = species-specific color background + name + stars
    // Undiscovered = grey "???"
    // Detail view: colored rect portrait, name, stars, JournalText, HintText
    // Uses hardcoded GetSpeciesColor(string speciesId) switch for 25 species
    // Footer: "Discovered: N / 25"

    // Currently shows text-only content:
    //   - Portrait is a ColorRect (placeholder for JournalIllustration texture)
    //   - No silhouette shown for undiscovered (grey rect placeholder for JournalSilhouette)
    //   - JournalText shown from InsectRegistry (English only)
    //   - HintText shown for all species (even undiscovered — design choice)
}
```

### DiscoveryNotification.cs
```csharp
// FIFO queue toast system
// Green banner for new discoveries, gold for star upgrades
// Slide down from top center → display 2.5s → fade out
// Uses _Draw() with DrawRect/DrawString
// Shows species DisplayName from InsectRegistry
```

### HarvestPopup.cs
```csharp
// Floating golden "+N" popup at harvest world position
// Drifts up 50px + fades out over 1.5s
// Supports multiple simultaneous popups
// World-to-screen conversion: (worldPos - camera.GlobalPosition) * camera.Zoom + viewportSize/2
```

---

## What Sprint 5 Needs to Implement (Technical)

### 1. Sprite Loading for Plants (Replace _Draw() placeholders)
**Current state:** `PlantData.GrowthSprites` (Texture2D[]) exists but is always null. GardenGrid._Draw() uses `DrawCircle`/`DrawLine` colored shapes for Seed/Sprout/Growing/Blooming stages.
**Needed:**
- Art: 4 growth sprites per plant × 20 plants = 80 sprites (likely 64x64 or 128x128)
- Load sprites into PlantData.GrowthSprites (either via static registry or .tres Resources)
- Modify `GardenGrid.DrawPlantPlaceholder()` to use `DrawTexture()` when GrowthSprites is available, fall back to _Draw() shapes when null
- Sprites need to be centered in the 128px tile

### 2. Sprite Loading for Insects (Replace _Draw() placeholders)
**Current state:** `InsectData.GardenSprite` (SpriteFrames) exists but is always null. `Insect._Draw()` uses colored circles + movement-specific details.
**Needed:**
- Art: 25 insect garden sprites (animated SpriteFrames, 2-4 frames idle, ~32x48)
- Load SpriteFrames into InsectData.GardenSprite
- Modify Insect to use AnimatedSprite2D child when GardenSprite is available, fall back to _Draw() when null
- Existing body size ~6px radius → new sprites ~16-24px

### 3. Journal Illustrations & Silhouettes
**Current state:** `InsectData.JournalIllustration` (Texture2D) and `JournalSilhouette` (Texture2D) are null. JournalUI uses ColorRect for portrait.
**Needed:**
- Art: 25 journal illustrations (~256x256) + 25 silhouettes (~256x256, grey)
- Load into InsectData
- Modify JournalUI detail view to show TextureRect instead of ColorRect
- Grid cells could show small thumbnail of illustration or silhouette

### 4. Localization System (EN + FR)
**Current state:** All text is hardcoded in English:
- JournalText and HintText in InsectRegistry (25 species × 2 strings = 50 strings)
- DisplayName in InsectRegistry (25 strings) and PlantRegistry (20 strings)
- UI labels ("Field Journal", "New Species Discovered!", "Nectar:", "PHOTO MODE", etc.)
- Zone names in ZoneManager/ZoneSelector ("Starter Garden", "Meadow", "Pond Edge")
**Needed:**
- CSV-based localization (Godot's built-in TranslationServer or custom CSV loader)
- Create `en.csv` and `fr.csv` in `localization/` folder
- Replace hardcoded strings with `Tr("key")` or equivalent
- Decide: translate InsectRegistry/PlantRegistry display names, or keep them in English?
- GDD specifies: "English + French (UI + journal text only, no dialogue)"

### 5. Balancing Spawn Rates & Economy
**Current state:** All 25 insects have spawn weights, visit durations, and movement speeds. Plants have seed costs (5-75) and nectar yields (3-10).
**Needed:**
- Playtest and adjust spawn weights for good pacing
- Verify required plants chains work (e.g., Luna Moth needs night_jasmine + white_birch)
- Check economy flow: starting 25 nectar → buy Starter seeds → harvest → unlock Meadow → etc.
- May need to tweak: SpawnCheckInterval, quiet tick probability (currently 40%), visit durations

### 6. Discovery Hints for All Species
**Current state:** All 25 insects have HintText populated in InsectRegistry. These are shown in JournalUI for all species (including undiscovered).
**Needed:**
- Review hint text quality — should be vague enough to be a hint, specific enough to be useful
- Translate hints to French
- Consider: should hints only appear after reaching certain journal milestones? (GDD mentions "vague clues appear when you unlock adjacent species or reach certain journal milestones")

---

## Patterns to Follow (Established in Sprints 1-4)

- All cross-system communication via `EventBus.Publish<T>()` / `Subscribe<T>()`
- All subscribers unsubscribe in `_ExitTree()`
- Store `Action<T>` delegates as fields for proper unsubscription
- Use `TimeManager.Instance.SpeedMultiplier` for game-time scaling
- Use `_Draw()` for placeholder visuals (KEEP FALLBACK when no sprite loaded)
- Use `_UnhandledInput()` for game-world clicks (prevents UI click-through)
- UI lives on `CanvasLayer layer=10` (above CanvasModulate)
- Programmatic UI construction (like JournalUI) — no .tscn for complex UI
- Static registries for data (InsectRegistry, PlantRegistry pattern)
- GameState enum manages modes: `Playing, Paused, PhotoMode, Journal`
- Keyboard shortcuts centralized in HUD._UnhandledInput: C (photo), J (journal), F1 (debug unlock), Escape (back)
- Full variable names, no abbreviations (except i/j loops, x/y coords)
- `CanProcess()` guards on EventBus handlers when zones can be disabled

---

## File System (Current)
```
project-flutter/src/
├── scenes/
│   ├── main/
│   │   ├── Main.tscn
│   │   └── GameWorld.cs
│   ├── garden/
│   │   ├── Garden.tscn
│   │   ├── GardenGrid.cs          ← needs sprite rendering for plants
│   │   ├── GardenCamera.cs
│   │   ├── DayNightVisuals.cs
│   │   └── HarvestPopup.cs
│   ├── insects/
│   │   ├── Insect.tscn
│   │   └── Insect.cs              ← needs AnimatedSprite2D for garden sprites
│   ├── photography/
│   │   ├── PhotoFocusController.cs
│   │   ├── ScreenFlash.cs
│   │   └── StarRatingPopup.cs
│   ├── journal/
│   │   ├── JournalUI.cs           ← needs TextureRect for illustrations/silhouettes
│   │   └── DiscoveryNotification.cs ← needs localized text
│   └── ui/
│       ├── HUD.tscn
│       ├── HUD.cs                 ← needs localized text
│       ├── SeedToolbar.cs         ← needs localized plant names
│       └── ZoneSelector.cs        ← needs localized zone names
├── scripts/
│   ├── autoload/
│   │   ├── GameManager.cs
│   │   ├── TimeManager.cs
│   │   ├── JournalManager.cs
│   │   ├── ZoneManager.cs
│   │   ├── EventBus.cs
│   │   └── Events.cs
│   ├── data/
│   │   ├── PlantData.cs           ← has GrowthSprites field (Texture2D[], currently null)
│   │   ├── InsectData.cs          ← has GardenSprite, JournalIllustration, JournalSilhouette fields (currently null)
│   │   ├── PlantRegistry.cs       ← 20 plants fully defined (static C#)
│   │   ├── InsectRegistry.cs      ← 25 insects fully defined (static C#)
│   │   ├── ZoneType.cs            ← enum { Starter, Meadow, Pond }
│   │   ├── MovementPattern.cs     ← enum { Hover, Flutter, Crawl, Erratic }
│   │   └── CellState.cs
│   └── systems/
│       ├── SpawnSystem.cs
│       ├── IMovementBehavior.cs
│       ├── HoverBehavior.cs
│       ├── FlutterBehavior.cs
│       ├── CrawlBehavior.cs
│       └── ErraticBehavior.cs
├── art/                            ← currently empty, Sprint 5 will populate
│   ├── plants/                     ← 80 growth sprites (4 × 20)
│   ├── insects/                    ← 25 garden sprites + 25 journal illustrations + 25 silhouettes
│   ├── tiles/
│   ├── ui/
│   └── backgrounds/
├── audio/                          ← currently empty
│   ├── music/
│   ├── sfx/
│   └── insects/
└── localization/                   ← to be created
    ├── en.csv
    └── fr.csv
```

### Files That Will Need Changes in Sprint 5
- **GardenGrid.cs** — `DrawPlantPlaceholder()`: draw sprites when GrowthSprites available
- **Insect.cs** — `_Draw()`: use AnimatedSprite2D when GardenSprite available
- **JournalUI.cs** — Detail view: TextureRect for illustrations; localized strings
- **DiscoveryNotification.cs** — Localized "New Species Discovered!" text
- **HUD.cs** — Localized labels
- **SeedToolbar.cs** — Localized plant names
- **ZoneSelector.cs** — Localized zone names
- **InsectRegistry.cs / PlantRegistry.cs** — May migrate to .tres Resources, or add localization keys

### New Files Expected in Sprint 5
- **localization/en.csv** — English strings
- **localization/fr.csv** — French strings
- **art/plants/*.png** — 80 plant growth sprites
- **art/insects/garden/*.tres** — 25 SpriteFrames resources
- **art/insects/journal/*.png** — 25 illustrations + 25 silhouettes
- Possibly a **LocalizationHelper.cs** or use Godot's built-in TranslationServer

---

## Key Architectural Questions for Sprint 5

1. **Sprite fallback strategy**: When replacing _Draw() with real sprites, should we keep the _Draw() code as fallback (check if sprite is null)? This allows incremental art integration.

2. **Resource loading approach**: Should plant/insect sprites be:
   - Loaded directly in static registries via `GD.Load<Texture2D>("res://art/plants/lavender_seed.png")`?
   - Stored in .tres Resource files alongside data?
   - Referenced by convention (e.g., `res://art/plants/{id}_stage{n}.png`)?

3. **Localization method**: Should we use:
   - Godot's built-in TranslationServer + .csv import?
   - Custom CSV loader for more control?
   - Keep static registry text in English, add `Tr()` wrapper for UI-only strings?

4. **Migrate to .tres Resources?** GDD says "will migrate to Resource files (.tres) in Sprint 5". Currently using static C# classes. Migration means:
   - Creating .tres files for each plant/insect
   - Loading them at runtime instead of hardcoded data
   - Benefit: Godot editor integration for art previews
   - Cost: more files, more setup, no code-completion for data

5. **Art pipeline**: What format/resolution for sprites?
   - Plants: 128×128 PNG per growth stage? Or smaller and scaled?
   - Insects: SpriteFrames with 2-4 frames at what resolution?
   - Journal illustrations: 256×256 PNG?

6. **Economy balancing**: How to systematically test?
   - Debug keys for nectar? (F2 = +100 nectar?)
   - Speed x10 already exists
   - Log spawn events to track insect variety?
