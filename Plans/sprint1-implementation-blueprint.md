# Building a cozy garden game in Godot 4 (C#): Sprint 1 technical blueprint

**The optimal architecture mirrors Stardew Valley's proven hybrid approach**: use TileMapLayer for efficient ground rendering and coordinate math, a Dictionary for per-cell game state, individual scenes for plants, and custom Resources (.tres) for all plant/insect data definitions. This report covers every Sprint 1 system in depth — grid, cursor, plants, day/night, project structure, camera, and key references — with production-ready C# patterns throughout.

The core insight driving every recommendation below is **separation of visual rendering from game state**. TileMapLayer renders soil; a `Dictionary<Vector2I, CellState>` tracks what's planted where; Plant.tscn scenes handle their own growth animations. This three-layer split keeps each system simple, testable, and extensible. Sprint 1 needs only three autoloads (GameManager, EventBus, SaveManager), with TimeManager living as a regular node until Sprint 2 demands global clock access.

**C#-specific note**: Use the **.NET version** of Godot 4 (not the standard build). Download from godotengine.org → .NET tab. Requires .NET 8 SDK installed. The `.csproj` is auto-generated when you create your first C# script. Use `[GlobalClass]` attribute on custom Resources so they appear in the Godot editor's Create Resource menu.

---

## 1. The grid: TileMapLayer for rendering, Dictionary for state

Three approaches exist for a garden grid — TileMapLayer alone, a pure custom Node2D grid, or a hybrid. **The hybrid wins decisively**, and it's exactly how Stardew Valley works. Stardew uses the xTile engine for visual tile layers but tracks crops, soil moisture, and growth as separate data objects indexed by grid coordinates. The tilemap is purely visual; logic lives in code.

**Why not TileMapLayer alone?** TileSet custom data layers are defined per tile *type*, not per placed cell. Every cell using the same tile shares identical custom data. This makes TileMapLayer unsuitable as the sole state tracker for per-cell data like "is this plot watered?" or "growth stage 3 of 4."

**Why not a pure custom grid?** You lose TileMapLayer's built-in `LocalToMap()` and `MapToLocal()` — functions that correctly handle camera zoom, offset, and coordinate transforms with zero manual math. You also lose efficient batched rendering and editor painting support.

### Core grid architecture

```csharp
// CellState.cs
public class CellState
{
    public enum State { Empty, Tilled, Planted, Watered, Growing, Blooming }

    public State CurrentState { get; set; } = State.Empty;
    public string PlantType { get; set; } = "";
    public int GrowthStage { get; set; } = 0;
    public bool IsWatered { get; set; } = false;
    public Node2D PlantNode { get; set; } = null;

    public bool CanPlant() => CurrentState == State.Tilled || CurrentState == State.Watered;
}
```

```csharp
// GardenGrid.cs — Main grid manager
using Godot;
using System.Collections.Generic;

public partial class GardenGrid : Node2D
{
    [Export] public Vector2I GridSize { get; set; } = new(20, 20);

    private TileMapLayer _groundLayer;
    private Sprite2D _hoverPreview;
    private Dictionary<Vector2I, CellState> _cells = new();

    public override void _Ready()
    {
        _groundLayer = GetNode<TileMapLayer>("GroundLayer");
        _hoverPreview = GetNode<Sprite2D>("HoverPreview");
        _hoverPreview.Modulate = new Color(1, 1, 1, 0.5f);

        for (int x = 0; x < GridSize.X; x++)
            for (int y = 0; y < GridSize.Y; y++)
                _cells[new Vector2I(x, y)] = new CellState();
    }
}
```

### Mouse-to-grid conversion (the critical function)

Always use `GetGlobalMousePosition()` (accounts for camera transforms), then convert through the TileMapLayer:

```csharp
private Vector2I? MouseToGrid()
{
    var mousePos = GetGlobalMousePosition();
    var gridPos = _groundLayer.LocalToMap(_groundLayer.ToLocal(mousePos));

    if (gridPos.X >= 0 && gridPos.X < GridSize.X
        && gridPos.Y >= 0 && gridPos.Y < GridSize.Y)
        return gridPos;

    return null;
}
```

**Key pitfall**: never use `GetViewport().GetMousePosition()` for world-space interactions — it gives screen coordinates, not world coordinates. Also remember `LocalToMap()` expects coordinates in TileMapLayer's local space — always call `ToLocal()` first if the TileMapLayer isn't at origin.

---

## 2. Cursor interaction without a player character

