# Sprint 3 implementation guide for Project Flutter's photography system

**The photography-and-journal loop requires six new files and ~25 lines added to Insect.cs, all wired through the existing EventBus.** The core architecture places a `PhotoFocusController` (Control with `_Draw()`) on UILayer for zoom-independent focus circles, a `PhotoSequenceManager` to orchestrate the shutter sequence, a `NotificationManager` with a FIFO queue for discovery banners, and a persistent `JournalUI` toggled via a new `GameState.Journal` state. Every system communicates through typed event structs — no direct coupling. Below is the complete implementation blueprint across all seven research areas.

## Photo mode toggle and the state-gated input pattern

The cleanest modal input architecture reuses `GameManager.GameState` as the state machine rather than building a separate FSM. A single `_UnhandledInput()` override on the garden's input handler checks `CurrentState` and delegates accordingly. This avoids the fragility of toggling `SetProcessUnhandledInput()` per node and keeps all input routing centralized.

```csharp
public partial class GardenInputHandler : Node2D
{
    public override void _UnhandledInput(InputEvent @event)
    {
        switch (GameManager.Instance.CurrentState)
        {
            case GameState.Playing:
                HandlePlayingInput(@event);
                break;
            case GameState.PhotoMode:
                HandlePhotoModeInput(@event);
                break;
        }
    }

    private void HandlePhotoModeInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
                EventBus.Publish(new PhotoFocusStartedEvent(GetGlobalMousePosition()));
            else
                EventBus.Publish(new PhotoFocusReleasedEvent(GetGlobalMousePosition()));
            GetViewport().SetInputAsHandled();
        }
    }
}
```

**Toggling into photo mode** uses a keybind (Tab or C) caught in `_UnhandledKeyInput()`. The call to `GameManager.Instance.ChangeState(GameState.PhotoMode)` publishes `GameStateChangedEvent`, and subscribers react independently: a `CursorManager` swaps to a camera cursor via `Input.SetCustomMouseCursor(cameraTex, Input.CursorShape.Arrow, new Vector2(16, 16))`, the `PhotoModeOverlay` shows a "📷 Photo Mode" indicator, and garden tile `InputPickable` flags flip off. The hardware cursor API requires images under 256×256px imported as Lossless — no latency compared to drawing a cursor via `_Draw()`.

**The focus circle belongs in screen space** (UILayer, CanvasLayer layer=10), not garden world space. If placed in the garden, Camera2D zoom would distort the circle's apparent size, requiring counter-scaling that introduces jitter. Since the focus circle is UI feedback, it should render identically regardless of zoom level. The only conversion needed is projecting the insect's world position to screen coordinates each frame.

An important interaction between `Area2D.InputPickable` and `_UnhandledInput`: insect Area2D nodes with `InputPickable=true` fire their `_InputEvent` signal *before* `_UnhandledInput` propagation. This is beneficial — in PhotoMode, insects detect clicks via their own `_InputEvent`, publish `InsectClickedEvent`, and the `PhotoFocusController` subscribes to start focus. In Playing mode, `InsectClickedEvent` is simply ignored.

## Concentric circle focus mechanic with _Draw()

The `PhotoFocusController` is a `Control` node parented to UILayer with **`MouseFilter = MouseFilterEnum.Ignore`** (critical — prevents it from intercepting clicks). It uses `_Draw()` for all rendering and `QueueRedraw()` only while actively focusing, keeping the draw overhead to exactly zero when idle.

**World-to-screen conversion** uses a single line: `Vector2 insectScreenPos = GetViewport().GetCanvasTransform() * _targetInsect.GlobalPosition`. This accounts for Camera2D offset and zoom. For the cursor position inside a Control on a CanvasLayer, use `GetViewport().GetMousePosition()` — not `GetGlobalMousePosition()`, which returns world-space coordinates.

The viewfinder draws **three concentric rings plus corner brackets** for a camera-style aesthetic, all achievable with `DrawArc` and `DrawLine`:

