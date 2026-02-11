# Sprint 2 blueprint for Project Flutter's insect system

**Area2D-rooted insect scenes driven by data resources, an enum FSM with strategy-pattern movement, and a timer-based spawn manager modeled on Neko Atsume's passive-attraction loop** form the architecture that best balances solo-dev simplicity with Steam-release robustness. The full system requires roughly 6 new files (Insect.cs, 4 movement behaviors, SpawnSystem.cs) plus the InsectData resource class and EventBus events—well within the 40-hour sprint budget. Every timer and duration in the system scales with `TimeManager.TimeScale`, so fast-forward works automatically without per-system wiring.

---

## 1. The insect scene: Area2D root, data-injected at spawn

**Area2D is the correct root node.** CharacterBody2D adds physics collision insects don't need (they fly over terrain). Node2D lacks built-in input detection. Area2D provides both `InputEvent` signal for Sprint 3's photography clicks and lightweight overlap detection—with zero physics overhead.

**Node hierarchy for `Insect.tscn`:**

```
Insect (Area2D) [Script: Insect.cs]
├── AnimatedSprite2D   — "Sprite", plays fly/idle/land from InsectData.GardenSprite
├── CollisionShape2D   — "ClickArea", CircleShape2D radius ~14px (forgiving click target)
├── AudioStreamPlayer2D — "AmbientSound", buzzes/chirps with distance attenuation
└── GPUParticles2D     — "Trail", optional pollen dust (polish pass)
```

**Connect the scene to its data via an `Initialize()` method**, not `[Export]` fields. Insects are spawned programmatically—never placed in the editor—so `[Export]` provides no benefit. `Initialize()` gives a type-safe contract the compiler enforces:

```csharp
public partial class Insect : Area2D
{
    private InsectData _data;
    private AnimatedSprite2D _sprite;
    private IMovementBehavior _movement;
    private InsectState _state = InsectState.Arriving;
    private double _visitTimeRemaining;

    public InsectData Data => _data;
    public InsectState CurrentState => _state;

    public void Initialize(InsectData data, Vector2 plantPosition, Vector2 entryPosition)
    {
        _data = data;
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _sprite.SpriteFrames = data.GardenSprite;  // direct reference assignment, no copy
        _sprite.Play("fly");

        if (data.AmbientSound != null)
        {
            var audio = GetNode<AudioStreamPlayer2D>("AmbientSound");
            audio.Stream = data.AmbientSound;
            audio.Play();
        }

        var rng = new RandomNumberGenerator();
        rng.Randomize();
        _movement = MovementBehaviorFactory.Create(data.MovementPattern, data, rng);
        _movement.Reset(plantPosition);

        GlobalPosition = entryPosition;
    }
}
```

Call `Initialize()` **before** `AddChild()` so data is ready when `_Ready()` fires. SpriteFrames assigned this way are shared references—**30 butterflies of the same species share one SpriteFrames and one GPU texture atlas**, so memory scales with species count, not instance count.

For click detection, set `InputPickable = true` (default on Area2D) and connect the built-in `InputEvent` signal. The CollisionShape2D defines the clickable area—use a circle slightly larger than the sprite for forgiving taps:

```csharp
InputEvent += (viewport, @event, shapeIdx) =>
{
    if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
    {
        EventBus.Publish(new InsectClickedEvent { Data = _data, Insect = this, Position = GlobalPosition });
        GetViewport().SetInputAsHandled();
    }
};
```

---

## 2. Movement that feels alive: enum FSM plus strategy-pattern behaviors

Two architectural decisions define the movement system. First, **use an enum-based FSM** (not node-based) for the insect's lifecycle states. GDQuest's Nathan Lovato explicitly recommends this for simple NPCs: "Using nodes and separate scripts fragments the code too much for my taste." Four to five states (Arriving, Moving, Pausing, Departing, Freed) fit cleanly in a single switch:

```csharp
public enum InsectState { Arriving, Moving, Pausing, Departing, Freed }

public override void _Process(double delta)
{
    float dt = (float)(delta * TimeManager.Instance.TimeScale);
    switch (_state)
    {
        case InsectState.Arriving:  ProcessArriving(dt); break;
        case InsectState.Moving:    ProcessMoving(dt); break;
        case InsectState.Pausing:   ProcessPausing(dt); break;
        case InsectState.Departing: ProcessDeparting(dt); break;
    }
    // Subtle idle bob — always active, sells "alive"
    _sprite.Position = new Vector2(0, Mathf.Sin(_time * 3f) * 1.5f);
}
```