### Hardware cursor beats software cursor

`Input.SetCustomMouseCursor()` (hardware cursor) renders through the OS compositor with zero latency. A `Sprite2D` following mouse position always lags by at least one frame:

```csharp
// ToolManager.cs — Autoload
using Godot;
using System.Collections.Generic;

public partial class ToolManager : Node
{
    public enum Tool { Trowel, WateringCan, Camera }

    [Signal] public delegate void ToolChangedEventHandler(int newTool);

    public static ToolManager Instance { get; private set; }

    private static readonly Dictionary<Tool, string> CursorPaths = new()
    {
        { Tool.Trowel, "res://assets/cursors/trowel.png" },
        { Tool.WateringCan, "res://assets/cursors/watering_can.png" },
        { Tool.Camera, "res://assets/cursors/camera.png" },
    };

    private Tool _currentTool = Tool.Trowel;
    public Tool CurrentTool
    {
        get => _currentTool;
        set
        {
            if (_currentTool == value) return;
            _currentTool = value;
            var texture = GD.Load<Texture2D>(CursorPaths[_currentTool]);
            Input.SetCustomMouseCursor(texture, Input.CursorShape.Arrow, new Vector2(16, 16));
            EmitSignal(SignalName.ToolChanged, (int)_currentTool);
        }
    }

    public override void _Ready() => Instance = this;
}
```

Cursor images must be **≤256×256 pixels** (128×128 recommended). The hotspot offset marks the cursor's "active point" — set a trowel's tip, not its center.

### Hover preview: ghost Sprite2D snapped to grid

```csharp
public override void _Process(double delta)
{
    var gridPos = MouseToGrid();
    if (gridPos is Vector2I pos)
    {
        _hoverPreview.Visible = true;
        _hoverPreview.Position = _groundLayer.MapToLocal(pos);

        var cell = _cells.GetValueOrDefault(pos);
        _hoverPreview.Modulate = CanUseTool(cell)
            ? new Color(0.5f, 1f, 0.5f, 0.5f)   // Green = valid
            : new Color(1f, 0.5f, 0.5f, 0.5f);   // Red = invalid
    }
    else
    {
        _hoverPreview.Visible = false;
    }
}
```

### Click handling: _UnhandledInput is mandatory

**Use `_UnhandledInput()`, never `_Input()`**. Toolbar buttons consume clicks via `_GuiInput()` — using `_Input()` means clicking a toolbar button ALSO triggers a garden action behind it:

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
    {
        var gridPos = MouseToGrid();
        if (gridPos is not Vector2I pos) return;

        switch (mouseBtn.ButtonIndex)
        {
            case MouseButton.Left:
                HandlePrimaryAction(pos);
                break;
            case MouseButton.Right:
                HandleSecondaryAction(pos);
                break;
        }
    }
}
```

Convention: **left-click for primary action** (place, water, photograph), **right-click for secondary** (remove, dig up, cancel).

---

## 3. Plant growth: Resources define data, AnimationPlayer drives visuals

### Custom Resources for plant definitions

```csharp
// PlantData.cs
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PlantData : Resource
{
    [Export] public string PlantName { get; set; } = "Unknown";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Array<float> GrowthTimes { get; set; } = new();  // hours per stage
    [Export] public float WaterNeeds { get; set; } = 1.0f;
    [Export] public Array<Texture2D> StageTextures { get; set; } = new();
    [Export] public float InsectAttraction { get; set; } = 0.0f;
    [Export] public bool AttractsButterflies { get; set; } = false;
    [Export] public bool AttractsBees { get; set; } = false;
    [Export] public bool IsPerennial { get; set; } = false;
}
```

**Critical**: `[GlobalClass]` is required for C# Resources to appear in the editor's "Create New Resource" dialog. The class must be `partial`. **Shared-reference trap**: all nodes referencing the same `.tres` share one instance. Call `.Duplicate()` for per-instance modifications.

### Loading all plant data from a directory

```csharp
public Dictionary<string, PlantData> AllPlants { get; } = new();