```csharp
public override void _Draw()
{
    if (!_isFocusing || !IsInstanceValid(_targetInsect)) return;

    Vector2 cursor = GetViewport().GetMousePosition();
    float progress = Mathf.Clamp(_focusElapsed / FocusDuration, 0f, 1f);
    float radius = Mathf.Lerp(80f, 20f, progress);

    // Outer ring fades out as focus tightens
    var outerColor = new Color(1f, 1f, 1f, 0.3f * (1f - progress));
    DrawArc(cursor, radius + 10f, 0f, Mathf.Tau, 64, outerColor, 2f, true);

    // Main ring grows more opaque
    var mainColor = new Color(0.8f, 0.95f, 0.8f, 0.6f + 0.4f * progress);
    DrawArc(cursor, radius, 0f, Mathf.Tau, 64, mainColor, 3f, true);

    // Inner ring appears after 30% progress
    if (progress > 0.3f)
    {
        float innerAlpha = (progress - 0.3f) / 0.7f;
        DrawArc(cursor, radius * 0.6f, 0f, Mathf.Tau, 64,
            new Color(0.5f, 1f, 0.5f, innerAlpha * 0.8f), 2f, true);
    }

    // Crosshairs
    float cl = radius * 0.3f;
    var crossColor = new Color(1f, 1f, 1f, 0.5f);
    DrawLine(cursor + new Vector2(-cl, 0), cursor + new Vector2(cl, 0), crossColor, 1f);
    DrawLine(cursor + new Vector2(0, -cl), cursor + new Vector2(0, cl), crossColor, 1f);

    // Corner brackets (viewfinder style)
    float bs = radius * 0.4f, bo = radius * 0.7f;
    var bc = new Color(1f, 1f, 1f, 0.7f);
    Vector2 tl = cursor + new Vector2(-bo, -bo);
    DrawLine(tl, tl + new Vector2(bs, 0), bc, 2f);
    DrawLine(tl, tl + new Vector2(0, bs), bc, 2f);
    // ... repeat for TR, BL, BR corners
}
```

**Performance is a non-issue**: ~15 draw calls per frame for the viewfinder is negligible. Godot only degrades with thousands of primitives — a forum benchmark showed 90,000 circles were needed to drop to 16fps. Call `QueueRedraw()` only inside the `if (_isFocusing)` guard in `_Process()`.

**Focus does not hard-break if the insect drifts** — it degrades quality instead. Hard-breaking feels punishing and conflicts with the cozy tone. The player can always complete the photo; the question is quality. However, focus **does cancel** on mouse release before `FocusDuration` completes, insect node freed/departed, insect leaves Visiting state, or GameState changes away from PhotoMode. Subscribe to `InsectDepartedEvent` to catch the departure case.

**FocusDuration runs on real-time delta**, not `TimeManager.SpeedMultiplier`. The focus is a player reflex action, not a simulation event. Insects *do* move faster under speed multiplier, which naturally increases difficulty — that provides sufficient challenge scaling.

## Quality rating at the shutter moment

At the instant the circle fully closes (elapsed ≥ FocusDuration while mouse held), the system calculates distance from cursor to insect center in **world space** using `GetGlobalMousePosition()` and `_targetInsect.GlobalPosition`. World space avoids all camera-transform complications since both values share the same coordinate system.

```csharp
public static int CalculateStarRating(
    Vector2 cursorWorld, Vector2 insectWorld,
    float initialRadius, float threeStarPct, float twoStarPct)
{
    float normalized = cursorWorld.DistanceTo(insectWorld) / initialRadius;
    if (normalized <= threeStarPct) return 3;
    if (normalized <= twoStarPct) return 2;
    return 1;
}
```

**PhotoDifficulty modifies thresholds, not focus duration.** Changing duration per insect would feel inconsistent — players expect the core mechanic to behave predictably. Instead, erratic insects get tighter threshold multipliers (Easy=1.0, Medium=0.85, Hard=0.7, VeryHard=0.55), creating a double penalty: harder-to-track movement *and* stricter accuracy requirements.