Second, **use the Strategy pattern via an `IMovementBehavior` interface** for the four movement types. This decouples movement from the insect scene, so you can reuse "hover" for both bees and hummingbirds later without subclassing:

```csharp
public interface IMovementBehavior
{
    Vector2 CalculateMovement(float delta);
    void Reset(Vector2 origin);
}

public static class MovementBehaviorFactory
{
    public static IMovementBehavior Create(MovementPattern pattern, InsectData data, RandomNumberGenerator rng)
        => pattern switch
        {
            MovementPattern.Hover   => new HoverBehavior(data, rng),
            MovementPattern.Flutter => new FlutterBehavior(data, rng),
            MovementPattern.Crawl   => new CrawlBehavior(data, rng),
            MovementPattern.Erratic => new ErraticBehavior(data, rng),
            _ => new HoverBehavior(data, rng),
        };
}
```

**The four movement implementations**, each producing organic motion through different techniques:

**Hover (bee)** uses Godot 4's built-in `FastNoiseLite` for smooth micro-offsets around a fixed anchor. Sampling noise at two offset time values (X at `t`, Y at `t + 1000`) prevents correlated axes—the bee drifts organically rather than vibrating diagonally:

```csharp
public class HoverBehavior : IMovementBehavior
{
    private readonly FastNoiseLite _noise;
    private Vector2 _anchor;
    private float _time;
    private float _hoverRadius = 8f;

    public HoverBehavior(InsectData data, RandomNumberGenerator rng)
    {
        _noise = new FastNoiseLite { Seed = rng.RandiRange(0, 99999),
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Frequency = 0.8f };
    }
    public void Reset(Vector2 origin) => _anchor = origin;
    public Vector2 CalculateMovement(float delta)
    {
        _time += delta;
        return _anchor + new Vector2(
            _noise.GetNoise1D(_time) * _hoverRadius,
            _noise.GetNoise1D(_time + 1000f) * _hoverRadius);
    }
}
```

**Flutter (butterfly)** interpolates along a path between plants with a sine wave applied perpendicular to the travel direction. A half-sine damping envelope zeroes the wobble at departure and arrival for smooth transitions:

```csharp
public Vector2 CalculateMovement(float delta)
{
    _progress = Mathf.Clamp(_progress + (_speed * delta / _start.DistanceTo(_end)), 0f, 1f);
    Vector2 basePos = _start.Lerp(_end, _progress);
    Vector2 perp = new Vector2(-(_end - _start).Normalized().Y, (_end - _start).Normalized().X);
    float wobble = Mathf.Sin(_progress * _sineFrequency * Mathf.Tau) * _amplitude;
    wobble *= Mathf.Sin(_progress * Mathf.Pi); // dampen at endpoints
    return basePos + perp * wobble;
}
```

**Crawl (ladybug)** traces an elliptical path around the plant center. The **0.6× Y-axis compression** sells a top-down perspective on the circular crawl, and a slight radius oscillation prevents robotic uniformity:

```csharp
public Vector2 CalculateMovement(float delta)
{
    _angle += _angularSpeed * delta;
    float r = _radius + Mathf.Sin(_angle * 2f) * 2f;
    return _anchor + new Vector2(Mathf.Cos(_angle) * r, Mathf.Sin(_angle) * r * 0.6f);
}
```

**Erratic (moth/dragonfly)** uses frequent random velocity changes with a soft tether that `Lerp`s the insect back toward its anchor when it strays beyond `maxDistance`. This creates convincing jittery movement without hard boundary snapping.

All four behaviors feed into a shared `ProcessMoving()` that applies a soft positional clamp and sprite flipping:

```csharp
private void ProcessMoving(float dt)
{
    Vector2 target = _movement.CalculateMovement(dt);
    Vector2 offset = target - _plantAnchor;
    if (offset.Length() > _maxWander)
        target = _plantAnchor + offset.Normalized() * _maxWander;
    GlobalPosition = GlobalPosition.Lerp(target, dt * 8f);
    _sprite.FlipH = target.X - GlobalPosition.X < -0.5f;
}
```

**Randomize per-instance parameters** (±15% speed variance, ±20% pause timing jitter) so identical species feel distinct. Sine-based micro-bobbing on the sprite's local Y position—even during pauses—sells continuous life.

---

## 3. Spawn manager: Neko Atsume's passive loop in real-time