public override void _Ready()
{
    var dir = DirAccess.Open("res://resources/plant_data/");
    dir.ListDirBegin();
    var fileName = dir.GetNext();

    while (fileName != "")
    {
        var clean = fileName.Replace(".remap", "");  // Handle export builds
        if (!dir.CurrentIsDir() && clean.EndsWith(".tres"))
        {
            var res = GD.Load<PlantData>($"res://resources/plant_data/{clean}");
            if (res != null)
                AllPlants[res.PlantName] = res;
        }
        fileName = dir.GetNext();
    }
}
```

### Plant scene structure

```
Plant (Node2D) — Plant.cs
├── Sprite2D — current stage texture
├── AnimationPlayer — transition animations (recommended over AnimatedSprite2D)
├── WaterIndicator (Sprite2D) — droplet icon
├── GrowthParticles (GpuParticles2D) — sparkle VFX
├── BloomParticles (GpuParticles2D) — petal VFX
├── InteractionArea (Area2D + CollisionShape2D)
├── AudioStreamPlayer2D — growth/harvest sounds
└── PointLight2D — subtle glow when blooming (night)
```

**AnimationPlayer** is recommended over AnimatedSprite2D because it can orchestrate texture changes, scale tweens, particle bursts, and sound effects on a single timeline — creating the polished "cozy" feel.

### Growth logic tied to game time

```csharp
// Plant.cs
using Godot;
using System.Collections.Generic;

public partial class Plant : Node2D
{
    [Export] public PlantData PlantData { get; set; }

    public enum GrowthStage { Seed, Sprout, Growing, Blooming }

    public float GrowthProgressHours { get; set; } = 0.0f;
    public float WaterLevel { get; set; } = 0.0f;
    public GrowthStage CurrentStage { get; private set; } = GrowthStage.Seed;

    private List<float> _stageThresholds = new();

    public override void _Ready()
    {
        float cumulative = 0f;
        foreach (var time in PlantData.GrowthTimes)
        {
            cumulative += time;
            _stageThresholds.Add(cumulative);
        }

        // Connect to TimeManager signal
        var timeManager = GetNode<TimeManager>("/root/Main/TimeManager");
        timeManager.HourPassed += OnHourPassed;
    }

    private void OnHourPassed(int hour)
    {
        if (CurrentStage == GrowthStage.Blooming) return;
        if (WaterLevel <= 0) return;

        GrowthProgressHours += 1.0f;
        WaterLevel -= PlantData.WaterNeeds / 24.0f;
        CheckStageAdvancement();
    }

    private void CheckStageAdvancement()
    {
        for (int i = _stageThresholds.Count - 1; i >= 0; i--)
        {
            if (GrowthProgressHours >= _stageThresholds[i])
            {
                var newStage = (GrowthStage)(i + 1);
                if (newStage != CurrentStage)
                {
                    CurrentStage = newStage;
                    UpdateVisuals();
                    if (CurrentStage == GrowthStage.Blooming)
                        EventBus.Instance.EmitPlantBlooming(this);
                }
                break;
            }
        }
    }

    private void UpdateVisuals()
    {
        var sprite = GetNode<Sprite2D>("Sprite2D");
        int index = (int)CurrentStage;
        if (index < PlantData.StageTextures.Count)
            sprite.Texture = PlantData.StageTextures[index];

        GetNode<AnimationPlayer>("AnimationPlayer").Play("stage_transition");
    }
}
```

Store `GrowthProgressHours` (float) in save data, not just the stage enum. This preserves partial progress across save/load.

### Signal patterns (C# in Godot)

Two approaches:

```csharp
// 1. Godot signals — visible in editor, cross-language compatible
[Signal] public delegate void GrowthStageChangedEventHandler(int newStage);
EmitSignal(SignalName.GrowthStageChanged, (int)CurrentStage);
plant.GrowthStageChanged += OnPlantGrowthChanged;

// 2. Pure C# events — better performance, type-safe
public event Action<Plant> OnBlooming;
OnBlooming?.Invoke(this);
plant.OnBlooming += HandleBlooming;
```

**Use Godot `[Signal]` for EventBus** (cross-system), **C# `event` for local** parent-child communication.

---

## 4. Day/night cycle: CanvasModulate with custom time delta

### CanvasModulate is the right visual approach

CanvasModulate applies multiplicative color on the entire canvas — one node, zero performance cost, works natively with PointLight2D. Place UI on a separate CanvasLayer so it isn't tinted.

### Cozy color palette

```csharp
private static readonly Dictionary<StringName, Color> PeriodColors = new()
{
    { "dawn",        new Color(0.94f, 0.78f, 0.73f) },  // Warm pink-peach
    { "morning",     new Color(1.0f, 0.96f, 0.90f) },   // Soft cream
    { "noon",        new Color(1.0f, 1.0f, 1.0f) },      // Full brightness
    { "golden_hour", new Color(1.0f, 0.90f, 0.70f) },   // Warm golden amber
    { "dusk",        new Color(0.85f, 0.65f, 0.70f) },   // Dusty rose
    { "night",       new Color(0.30f, 0.35f, 0.55f) },   // Soft indigo
};
```

Don't make night too dark — multiplicative blending crushes dark sprites.

### Smooth transitions

```csharp
// DayNightVisuals.cs
public partial class DayNightVisuals : CanvasModulate
{
    private Tween _currentTween;