All tuning values live in a **`PhotoConfig` Resource** with `[Export]` fields — `FocusDuration`, `ThreeStarRadius`, `TwoStarRadius`, `InitialCircleRadius`, `FreezeFrameDuration`. Resources are serialized to `.tres` files, editable in the inspector without recompiling, and support `[Export(PropertyHint.Range)]` for sliders. Load with a fallback: `Config ??= GD.Load<PhotoConfig>("res://Resources/default_photo_config.tres")`.

**Edge case handling**: if the insect departs at the exact shutter moment, check `IsInstanceValid(_targetInsect)` first. If invalid, publish a `PhotoMissedEvent` and skip journal recording. If valid, call `Freeze()` immediately to prevent departure during the result display.

## Shutter flash, freeze, and the full feedback sequence

The complete sequence fires in order: **shutter SFX → white flash → insect freeze → rating calculation → star popup → auto-dismiss**. All of this overlays live gameplay — the game does not pause.

**Shutter sound** uses a non-positional `AudioStreamPlayer` (not 2D), since the click is a camera/UI sound that should play at full volume regardless of where the insect is. One-shot SFX in Godot 4: set the stream, call `Play()`, non-looping audio stops automatically.

**White flash** is a full-rect `ColorRect` on UILayer (Color white, alpha 0, `MouseFilter=Ignore`). The tween ramps alpha 0→0.7 in 30% of the duration, then fades 0.7→0 in the remaining 70%:

```csharp
public void Flash(float maxAlpha = 0.7f, float duration = 0.3f)
{
    var tween = CreateTween();
    tween.TweenProperty(this, "color:a", maxAlpha, duration * 0.3f)
         .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    tween.TweenProperty(this, "color:a", 0.0f, duration * 0.7f)
         .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
}
```

**Insect freeze** uses a simple `_isFrozen` bool checked at the top of `_Process()` with an early return that skips all movement. This is surgical — `ProcessMode.Disabled` would also kill `_Draw()` updates and child animations. The freeze includes a subtle `Modulate` brighten (1.3→1.0 over the duration) for visual feedback that the insect was "captured."

**Camera shake** adds a gentle trauma-based shake using `FastNoiseLite` for smooth, organic oscillation (not `GD.RandRange`, which produces jarring jitter). For a cozy game, use conservative values: **MaxOffset of 5px** and **trauma of 0.3**. The camera subscribes to `PhotoTakenEvent` independently.

**Star rating popup** is a `PackedScene` instantiated at runtime on UILayer. Stars animate in sequentially using `Tween.TransitionType.Back` for a satisfying pop-in, then the popup auto-dismisses via `QueueFree()` after 2 seconds. Using PackedScene instantiation (not a persistent hidden node) is cleaner because multiple rapid photos could need simultaneous popups.

The **orchestrator** (`PhotoSequenceManager`) ties these systems together while keeping them decoupled — each effect subscribes to `PhotoTakenEvent` independently:

```csharp
// ScreenFlash      → subscribes to PhotoTakenEvent → flashes white
// ShutterSfx       → subscribes to PhotoTakenEvent → plays click  
// ShakeCamera2D    → subscribes to PhotoTakenEvent → adds trauma
// PhotoSequenceManager → subscribes to ShutterFiredEvent → orchestrates popup + journal
```

Removing or changing any single effect requires zero changes to other systems.

## Journal UI as a persistent Control with GridContainer cells

The journal is a **persistent hidden Control** in UILayer, toggled visible via `JournalUI.OpenJournal()`. Instantiating on demand would cause a micro-stutter the first time (~25 cells to create). With only **~25 species at 96×96 portraits**, the total memory footprint is under 10MB — no lazy loading needed. Synchronous `GD.Load<InsectData>()` loads each `.tres` in microseconds since textures are lazy-loaded to GPU only when first drawn.