The spawn system adapts Neko Atsume's core design loop—**place items, creatures arrive passively, chance-based, rare creatures need specific setups**—to real-time Godot. The critical difference: Neko Atsume is check-in-and-leave; Project Flutter spawns live, so staggering matters.

**SpawnSystem lives as a child of the Garden node**, not an autoload. It's scene-specific (needs direct access to plants and insect containers), should pause/free with the garden, and EventBus already handles cross-scene communication. Scene tree layout:

```
Garden (Node2D)
├── TileMapLayer
├── PlantContainer (Node2D)
├── InsectContainer (Node2D)
├── SpawnSystem (Node)
│   ├── SpawnTimer (Timer, ~5s)
│   └── DespawnTimer (Timer, ~10s)
└── TimeManager
```

**The spawn algorithm runs once per tick, attempting at most one spawn** for natural staggering. Never batch-spawn all eligible insects simultaneously:

```csharp
private void OnSpawnTick()
{
    _spawnTimer.WaitTime = 5.0 + _rng.RandfRange(-1.0, 1.0); // jitter next interval

    var plantsWithSlots = GetBloomingPlantsWithAvailableSlots();
    if (plantsWithSlots.Count == 0) return;
    if (_rng.Randf() > 0.6f) return; // 40% quiet ticks build anticipation

    var targetPlant = plantsWithSlots[_rng.RandiRange(0, plantsWithSlots.Count - 1)];
    if (GetInsectCountInZone(targetPlant.Zone) >= MaxInsectsPerZone) return;

    var ctx = BuildSpawnContext();
    var eligible = _allInsects
        .Where(i => i.Zone == targetPlant.Zone || i.Zone == "Any")
        .Where(i => MatchesTimeOfDay(i.TimeOfDay, ctx.CurrentTime))
        .Where(i => MeetsSpawnCondition(i, ctx))
        .ToList();

    if (eligible.Count == 0) return;
    var selected = WeightedRandomSelect(eligible);
    SpawnInsect(selected, targetPlant);
}
```

**Weighted random selection** uses the standard cumulative-weight algorithm—O(n) per roll, trivially fast for ~50 insect types:

```csharp
private InsectData WeightedRandomSelect(List<InsectData> candidates)
{
    float total = candidates.Sum(c => c.SpawnWeight);
    float roll = _rng.RandfRange(0f, total);
    float cumulative = 0f;
    foreach (var c in candidates)
    {
        cumulative += c.SpawnWeight;
        if (roll < cumulative) return c;
    }
    return candidates[^1];
}
```

**Plant slot tracking** uses a simple `List<Insect>` per plant (slots are 1–3, so O(n) removal is negligible). Connect to the insect's `TreeExiting` signal for automatic cleanup when it's freed:

```csharp
public partial class PlantEntity : Node2D
{
    private List<Insect> _occupiedSlots = new();
    public bool HasAvailableSlot() => _occupiedSlots.Count < InsectSlotCount;

    public void OccupySlot(Insect insect)
    {
        _occupiedSlots.Add(insect);
        insect.TreeExiting += () => _occupiedSlots.Remove(insect);
    }

    public Vector2 GetSlotWorldPosition(int index)
    {
        float angle = (Mathf.Tau / InsectSlotCount) * index;
        return GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 16f;
    }
}
```

---

## 4. Attraction matching and special spawn conditions

Build a `HashSet<string>` of blooming plant types **once per spawn tick**, then run LINQ filters against it. This is O(n) for plants + O(m) per insect's requirements—trivially fast:

```csharp
private bool MeetsPlantRequirements(InsectData insect, HashSet<string> bloomingTypes)
{
    if (insect.RequiredPlants == null || insect.RequiredPlants.Length == 0) return true;
    return insect.RequiredPlants.All(req => bloomingTypes.Contains(req));
}
```

**For special conditions** (Praying Mantis needs 5+ insects present, Monarch needs 3+ Milkweed AND Goldenrod), use a **hybrid approach**: data-driven fields on InsectData cover most cases, with a delegate dictionary for truly exotic logic. Add two optional fields to InsectData:

```csharp
[Export] public int MinInsectsRequired { get; set; } = 0;
[Export] public Godot.Collections.Dictionary<string, int> RequiredPlantCounts { get; set; }
```