    private void OnPeriodChanged(StringName period)
    {
        if (!PeriodColors.TryGetValue(period, out var target))
            target = Colors.White;

        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "color", target, 2.0)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }
}
```

### Custom delta beats Engine.TimeScale

`Engine.TimeScale` globally scales everything including UI. Use custom delta multiplication instead:

```csharp
// TimeManager.cs
public partial class TimeManager : Node
{
    [Signal] public delegate void HourPassedEventHandler(int hour);
    [Signal] public delegate void TimeOfDayChangedEventHandler(StringName period);

    public const float DayCycleDuration = 300.0f;  // 5 minutes default

    public float SpeedMultiplier { get; set; } = 1.0f;
    public float CurrentTimeNormalized { get; private set; } = 0.25f; // Start at morning

    private float _accumulatedTime = 0.0f;
    private float _secondsPerGameMinute;
    private int _lastHour = -1;
    private StringName _lastPeriod;

    public override void _Ready()
    {
        _secondsPerGameMinute = DayCycleDuration / (24.0f * 60.0f);
    }

    public override void _Process(double delta)
    {
        _accumulatedTime += (float)delta * SpeedMultiplier;
        while (_accumulatedTime >= _secondsPerGameMinute)
        {
            _accumulatedTime -= _secondsPerGameMinute;
            AdvanceMinute();
        }
    }

    private void AdvanceMinute()
    {
        CurrentTimeNormalized += 1.0f / (24.0f * 60.0f);
        if (CurrentTimeNormalized >= 1.0f) CurrentTimeNormalized -= 1.0f;

        int currentHour = (int)(CurrentTimeNormalized * 24.0f);
        if (currentHour != _lastHour)
        {
            _lastHour = currentHour;
            EmitSignal(SignalName.HourPassed, currentHour);
        }

        var period = GetCurrentPeriod();
        if (period != _lastPeriod)
        {
            _lastPeriod = period;
            EmitSignal(SignalName.TimeOfDayChanged, period);
        }
    }

    public StringName GetCurrentPeriod()
    {
        float hour = CurrentTimeNormalized * 24.0f;
        return hour switch
        {
            < 5.5f  => "night",
            < 7.0f  => "dawn",
            < 10.0f => "morning",
            < 14.0f => "noon",
            < 17.0f => "golden_hour",
            < 19.5f => "dusk",
            _       => "night"
        };
    }
}
```

---

## 5. Three autoloads are enough for Sprint 1

### Project folder structure

```
project_flutter/
├── assets/
│   ├── art/sprites/{plants,insects,garden,ui}/
│   ├── art/tilesets/
│   ├── audio/{music,sfx,ambience}/
│   └── fonts/
├── resources/
│   ├── plant_data/          # .tres per plant type
│   └── insect_data/
├── scenes/
│   ├── main/                # Main.tscn + Main.cs
│   ├── garden/              # GardenGrid.tscn + GardenGrid.cs
│   ├── plants/              # Plant.tscn + Plant.cs
│   ├── ui/                  # HUD, Journal, PauseMenu
│   └── environment/         # DayNightVisuals, GardenCamera
├── scripts/
│   ├── autoload/            # GameManager.cs, EventBus.cs, SaveManager.cs
│   ├── data/                # PlantData.cs, CellState.cs, InsectData.cs
│   └── systems/             # SpawnSystem.cs, NectarEconomy.cs
├── project.godot
└── ProjectFlutter.csproj    # Auto-generated by Godot
```

### EventBus pattern (C# singleton)

```csharp
// EventBus.cs — Autoload
using Godot;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal] public delegate void PlantPlantedEventHandler(Resource plantData, Vector2I gridPos);
    [Signal] public delegate void PlantHarvestedEventHandler(Resource plantData, Vector2I gridPos);
    [Signal] public delegate void PlantBloomingEventHandler(Node2D plant);
    [Signal] public delegate void DayStartedEventHandler(int dayNumber);
    [Signal] public delegate void InsectDiscoveredEventHandler(Resource insectData);
    [Signal] public delegate void PauseToggledEventHandler(bool isPaused);

    public override void _Ready() => Instance = this;

    // Type-safe convenience methods
    public void EmitPlantPlanted(PlantData data, Vector2I pos)
        => EmitSignal(SignalName.PlantPlanted, data, pos);

    public void EmitPlantBlooming(Plant plant)
        => EmitSignal(SignalName.PlantBlooming, plant);
}
```

Usage: `EventBus.Instance.PlantBlooming += OnPlantBlooming;`

### Save system: System.Text.Json

C# advantage — strongly typed serialization with no manual Dictionary construction:

```csharp
// SaveManager.cs
using Godot;
using System.Text.Json;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }
    private const string SavePath = "user://save_game.json";

    public override void _Ready() => Instance = this;

    public void SaveGame(GameSaveData data)
    {
        data.SaveVersion = 1;
        data.Timestamp = System.DateTime.UtcNow.ToString("o");

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public GameSaveData LoadGame()
    {
        if (!FileAccess.FileExists(SavePath)) return null;
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        return JsonSerializer.Deserialize<GameSaveData>(file.GetAsText());
    }
}