**Add `GameState.Journal`** to the enum rather than treating the journal as a pause or overlay. This gives clear state separation: ambient garden life continues (it's not Paused), but player input is blocked (it's not Playing). The journal's background `ColorRect` with `MouseFilter = MouseFilterEnum.Stop` on a full-rect anchor blocks all mouse events from reaching `_UnhandledInput()`.

The node hierarchy uses a **`GridContainer`** (Columns=5) inside a `ScrollContainer` for future-proofing beyond 25 species. Each cell is a custom `JournalCell` scene:

```
JournalUI (Control) [FullRect, Visible: false]
├── Background (ColorRect) [FullRect, rgba(0,0,0,0.4), MouseFilter: Stop]
└── JournalPanel (PanelContainer) [anchored center, 900×600]
    └── MarginContainer
        └── VBoxContainer
            ├── HeaderHBox [TitleLabel, FilterButton, SortButton, CloseButton]
            ├── GridView (ScrollContainer → SpeciesGrid GridContainer, Columns=5)
            ├── DetailView (Visible: false) [IllustrationRect, NameLabel, StarsHBox,
            │    FunFactText (RichTextLabel), HabitatLabel, BackButton]
            └── FooterLabel ["Discovered: 0 / 25"]
```

**Grid-to-detail transition uses visibility toggle**, not scene swapping. Set `GridView.Visible = false` and `DetailView.Visible = true` with an optional modulate-alpha tween. This keeps both views in the same scene, avoiding load times and simplifying the back button.

Each `JournalCell` is a `PanelContainer` with `MouseFilter=Stop`. Discovered cells show the portrait texture, species name, and filled star characters (★). Undiscovered cells show `JournalSilhouette` (grey) and "???" text. Cell clicks use **C# `Action<string>` delegates** (not Godot signals) for type safety and zero marshaling overhead:

```csharp
public partial class JournalCell : PanelContainer
{
    public Action<string>? CellClicked;
    private string _speciesId = "";

    public void Setup(InsectData data, bool discovered, int stars)
    {
        _speciesId = data.Id;
        _portrait.Texture = discovered ? data.JournalIllustration : data.JournalSilhouette;
        _nameLabel.Text = discovered ? data.DisplayName : "???";
        _starsLabel.Text = discovered ? new string('★', stars) + new string('☆', 3 - stars) : "";
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            CellClicked?.Invoke(_speciesId);
        }
    }
}
```

**Placeholder theming** uses `StyleBoxFlat` for the panel backgrounds — warm parchment colors (`#f5efe6`), brown borders (`#8b7355`), rounded corners, and drop shadows. Apply via `AddThemeStyleboxOverride("panel", style)`. For portraits without art, use a `ColorRect` sized to 96×96 with species-appropriate colors.

## Discovery flow and the notification queue

A `PhotoResultProcessor` static class mediates between the photo system and JournalManager, handling the three-branch logic:

- **New species**: `JournalManager.DiscoverSpecies(id, stars)` → JournalManager publishes `SpeciesDiscoveredEvent` → NotificationManager shows "✨ New Discovery! ✨" banner with fanfare
- **Better rating**: `JournalManager.UpdateStarRating(id, stars)` → publishes `JournalUpdatedEvent` → "📸 New Best Photo!" banner with chime
- **Same or worse**: publishes `SimplePhotoFeedbackEvent` → subtle "Photo taken" toast