This lets designers set `MinInsectsRequired = 5` on the Praying Mantis `.tres` and `RequiredPlantCounts = { "milkweed": 3 }` on the Monarch—no code changes. Reserve a `Dictionary<string, Func<SpawnContext, bool>>` for the 2–3 species that need logic impossible in data:

```csharp
_specialConditions["luna_moth"] = ctx =>
    ctx.BloomingPlantTypes.Contains("moonflower")
    && ctx.CurrentTime == TimeOfDay.Night
    && ctx.ActiveInsectsBySpecies.GetValueOrDefault("luna_moth") == 0;
```

**Loading all InsectData from a directory** uses `DirAccess` with the critical `.remap` suffix strip for export compatibility:

```csharp
private void LoadAllInsectData(string path)
{
    using var dir = DirAccess.Open(path);
    if (dir == null) { GD.PushError($"Cannot open: {path}"); return; }
    foreach (string file in dir.GetFiles())
    {
        if (!file.EndsWith(".tres") && !file.EndsWith(".tres.remap")) continue;
        string cleanPath = path.PathJoin(file.Replace(".remap", ""));
        var data = GD.Load<InsectData>(cleanPath);
        if (data != null) _allInsects.Add(data);
    }
}
```

The `.remap` gotcha is well-documented in Godot forums: after export, `.tres` files get a `.remap` suffix, but `ResourceLoader` resolves the original path internally once you strip it.

---

## 5. Lifecycle flow: Tween-driven arrival and departure with game-time visits

The full lifecycle uses **Godot Tweens for cinematic arrival/departure** and **`_Process`-driven timers for visit duration** (scaled by `TimeManager.TimeScale` for fast-forward).

**Arrival: fly in from a random screen edge** with `Tween.TransitionType.Sine` + `EaseType.Out` for gentle deceleration. This feels more alive than fade-in—players watch insects arrive, building the cozy anticipation loop:

```csharp
private void StartArrival()
{
    _currentTween?.Kill();
    _currentTween = CreateTween();
    _currentTween.TweenProperty(this, "global_position", _targetPlantPosition, 1.5f)
        .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    _currentTween.Parallel().TweenProperty(this, "modulate:a", 1.0f, 0.5f).From(0.0f);
    _currentTween.TweenCallback(Callable.From(() => TransitionTo(InsectState.Visiting)));
}
```

**Visit duration tracks game-time**, not wall-clock time. Multiply `delta` by `TimeManager.TimeScale` so 2× fast-forward halves visit duration in real seconds:

```csharp
if (_state == InsectState.Visiting)
{
    _visitTimeRemaining -= delta * TimeManager.Instance.TimeScale;
    if (_visitTimeRemaining <= 0) TransitionTo(InsectState.Departing);
}
```

**Departure triggers** are multiple: timer expiry, time-of-day transition (day insects leave at dusk), plant removal, or player scare. Subscribe to `TimeOfDayChangedEvent` via EventBus and check `_data.TimeOfDay != newTime`:

```csharp
private void OnTimeOfDayChanged(TimeOfDayChangedEvent evt)
{
    if (_data.TimeOfDay != TimeOfDay.Both && _data.TimeOfDay != evt.NewTimeOfDay)
        TransitionTo(InsectState.Departing);
}
```