public class GameSaveData
{
    public int SaveVersion { get; set; }
    public int DayCount { get; set; }
    public string Timestamp { get; set; }
    public Dictionary<string, CellSaveData> GardenCells { get; set; } = new();
}
```

---

## 6. Camera and viewport settings for hand-drawn art

### Project settings (critical)

```ini
display/window/size/viewport_width = 1920
display/window/size/viewport_height = 1080
display/window/stretch/mode = "canvas_items"    # NOT viewport!
display/window/stretch/aspect = "keep_height"
rendering/textures/canvas_textures/default_texture_filter = 1  # Linear (NOT Nearest)
```

**`canvas_items`** renders at native screen resolution — essential for hand-drawn art. `viewport` mode would upscale and destroy detail. **Linear filtering** preserves smooth gradients. Enable **mipmaps** for textures at varying zoom levels.

### Pannable camera with zoom

```csharp
// GardenCamera.cs
public partial class GardenCamera : Camera2D
{
    [Export] public float PanSpeed { get; set; } = 400.0f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.5f;
    [Export] public float MaxZoom { get; set; } = 3.0f;

    public override void _Ready()
    {
        PositionSmoothingEnabled = true;
        PositionSmoothingSpeed = 8.0f;
    }

    public override void _Process(double delta)
    {
        var dir = Vector2.Zero;
        if (Input.IsActionPressed("camera_left")) dir.X -= 1;
        if (Input.IsActionPressed("camera_right")) dir.X += 1;
        if (Input.IsActionPressed("camera_up")) dir.Y -= 1;
        if (Input.IsActionPressed("camera_down")) dir.Y += 1;

        if (dir != Vector2.Zero)
            Position += dir.Normalized() * PanSpeed * (float)delta / Zoom.X;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn)
        {
            float zoomDelta = mouseBtn.ButtonIndex switch
            {
                MouseButton.WheelUp => ZoomSpeed,
                MouseButton.WheelDown => -ZoomSpeed,
                _ => 0
            };

            if (zoomDelta != 0)
            {
                float newZoom = Mathf.Clamp(Zoom.X + zoomDelta, MinZoom, MaxZoom);
                Zoom = new Vector2(newZoom, newZoom);
            }
        }
    }
}
```

---

## 7. Key references

- **Chickensoft** (chickensoft.games) — Best C#/Godot community, display scaling guide, testing patterns
- **JetBrains Godot C# guide** — Singletons, autoloads, project organization
- **Official C# docs**: `docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/`
- **elliotfontaine/untitled-farming-sim** — Godot 4 farming with crop rotation (GitHub)
- **Kenny-Haworth/Harvest-Moon-2.0** — Feature-complete open-source farming game in Godot

## Conclusion

The three-layer pattern — **TileMapLayer for rendering, Dictionary for state, scenes for entities** — gives you Stardew Valley's proven separation of concerns with C#'s type safety. Key C# advantages: `System.Text.Json` for saves, LINQ for data queries, `Dictionary<Vector2I, CellState>` with generics, pattern matching (`switch` expressions) for clean state logic.

**Two decisions that prevent refactoring pain**: Use `_UnhandledInput()` for all game-world clicks from day one (prevents UI click-through bugs). Store plant growth as cumulative `float` hours, not just a stage enum (makes save/load and time-skip trivial).