The `NotificationManager` lives as a **persistent child of UILayer** (not an autoload — it's a UI concern). It maintains a `Queue<NotificationData>` from `System.Collections.Generic` (.NET BCL collections work perfectly in Godot C# for internal logic). Notifications display sequentially: when the current banner's tween completes, it calls `ShowNextNotification()` which dequeues the next item.

```csharp
private void ShowNextNotification()
{
    if (_queue.Count == 0) { _isShowing = false; return; }
    _isShowing = true;
    var data = _queue.Dequeue();
    _activeBanner = _bannerScene.Instantiate<NotificationBanner>();
    AddChild(_activeBanner);
    _activeBanner.Setup(data);
    _activeBanner.AnimateIn();
    var tween = CreateTween();
    tween.TweenInterval(data.DisplayDuration);
    tween.TweenCallback(Callable.From(DismissCurrentBanner));
}
```

Banners slide in from the top using `Tween.TransitionType.Back` for a satisfying overshoot, and slide out with `Tween.TransitionType.Quad`. **New discovery banners** include `CpuParticles2D` (not GPU — works on all hardware) configured as a one-shot burst: 20 gold particles, `Explosiveness=0.9`, spreading 180° with light gravity drift. The particles fire after the slide-in completes.

**All events use `readonly record struct`** (C# 12) for value semantics, immutability, and auto-generated equality:

```csharp
public readonly record struct PhotoTakenEvent(
    string InsectId, string DisplayName, int StarRating, Vector2 WorldPosition);
public readonly record struct SimplePhotoFeedbackEvent(string InsectId, Vector2 WorldPosition);
```

## Minimal, clean extensions to Insect.cs

The recommended approach is a **hybrid**: add targeted public surface area to Insect.cs for operations that only the insect can own (freeze, state queries), while the photo system queries JournalManager independently for discovery status. Pure event-driven (Option C) was rejected because the photo system needs synchronous, frame-accurate position data that events cannot efficiently provide.

**Total additions to Insect.cs: ~25 lines**, with no modification to existing behavior:

```csharp
// New public surface area
public InsectData Data => _data;
public InsectState CurrentState => _state;
public bool IsPhotographable => _state == InsectState.Visiting && !_isFrozen && IsInsideTree();

private bool _isFrozen;
private double _freezeTimer;

public void Freeze(float duration = 0.5f)
{
    if (_state != InsectState.Visiting) return;
    _isFrozen = true;
    _freezeTimer = duration;
    Modulate = new Color(1.3f, 1.3f, 1.3f, 1f);
    CreateTween().TweenProperty(this, "modulate",
        new Color(1f, 1f, 1f, 1f), duration * 0.8f)
        .SetTrans(Tween.TransitionType.Sine);
}

// In _Process, add at the very top:
if (_isFrozen)
{
    _freezeTimer -= delta;
    if (_freezeTimer <= 0) _isFrozen = false;
    QueueRedraw();
    return; // Skip movement, keep drawing
}
```

The `PhotoFocusController` holds a **direct C# reference** to the target `Insect` node, guarded by `IsInstanceValid()` every frame. This is O(1) — far better than `FindNode()` lookups. `IsInstanceValid()` checks both null and freed native instances, catching all edge cases. **Do not use `WeakRef`** in Godot C# — it has known historical issues; `IsInstanceValid` is the canonical pattern.

**Race condition safety** uses a belt-and-suspenders approach: Insect publishes `InsectDepartingEvent` before transitioning to Departing state (event-based cancellation), and `_Process()` checks `IsInstanceValid() && IsPhotographable` every frame (poll-based fallback). The `Freeze()` method's `_state == Visiting` guard prevents freezing an already-departing insect.

An `InsectDepartingEvent` should be added to the departure transition in Insect.cs: `EventBus.Publish(new InsectDepartingEvent(Data.Id, this))` just before `_state = InsectState.Departing`.

## Conclusion

The photography system adds six new files and touches one existing file, all communicating through **eight event types** on the existing static EventBus. Three architectural decisions deserve emphasis. First, placing the focus circle in screen space (UILayer) rather than world space eliminates an entire class of zoom-related bugs. Second, degrading quality instead of breaking focus on cursor drift preserves the cozy feel — the mechanic is forgiving but rewards precision. Third, the `PhotoResultProcessor` mediator pattern keeps the three-way interaction between PhotoSystem, JournalManager, and NotificationManager fully decoupled, meaning any system can be modified or removed without cascading changes. The `PhotoConfig` Resource makes all tuning values inspector-editable, which will pay dividends during the playtesting phase that a **40-hour sprint** demands.