**Departure animation** uses `EaseType.In` (accelerating away—the inverse of arrival's deceleration) for a natural exit feel. The complete signal flow:

```
SpawnSystem.SpawnInsect()
→ Insect.Initialize() + AddChild()
→ Arriving (Tween fly-in, 1.5s)
→ Visiting (EventBus: InsectArrivedEvent + first-discovery check)
→ _Process decrements visit timer × TimeScale
→ Timer/dusk/removal triggers Departing (Tween fly-out, 1.2s)
→ EventBus: InsectDepartedEvent
→ QueueFree() → TreeExiting auto-vacates plant slot
```

**Edge case handling**: if a plant is removed mid-visit, the insect listens for `PlantRemovedEvent` and departs. If time flips to night while a day insect is still arriving, `_currentTween.Kill()` in `StartDeparture()` cleanly replaces the arrival tween. Always use `GodotObject.IsInstanceValid()` before accessing any insect reference from external systems.

---

## 6. Skip object pooling—cache PackedScenes instead

**Object pooling is not worth the complexity at 10–12 insects per zone.** Community benchmarks (EasyPool, GDQuest) show pooling benefits materialize around thousands of objects with rapid spawn/despawn (bullet-hell scenarios). At one insect spawned every 5 seconds and max 10 alive, .NET 8's generational GC handles Gen0 collections in under 1 millisecond. The overhead of maintaining a pool, resetting state, and managing re-initialization exceeds the cost of `Instantiate()` + `QueueFree()`.

**Do cache PackedScenes.** Although Godot's `ResourceLoader` caches by default (`CacheMode.Reuse`), an explicit dictionary prevents accidental unloading and avoids repeated string path construction:

```csharp
private readonly Dictionary<string, PackedScene> _sceneCache = new();

private PackedScene GetCachedScene(string insectId)
{
    if (!_sceneCache.TryGetValue(insectId, out var scene))
    {
        scene = GD.Load<PackedScene>($"res://Scenes/Insects/Insect.tscn");
        _sceneCache[insectId] = scene;
    }
    return scene;
}
```

If all insects share one scene (configured via InsectData at runtime), this simplifies to a single cached `PackedScene`. **SpriteFrames are shared Resources**, not copied—GPU texture memory scales with species count, not instance count.

Key C# GC consideration: Godot signal parameters allocate heap arrays. For the EventBus, **use pure C# events rather than Godot `[Signal]` delegates** to avoid per-emission allocations. At 10 insects this is negligible, but it's good practice established now.

---

## 7. Wiring it all together with a pure C# EventBus

**Use a static C# EventBus for cross-system communication** rather than Godot's `[Signal]` system. Multiple Godot forum contributors confirm C# signals are "incredibly annoying" with type marshaling, and signal parameter arrays allocate on every emission. The pure C# approach is type-safe, zero-alloc for parameterless events, and cleaner:

```csharp
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _subs = new();

    public static void Subscribe<T>(Action<T> cb)
    {
        var t = typeof(T);
        if (!_subs.TryGetValue(t, out var list)) _subs[t] = list = new();
        list.Add(cb);
    }
    public static void Unsubscribe<T>(Action<T> cb)
    {
        if (_subs.TryGetValue(typeof(T), out var list)) list.Remove(cb);
    }
    public static void Publish<T>(T evt)
    {
        if (!_subs.TryGetValue(typeof(T), out var list)) return;
        foreach (var cb in list.ToArray()) ((Action<T>)cb)?.Invoke(evt);
    }
}
```

**Event types for Sprint 2:**

```csharp
public record InsectArrivedEvent(InsectData Data, Vector2 Position);
public record InsectDepartedEvent(InsectData Data, Vector2 Position);
public record InsectDiscoveredEvent(InsectData Data);  // first sighting
public record InsectClickedEvent(InsectData Data, Insect Insect, Vector2 Position, bool IsStationary);
public record TimeOfDayChangedEvent(TimeOfDay Old, TimeOfDay New);
public record PlantRemovedEvent(Vector2I CellPosition);
```

**Coupling strategy by relationship type:**

| Communication path | Pattern | Why |
|---|---|---|
| TimeManager → SpawnSystem/Insects | EventBus | TimeManager shouldn't know about insects |
| SpawnSystem → Insect | Direct `Initialize()` call | Creator owns the instance |
| Insect → SpawnSystem (departure) | EventBus + `TreeExiting` | Insect shouldn't reference SpawnSystem |
| Insect → UI / Collection | EventBus | Full decoupling across boundaries |
| Area2D click → Photography (Sprint 3) | EventBus + `GetPhotoData()` | Click event decoupled; data via direct method on the Insect passed in the event |

**Always unsubscribe from EventBus in `_ExitTree()`** to prevent calls to freed objects. Use `GodotObject.IsInstanceValid()` in any callback that holds a node reference.

For Sprint 3 photography hooks, the insect already exposes `GetPhotoData()` returning species ID, rarity, first-discovery status, position, and current animation frame—everything the camera system will need to score and catalog a photo.

## Conclusion

The architecture decomposes cleanly into three concerns: **data** (InsectData resources loaded from `res://Data/Insects/`), **behavior** (IMovementBehavior implementations selected by factory), and **orchestration** (SpawnSystem's timer-driven Neko Atsume loop). The enum FSM keeps lifecycle management in one readable file. FastNoiseLite and sine-wave math produce organic movement without external dependencies. Pure C# EventBus provides type-safe decoupling that outperforms Godot signals in C# ergonomics. The slot system auto-cleans via `TreeExiting`, and all timers scale with `TimeManager.TimeScale` for free fast-forward support. The one non-obvious gotcha to watch: always strip `.remap` suffixes when scanning resource directories for export builds.