# Project Flutter — Sprint 3 Deep Search Context

## Project Overview
Cozy top-down garden game (Godot 4.5, C# 12 / .NET 8). Grow plants → attract insects → photograph them → fill a field journal. No GDScript, C# only.

## Sprint 3 Goal: Photography & Journal
From GDD:
- Photo mode toggle
- Concentric circle focus mechanic (click & hold on insect)
- Quality rating (★☆☆ to ★★★ based on how centered the insect was)
- Shutter sound + flash effect
- Journal UI (grid of entries, silhouettes for undiscovered, portraits for discovered)
- Journal entry detail view (illustration, name, fun fact, star rating)
- Discovery tracking (JournalManager autoload already exists)
- New discovery notification/fanfare
- **Deliverable:** Full photograph → journal → collection loop working

## Current Architecture

### Scene Tree
```
Main (Node2D)
├── Garden (Node2D)
│   ├── DayNightVisuals (CanvasModulate) — color tweens per time period
│   ├── GardenGrid (Node2D) — 4x4 grid, 128px tiles, _Draw() placeholders
│   │   ├── GroundLayer (TileMapLayer)
│   │   ├── HoverPreview (Sprite2D)
│   │   └── Plants (Node2D)
│   ├── InsectContainer (Node2D) — spawned insects live here
│   └── SpawnSystem (Node) — timer-based spawn on blooming plants
├── GardenCamera (Camera2D) — WASD pan, mouse wheel zoom
└── UILayer (CanvasLayer, layer=10)
    └── HUD (Control) — time display, nectar counter, speed button
```

### Autoloads (Godot Node singletons)
- **GameManager** — `static Instance`, GameState enum (Playing/Paused/PhotoMode), CurrentZone, Nectar
- **TimeManager** — `static Instance`, day/night cycle, SpeedMultiplier, periods (night/dawn/morning/noon/golden_hour/dusk)
- **JournalManager** — `static Instance`, Dictionary<string,int> discovered species + star ratings

### EventBus (Pure static C# class, NOT an autoload)
```csharp
public static class EventBus
{
    public static void Subscribe<T>(Action<T> callback);
    public static void Unsubscribe<T>(Action<T> callback);
    public static void Publish<T>(T evt);
}
```

### Event Records (Events.cs)
```csharp
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
public record InsectDepartedEvent(string InsectId, Vector2 Position);
public record InsectClickedEvent(string InsectId, Node2D Insect, Vector2 Position);
```

### Insect.cs (Area2D, the photo target)
```csharp
public partial class Insect : Area2D
{
    public enum InsectState { Arriving, Visiting, Departing, Freed }

    private InsectData _data;
    private IMovementBehavior _movement;
    private InsectState _state = InsectState.Arriving;
    private float _visitTimeRemaining;
    private float _time;
    private Vector2 _plantAnchor;
    private Vector2I _hostCellPos;

    public InsectData Data => _data;
    public InsectState CurrentState => _state;

    // Initialize is called before AddChild by SpawnSystem
    public void Initialize(InsectData data, Vector2 plantPosition, Vector2 entryPosition, Vector2I hostCellPos);

    // Lifecycle: Arriving (tween fly-in 1.5s) → Visiting (movement behavior) → Departing (tween fly-out 1.2s) → QueueFree
    // Subscribes to TimeOfDayChangedEvent (day insects leave at night) and PlantRemovedEvent (only for own host cell)
    // Uses _Draw() for placeholder visuals: colored circles per MovementPattern + idle bob
    // CollisionShape2D "ClickArea" with CircleShape2D radius 14px for click detection
    // InputPickable = true (Area2D default) — can receive InputEvent signal for clicks
}
```

### InsectData.cs (Resource)
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
    [Export] public float VisitDurationMin { get; set; } = 60.0f;
    [Export] public float VisitDurationMax { get; set; } = 180.0f;
    [Export] public string PhotoDifficulty { get; set; }
    [Export] public MovementPattern MovementPattern { get; set; } = MovementPattern.Hover;
    [Export] public float MovementSpeed { get; set; } = 30.0f;
    [Export] public float PauseFrequency { get; set; } = 0.4f;
    [Export] public float PauseDuration { get; set; } = 2.0f;
    [Export] public SpriteFrames GardenSprite { get; set; }
    [Export] public Texture2D JournalIllustration { get; set; }
    [Export] public Texture2D JournalSilhouette { get; set; }
    [Export] public string JournalText { get; set; }
    [Export] public string HintText { get; set; }
    [Export] public AudioStream AmbientSound { get; set; }
}
```

### GameManager.cs
```csharp
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    public enum GameState { Playing, Paused, PhotoMode }
    public GameState CurrentState { get; private set; } = GameState.Playing;
    public ZoneType CurrentZone { get; set; } = ZoneType.Starter;
    public int Nectar { get; private set; }
    public void ChangeState(GameState newState);  // publishes GameStateChangedEvent
    public void AddNectar(int amount);             // publishes NectarChangedEvent
    public bool SpendNectar(int amount);           // publishes NectarChangedEvent
}
```

### JournalManager.cs
```csharp
public partial class JournalManager : Node
{
    public static JournalManager Instance { get; private set; }
    private readonly Dictionary<string, int> _discoveredSpecies = new();
    public void DiscoverSpecies(string insectId, int starRating);  // publishes SpeciesDiscoveredEvent or JournalUpdatedEvent
    public bool IsDiscovered(string insectId);
    public int GetStarRating(string insectId);
    public int GetDiscoveredCount();
}
```

### TimeManager.cs
```csharp
public partial class TimeManager : Node
{
    public static TimeManager Instance { get; private set; }
    public const float DayCycleDuration = 300.0f;
    public float SpeedMultiplier { get; private set; } = 1.0f;
    public float CurrentTimeNormalized { get; private set; } = 0.25f;  // 0-1, starts at 6:00
    public string CurrentPeriod { get; private set; }                   // "night"/"dawn"/"morning"/"noon"/"golden_hour"/"dusk"
    public bool Paused { get; set; }
    public void SetSpeed(float multiplier);
    public bool IsDaytime();   // dawn, morning, noon, golden_hour
    public bool IsNighttime(); // dusk, night
    // Publishes HourPassedEvent and TimeOfDayChangedEvent
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

### GardenGrid.cs (key methods)
```csharp
public partial class GardenGrid : Node2D
{
    [Export] public Vector2I GridSize { get; set; } = new(4, 4);
    [Export] public int TileSize { get; set; } = 128;
    // Uses _Draw() for all placeholder visuals (grass bg, soil tiles, plant shapes, hover highlight)
    // _UnhandledInput: left click = plant/water/grow/harvest, right click = remove
    // Publishes PlantPlantedEvent, PlantBloomingEvent, PlantHarvestedEvent, PlantRemovedEvent
    public Dictionary<Vector2I, CellState> GetCells();
    public CellState GetCell(Vector2I pos);
    public Vector2 GridToWorld(Vector2I gridPos);  // returns center of tile in local coords
}
```

### SpawnSystem.cs (key behavior)
```csharp
public partial class SpawnSystem : Node
{
    [Export] public int MaxInsectsPerZone { get; set; } = 10;
    [Export] public float SpawnCheckInterval { get; set; } = 5.0f;
    // _Process: timer-based, scaled by TimeManager.SpeedMultiplier
    // TrySpawn: finds blooming cells with available slots, 40% quiet ticks, filters by time/zone/required plants, weighted random select
    // SpawnInsect: instantiates Insect.tscn, calls Initialize(), registers slot via cell.OccupySlot(), auto-vacate via TreeExiting
    // Currently uses 4 hardcoded test InsectData (honeybee, cabbage_white, ladybug, sphinx_moth)
}
```

### Movement Behaviors (IMovementBehavior)
```csharp
public interface IMovementBehavior
{
    Vector2 CalculatePosition(float delta);
    void Reset(Vector2 anchor);
}
// Implementations: HoverBehavior (FastNoiseLite), FlutterBehavior (sine wave path),
//                  CrawlBehavior (elliptical), ErraticBehavior (random + tether)
// Factory: MovementBehaviorFactory.Create(MovementPattern, InsectData, RNG)
```

### Enums
```csharp
public enum ZoneType { Starter, Meadow, Pond }
public enum MovementPattern { Hover, Flutter, Crawl, Erratic }
```

### HUD.cs
```csharp
public partial class HUD : Control
{
    // Subscribes to HourPassedEvent, TimeOfDayChangedEvent, NectarChangedEvent
    // Unsubscribes in _ExitTree
    // TimeLabel (top-right): "HH:MM (period)"
    // NectarLabel (top-left): "Nectar: N"
    // SpeedButton: cycles x1 → x2 → x3 → x10
}
```

### GDD Photography Section (4.4)
- Switch to photo mode (toggle or hold key)
- Click and hold on insect → concentric circle closes (1-2 sec)
- Insect continues moving during focus
- Circle closes → shutter sound + white flash + insect freezes momentarily
- Quality: ★☆☆ (edge), ★★☆ (reasonable), ★★★ (perfect center)
- First photo = new journal entry
- Can re-photograph for better rating
- Photo difficulty varies by species behavior

### GDD Journal Section (4.5)
- Grid of entries: discovered = portrait, undiscovered = grey silhouette
- Each entry: species name, illustration, star rating, flavor text, habitat hint, first discovery date
- Discovery hints for undiscovered species
- Completion tracker: "17/25 species documented"
- 100% completion = special reward

### Key Patterns to Follow
- All cross-system communication via EventBus.Publish<T>() / Subscribe<T>()
- All subscribers unsubscribe in _ExitTree()
- Store Action<T> delegates as fields for proper unsubscription
- Use TimeManager.Instance.SpeedMultiplier for game-time scaling
- Use _Draw() for placeholder visuals (no real art yet)
- Use _UnhandledInput() for game-world clicks (prevents UI click-through)
- UI lives on CanvasLayer layer=10 (above CanvasModulate)
- GameManager.Instance.ChangeState() to switch to PhotoMode
