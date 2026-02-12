# Sprint 4 implementation blueprint for Project Flutter

**Three zones, a nectar economy, and 45 registerable species form the backbone of Sprint 4.** The recommended architecture keeps all three zone grids in memory simultaneously as sibling Node2D instances, swapping visibility and `ProcessMode` for instant zone transitions. This approach avoids serialization complexity, keeps state always-live, and costs negligible memory for three small grids. Below is a complete implementation guide covering all ten architectural questions with concrete C# code.

## 1. Zone architecture: multiple Garden instances with visibility swap

The strongest pattern for this game is **Option A: multiple Garden node instances as siblings**, toggling `Visible` and `ProcessMode`. With only three zones of modest size (4×4, 6×6, 5×5), keeping all in memory is trivial—roughly **1–2 KB of cell data per zone**. This eliminates serialization, scene loading delays, and state reconstruction bugs entirely.

**Scene tree structure:**

```
Root
├── GameManager (Autoload)
├── TimeManager (Autoload)
├── ZoneManager (Autoload)
├── GameWorld (Node2D)
│   ├── StarterZone (Garden)    ← Visible, ProcessMode.Inherit
│   ├── MeadowZone (Garden)     ← Hidden, ProcessMode.Disabled
│   └── PondZone (Garden)       ← Hidden, ProcessMode.Disabled
├── SpawnSystem (Node)
├── Camera2D
└── UILayer (CanvasLayer, layer=10)
    ├── HUD
    ├── SeedToolbar
    ├── ShopPanel
    ├── ZoneSelector
    └── JournalUI
```

The critical detail is that `ProcessMode = ProcessModeEnum.Disabled` cascades to all children, stopping `_Process`, `_PhysicsProcess`, and input handling on inactive zones. However, **static C# EventBus delegates still fire** on disabled nodes since they bypass Godot's processing pipeline—guard handlers with a `ProcessMode` check when necessary.

**ZoneData class for per-zone state:**

```csharp
public enum ZoneId { Starter, Meadow, Pond }
public enum CellType { Normal, Water }
public enum CellState { Empty, Tilled, Planted, Growing, Mature, Harvestable }

public class CellData
{
    public CellType Type { get; set; } = CellType.Normal;
    public CellState State { get; set; } = CellState.Empty;
    public string PlantId { get; set; } = string.Empty;
    public int GrowthStage { get; set; }
    public float GrowthTimer { get; set; }
    public List<string> InsectSlots { get; set; } = new();
}

public class ZoneData
{
    public ZoneId Id { get; }
    public string DisplayName { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    public bool IsUnlocked { get; set; }
    public int UnlockNectarCost { get; }
    public int UnlockJournalRequired { get; }
    public CellData[,] Cells { get; }

    public ZoneData(ZoneId id, string name, int w, int h,
                    bool unlocked, int nectarCost, int journalReq)
    {
        Id = id; DisplayName = name;
        GridWidth = w; GridHeight = h;
        IsUnlocked = unlocked;
        UnlockNectarCost = nectarCost;
        UnlockJournalRequired = journalReq;
        Cells = new CellData[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                Cells[x, y] = new CellData();
    }

    public bool IsPlantable(int x, int y) =>
        x >= 0 && x < GridWidth && y >= 0 && y < GridHeight
        && Cells[x, y].Type != CellType.Water
        && Cells[x, y].State == CellState.Empty;
}
```

**ZoneManager autoload** owns all zone state, handles switching, camera bounds, and unlock logic:

```csharp
public partial class ZoneManager : Node
{
    public static ZoneManager Instance { get; private set; }
    private Dictionary<ZoneId, ZoneData> _zones = new();
    private Dictionary<ZoneId, Garden> _gardenNodes = new();
    private ZoneId _activeZone = ZoneId.Starter;

    public ZoneId ActiveZoneId => _activeZone;
    public ZoneData ActiveZoneData => _zones[_activeZone];

    public override void _Ready()
    {
        Instance = this;
        _zones[ZoneId.Starter] = new ZoneData(ZoneId.Starter, "Garden", 4, 4, true, 0, 0);
        _zones[ZoneId.Meadow] = new ZoneData(ZoneId.Meadow, "Meadow", 6, 6, false, 100, 5);
        var pond = new ZoneData(ZoneId.Pond, "Pond", 5, 5, false, 150, 12);
        SetupPondWater(pond);
        _zones[ZoneId.Pond] = pond;
    }

    private void SetupPondWater(ZoneData pond)
    {
        int[,] water = { {1,1},{2,1},{3,1},{1,2},{2,2},{3,2},{2,3} };
        for (int i = 0; i < water.GetLength(0); i++)
            pond.Cells[water[i,0], water[i,1]].Type = CellType.Water;
    }

    public void RegisterGarden(ZoneId id, Garden g) => _gardenNodes[id] = g;

    public void SwitchToZone(ZoneId target)
    {
        if (target == _activeZone || !_zones[target].IsUnlocked) return;
        var prev = _activeZone;
        _gardenNodes[_activeZone].Visible = false;
        _gardenNodes[_activeZone].ProcessMode = ProcessModeEnum.Disabled;
        _activeZone = target;
        _gardenNodes[target].Visible = true;
        _gardenNodes[target].ProcessMode = ProcessModeEnum.Inherit;
        UpdateCameraBounds();
        EventBus.Publish(new ZoneChangedEvent(prev, target));
    }

    private void UpdateCameraBounds()
    {
        var cam = GetViewport().GetCamera2D();
        if (cam == null) return;
        var d = ActiveZoneData;
        int cs = 64, pad = 32;
        cam.LimitLeft = -pad; cam.LimitTop = -pad;
        cam.LimitRight = d.GridWidth * cs + pad;
        cam.LimitBottom = d.GridHeight * cs + pad;
        cam.Position = new Vector2(d.GridWidth * cs / 2f, d.GridHeight * cs / 2f);
        cam.ForceUpdateScroll();
    }

    public bool CanUnlock(ZoneId id)
    {
        var d = _zones[id];
        return !d.IsUnlocked
            && GameManager.Instance.Nectar >= d.UnlockNectarCost
            && GameManager.Instance.JournalEntryCount >= d.UnlockJournalRequired;
    }

    public bool TryUnlock(ZoneId id)
    {
        if (!CanUnlock(id)) return false;
        GameManager.Instance.SpendNectar(_zones[id].UnlockNectarCost);
        _zones[id].IsUnlocked = true;
        EventBus.Publish(new ZoneUnlockedEvent(id));
        return true;
    }
}
```

**Camera bounds by zone** (CellSize=64, padding=32):

| Zone | Grid | LimitRight | LimitBottom | Center position |
|------|------|------------|-------------|-----------------|
| Starter | 4×4 | 288 | 288 | (128, 128) |
| Meadow | 6×6 | 416 | 416 | (192, 192) |
| Pond | 5×5 | 352 | 352 | (160, 160) |

**SpawnSystem integration** is straightforward: on `ZoneChangedEvent`, the system re-evaluates spawn candidates using only the active zone's cell data. It iterates `ActiveZoneData.Cells`, collects mature plants, checks for water tiles, and builds the spawn pool accordingly.

## 2. Seed selection uses a bottom hotbar with cursor mode

Research into Stardew Valley, Coral Island, Ooblets, and Garden Paws confirms that the **bottom-screen hotbar with numbered slots** is the dominant UX pattern for cozy farming games. Players click or press 1–9 to select a seed type, then left-click cells to plant. Right-click or ESC deselects. The selected slot gets a **highlighted yellow border**, and a semi-transparent **ghost preview** appears on the hovered grid cell.

The implementation splits into three pieces: the `SeedToolbar` (UI control at screen bottom), a `SeedCursor` (Node2D in game world for ghost preview), and integration with the existing `_UnhandledInput()` grid click handler.

```csharp
public partial class SeedToolbar : Control
{
    private string _selectedPlantId;
    private List<Button> _slots = new();

    public string SelectedPlantId => _selectedPlantId;

    public override void _Ready()
    {
        AnchorLeft = 0.5f; AnchorRight = 0.5f;
        AnchorTop = 1f; AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Begin;
        RebuildSlots();
        EventBus.OnZoneChanged += _ => RebuildSlots();
    }

    private void RebuildSlots()
    {
        foreach (var c in GetChildren()) ((Node)c).QueueFree();
        _slots.Clear();
        var hbox = new HBoxContainer();
        AddChild(hbox);

        var zone = ZoneManager.Instance.ActiveZoneId;
        var plants = PlantRegistry.GetByZone(zone);
        int i = 0;
        foreach (var p in plants)
        {
            var btn = new Button { CustomMinimumSize = new Vector2(52, 52) };
            var plantId = p.Id; // capture
            btn.Pressed += () => SelectSeed(plantId);
            hbox.AddChild(btn);
            _slots.Add(btn);
            i++;
        }
    }

    private void SelectSeed(string plantId)
    {
        _selectedPlantId = (_selectedPlantId == plantId) ? null : plantId;
        EventBus.Publish(new SeedSelectedEvent(_selectedPlantId));
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey k && k.Pressed && k.Keycode == Key.Escape
            && _selectedPlantId != null)
        {
            _selectedPlantId = null;
            EventBus.Publish(new SeedSelectedEvent(null));
            GetViewport().SetInputAsHandled();
        }
    }
}
```

The **SeedCursor** is a `Node2D` child of `GameWorld` (not the UI layer) that follows the mouse, snaps to grid cells, and uses `_Draw()` to render a semi-transparent colored circle:

```csharp
public partial class SeedCursor : Node2D
{
    private PlantData _seed;
    private bool _validCell;

    public override void _Ready()
    {
        ZIndex = 100;
        Visible = false;
        EventBus.OnSeedSelected += e => {
            _seed = e.PlantId != null ? PlantRegistry.GetById(e.PlantId) : null;
            Visible = _seed != null;
        };
    }

    public override void _Process(double delta)
    {
        if (_seed == null) return;
        var mouse = GetGlobalMousePosition();
        int gx = (int)(mouse.X / 64), gy = (int)(mouse.Y / 64);
        _validCell = ZoneManager.Instance.ActiveZoneData.IsPlantable(gx, gy);
        GlobalPosition = new Vector2(gx * 64 + 32, gy * 64 + 32);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_seed == null) return;
        var c = _seed.DrawColor with { A = _validCell ? 0.55f : 0.2f };
        DrawCircle(Vector2.Zero, 14f, c);
        var ring = _validCell ? new Color(1,1,1,0.6f) : new Color(1,0,0,0.5f);
        DrawArc(Vector2.Zero, 15f, 0, Mathf.Tau, 32, ring, 1.5f);
    }
}
```

## 3. Shop panel as a modal overlay with programmatic UI

The seed shop opens as a **full-screen overlay** that sets `GameState.Shopping`, pausing garden interaction. It uses `ScrollContainer` + `VBoxContainer` built entirely in C# code. Each row displays a colored rectangle icon, plant name, nectar cost, and a "Buy" button. Seeds from locked zones appear grayed out with a lock icon.

```csharp
public partial class ShopPanel : Control
{
    private VBoxContainer _list;

    public override void _Ready()
    {
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(dim);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(420, 500);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        panel.OffsetLeft = -210; panel.OffsetRight = 210;
        panel.OffsetTop = -250; panel.OffsetBottom = 250;
        AddChild(panel);

        var outer = new VBoxContainer();
        panel.AddChild(outer);

        var title = new Label { Text = "Seed Shop",
            HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 22);
        outer.AddChild(title);
        outer.AddChild(new HSeparator());

        var scroll = new ScrollContainer {
            CustomMinimumSize = new Vector2(0, 380),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        outer.AddChild(scroll);

        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_list);

        var close = new Button { Text = "Close" };
        close.Pressed += () => { Visible = false;
            EventBus.Publish(new ShopClosedEvent()); };
        outer.AddChild(close);
    }

    public void Open()
    {
        PopulateList();
        Visible = true;
        EventBus.Publish(new ShopOpenedEvent());
    }

    private void PopulateList()
    {
        foreach (var c in _list.GetChildren()) ((Node)c).QueueFree();
        foreach (var p in PlantRegistry.All())
        {
            bool locked = !ZoneManager.Instance.GetZoneData(
                ZoneIdFromPlantZone(p.Zone)).IsUnlocked;
            var row = new HBoxContainer();
            // Icon: colored rect via custom Control
            // Name, Cost labels, Buy button
            var buy = new Button { Text = locked ? "🔒" : "Buy",
                Disabled = locked || GameManager.Instance.Nectar < p.SeedCost };
            if (!locked) {
                var id = p.Id;
                buy.Pressed += () => PurchaseSeed(id);
            }
            _list.AddChild(row);
        }
    }

    private void PurchaseSeed(string plantId)
    {
        var p = PlantRegistry.GetById(plantId);
        if (GameManager.Instance.Nectar < p.SeedCost) return;
        GameManager.Instance.SpendNectar(p.SeedCost);
        EventBus.Publish(new SeedPurchasedEvent(plantId, p.SeedCost));
    }
}
```

The shop should be a `GameState.Shopping` state rather than a transparent overlay because it requires the player's full attention and prevents accidental garden interactions. The existing `_UnhandledInput()` handlers in the Garden should check `GameManager.State == GameState.Playing` before processing clicks.

## 4. PlantRegistry: 20 species as a static C# dictionary

The registry mirrors the established `InsectRegistry` pattern—a static class with a `Dictionary<string, PlantData>` populated in the static constructor. Each plant is a C# `record` with **13 fields** covering identity, economy, growth, attraction, and rendering.

```csharp
public record PlantData(
    string Id, string DisplayName, PlantZone Zone, PlantRarity Rarity,
    int SeedCost, int NectarYield, int GrowthCycles, int InsectSlots,
    bool NightBlooming, string[] AttractedInsects,
    Color DrawColor, Vector2I TileSize);
```

The complete 20-plant registry organized by zone:

| Plant | Zone | Rarity | Cost | Yield | Growth | Slots | Night | Key Attractions |
|-------|------|--------|------|-------|--------|-------|-------|-----------------|
| Lavender | Starter | Common | 5 | 3 | 2 | 2 | No | Honeybee, Bumblebee |
| Sunflower | Starter | Common | 6 | 3 | 2 | 2 | No | Honeybee, Ladybug, Bumblebee |
| Daisy | Starter | Common | 5 | 3 | 2 | 2 | No | Honeybee, Cabbage White |
| Coneflower | Starter | Common | 7 | 3 | 2 | 2 | No | Cabbage White, Garden Spider, Bumblebee |
| Marigold | Starter | Common | 6 | 3 | 2 | 2 | No | Ladybug, Garden Spider |
| Milkweed | Meadow | Uncommon | 25 | 5 | 3 | 3 | No | Monarch, Hummingbird Moth |
| Dill | Meadow | Common | 10 | 3 | 3 | 2 | No | Swallowtail, Green Lacewing |
| Goldenrod | Meadow | Common | 12 | 3 | 3 | 2 | No | Monarch, Green Lacewing |
| Wildflower Mix | Meadow | Uncommon | 30 | 5 | 3 | 3 | No | Swallowtail, Painted Lady |
| Black-Eyed Susan | Meadow | Common | 15 | 4 | 3 | 2 | No | Painted Lady, Hummingbird Moth |
| Water Lily | Pond | Uncommon | 30 | 5 | 3 | 2 | No | Blue Dasher, Damselfly |
| Cattail | Pond | Common | 10 | 3 | 3 | 2 | No | Blue Dasher, Damselfly |
| Iris | Pond | Uncommon | 25 | 5 | 3 | 2 | No | Damselfly |
| Lotus | Pond | Rare | 60 | 8 | 5 | 3 | No | Jewel Beetle, Emperor Dragonfly |
| Passionflower | Pond | Rare | 50 | 7 | 4 | 3 | No | Jewel Beetle, Swallowtail |
| Moonflower | Starter | Uncommon | 25 | 5 | 3 | 2 | Yes | Luna Moth, Firefly |
| Evening Primrose | Starter | Common | 8 | 3 | 2 | 2 | Yes | Firefly, Cricket |
| Night Jasmine | Meadow | Rare | 60 | 8 | 4 | 3 | Yes | Luna Moth, Hawk Moth |
| **White Birch** | Meadow | Rare | **75** | **10** | 5 | 3 | Yes | Walking Stick, Atlas Moth |
| Switchgrass | Pond | Uncommon | 20 | 5 | 3 | 2 | Yes | Cricket, Walking Stick |

**Handling the White Birch 2×2 plant** requires three design decisions. First, `TileSize = new Vector2I(2, 2)` in the data record distinguishes it from 1×1 plants. Second, placement validation must check all four cells (anchor + right + below + diagonal) are plantable. Third, the anchor cell (top-left) stores the `PlantId` while the other three cells store a reference string like `"white_birch_ref"` that prevents other plants from being placed there but delegates all logic to the anchor:

```csharp
public bool TryPlantMultiTile(int ax, int ay, PlantData plant, ZoneData zone)
{
    for (int dx = 0; dx < plant.TileSize.X; dx++)
        for (int dy = 0; dy < plant.TileSize.Y; dy++)
            if (!zone.IsPlantable(ax + dx, ay + dy)) return false;

    zone.Cells[ax, ay].PlantId = plant.Id;
    zone.Cells[ax, ay].State = CellState.Planted;
    for (int dx = 0; dx < plant.TileSize.X; dx++)
        for (int dy = 0; dy < plant.TileSize.Y; dy++)
            if (dx != 0 || dy != 0) {
                zone.Cells[ax+dx, ay+dy].PlantId = $"{plant.Id}_ref";
                zone.Cells[ax+dx, ay+dy].State = CellState.Planted;
            }
    return true;
}
```

## 5. InsectRegistry expands to 25 species with three new movement patterns

The expanded registry adds **Dart** (dragonflies darting between positions), **Skim** (water striders gliding across water surfaces), and **Pulse** (fireflies with rhythmic glow oscillation) to the existing Hover, Flutter, Crawl, and Erratic patterns. Each movement pattern corresponds to a behavior class implementing a shared interface.

The spawn condition system uses a flexible `SpawnCondition` record that encodes special requirements:

```csharp
public enum SpawnConditionType {
    None, PlantAttraction, WaterRequired,
    MinInsectsPresent, MultiPlantCombo,
    AllNightPlants, MinPlantDiversity }

public record SpawnCondition(
    SpawnConditionType Type, string Parameter = "", int Count = 0);
```

**Special spawn conditions for notable insects:**

- **Praying Mantis**: `new SpawnCondition(MinInsectsPresent, "", 5)` — only appears when 5+ other insects are already active in the zone, creating a natural "ecosystem threshold" reward
- **Water Strider/Pond Skater/Emperor Dragonfly**: `new SpawnCondition(WaterRequired)` — SpawnSystem checks `cell.Type == CellType.Water` in the active zone
- **Monarch Migration**: `new SpawnCondition(MultiPlantCombo, "milkweed,goldenrod", 3)` — requires at least 3 total Milkweed + Goldenrod plants blooming simultaneously
- **Atlas Moth**: `new SpawnCondition(AllNightPlants, "", 4)` — requires all 4 night-blooming plant species present in the zone (the ultimate achievement insect)
- **Rainbow Scarab**: `new SpawnCondition(MinPlantDiversity, "", 8)` — requires 8+ different plant species, encouraging players to diversify rather than monocrop

**Spawn weight distribution** follows a geometric rarity curve: Common **45**, Uncommon **25**, Rare **12**, Very Rare **4**. This means in any spawn evaluation, a Common insect is roughly **11× more likely** than a Very Rare one, making rare sightings feel genuinely special without being impossibly frustrating.

**Visit durations decrease with rarity** (Common **8–15s**, Uncommon **6–10s**, Rare **4–7s**, Very Rare **3–5s**), creating the core photography tension: rare insects are both harder to encounter and harder to photograph well due to shorter windows.

Night insects should be assigned to the zone where their attracted plants primarily grow (e.g., Firefly → Starter since Evening Primrose and Moonflower are Starter plants), with an additional `NightOnly` boolean field. The SpawnSystem checks `TimeManager.IsNight` before including night insects in the spawn pool.

## 6. Pond water tiles use CellType, not CellState

Water is a **tile type**, not a state—a cell's type is immutable while its state changes through the plant lifecycle. Adding `Water` to `CellType` (not `CellState`) keeps the grid system unified. The recommended approach uses **pre-placed water tiles** in a fixed layout (7 tiles forming a central pond cluster), giving the Pond zone immediate visual identity without requiring the player to purchase infrastructure before they can use the zone.

Water tile rendering uses `_Draw()` with **sine-wave color oscillation** plus animated ripple highlight lines:

```csharp
private void DrawWaterTile(Rect2 rect, int gridX, int gridY)
{
    float phase = _waterAnimTime * 2.0f + gridX * 0.7f + gridY * 0.5f;
    float r = 0.15f + 0.05f * Mathf.Sin(phase);
    float g = 0.35f + 0.08f * Mathf.Sin(phase + 1.0f);
    float b = 0.65f + 0.10f * Mathf.Sin(phase + 2.0f);
    DrawRect(rect, new Color(r, g, b));

    for (int i = 0; i < 3; i++)
    {
        float lineY = rect.Position.Y + rect.Size.Y * (0.25f + 0.25f * i);
        float wave = Mathf.Sin(_waterAnimTime * 1.5f + i * 1.2f + gridX) * 4f;
        DrawLine(
            new Vector2(rect.Position.X + 4, lineY + wave),
            new Vector2(rect.Position.X + rect.Size.X - 4, lineY - wave),
            new Color(0.5f, 0.7f, 0.9f, 0.3f), 1.5f);
    }
}
```

The Pond zone's `_Process` accumulates `_waterAnimTime` and calls `QueueRedraw()` continuously—acceptable since only the active zone processes. **SpawnSystem checks** `cell.Type == CellType.Water` when evaluating water-dependent insects, and water cells have their own `InsectSlots` for tracking water-surface visitors.

## 7. Zone unlock lives in ZoneManager with tab navigation

Zone unlock state belongs in **ZoneManager** (shown above) rather than a separate system—it's the single authority for all zone state. The unlock check combines a nectar *spend* (deducted on unlock) with a journal count *threshold* (not consumed). This dual requirement creates natural gameplay pacing: players need both economic progress and exploration progress.

**Zone navigation uses horizontal tab buttons** at the top of the screen. Locked zones are clickable but open an unlock requirements panel instead of switching:

```csharp
public partial class ZoneSelector : Control
{
    private Dictionary<ZoneId, Button> _tabs = new();

    private void BuildUI()
    {
        var hbox = new HBoxContainer { Position = new Vector2(10, 10) };
        AddChild(hbox);
        CreateTab(hbox, ZoneId.Starter, "🌱 Garden");
        CreateTab(hbox, ZoneId.Meadow, "🌻 Meadow");
        CreateTab(hbox, ZoneId.Pond, "🐸 Pond");
    }

    private void CreateTab(HBoxContainer parent, ZoneId id, string label)
    {
        var btn = new Button { Text = label, CustomMinimumSize = new Vector2(120, 40) };
        btn.Pressed += () => {
            if (ZoneManager.Instance.IsZoneUnlocked(id))
                ZoneManager.Instance.SwitchToZone(id);
            else ShowUnlockPanel(id);
        };
        parent.AddChild(btn);
        _tabs[id] = btn;
    }
}
```

The unlock panel shows ✅/❌ status for each requirement with current progress values, and the unlock button is only enabled when both conditions are met.

## 8. Economy tuned for 15-minute Meadow unlock with 2× ROI seeds

Drawing from Stardew Valley's early parsnip economy (**1.75× return on seed investment**) and Neko Atsume's "invest to attract" loop, the recommended economy gives common seeds a **2× ROI within 2–3 harvests** and targets Meadow unlock at **~15 minutes** of active play.

**Starting state**: Player receives **25 nectar**, enough for **4 common Starter seeds** at 5–7 each (spending ~24, keeping 1).

**Progression math with day-gated harvesting** (each game day ≈ 5 real minutes, plants produce 2–3 harvests per day):

- **Day 1 (0–5 min)**: Plant 4 seeds → 3 harvests × 4 plants × 3 nectar = **36 nectar** + day bonus (+2) + first journal entry (+5) = **~44 total**
- **Day 2 (5–10 min)**: Buy 4 more seeds (24 cost), 8 plants × 3 harvests × 3 = **72 nectar** + bonuses (~5) = **~93 total**
- **Day 3 (10–15 min)**: Passes **100 nectar** → **Meadow unlocks** ✓

After spending 100 on Meadow, the player restarts from ~0 with 8+ Starter plants still producing. With Meadow's 36 cells and access to Uncommon seeds (yield 5), earning another 150 for Pond takes roughly **15–20 more minutes**, hitting the **30-minute target**.

**Bonus nectar sources** accelerate progression by roughly **20%**:

- First journal entry: **+5 nectar** (one-time per species)
- 3-star photograph: **+3 nectar** (per unique photo)
- Day completion: **+2 nectar** (every game day)
- Subsequent journal entries: **+2 nectar** each

**Regrow times after harvest** must be short enough that harvesting always feels worthwhile:

| Rarity | Growth Time | Regrow Time | Regrow/Growth Ratio |
|--------|------------|-------------|---------------------|
| Common | 2 cycles (60s) | 1 cycle (30s) | 50% — harvest freely |
| Uncommon | 3 cycles (90s) | 1.5 cycles (45s) | 50% — manageable pause |
| Rare | 4–5 cycles (120–150s) | 2 cycles (60s) | 40% — plan around it |

The key design principle: **regrow should always be shorter than initial growth**. A player who has already waited to grow a plant should never feel punished for harvesting. With 4+ plants, staggering harvests across different plants creates an emergent strategy that feels clever rather than restrictive. A rare plant yielding **8–10 nectar** clearly outweighs **60 seconds** of missed insect attraction.

## 9. Nine new events wire Sprint 4 systems together

Sprint 4 requires these EventBus additions, all as C# record types:

```csharp
public record ZoneChangedEvent(ZoneId From, ZoneId To);
public record ZoneUnlockedEvent(ZoneId Zone);
public record SeedSelectedEvent(string? PlantId);
public record SeedPurchasedEvent(string PlantId, int Cost);
public record ShopOpenedEvent();
public record ShopClosedEvent();
public record WaterTilePlacedEvent(int X, int Y);
public record NectarChangedEvent(int OldAmount, int NewAmount);
public record PlantHarvestedEvent(string PlantId, int NectarEarned, int X, int Y);
```

**Subscriber matrix showing which systems react to each event:**

| Event | Key Subscribers |
|-------|----------------|
| ZoneChangedEvent | Camera (update limits), SpawnSystem (swap pool), HUD (zone indicator), SeedToolbar (rebuild slots) |
| ZoneUnlockedEvent | ZoneSelector (update tab states), HUD (celebration), ShopPanel (unlock new seeds) |
| SeedSelectedEvent | SeedCursor (show/hide preview), Garden (enable placement mode) |
| SeedPurchasedEvent | GameManager (deduct nectar → fires NectarChanged), SeedToolbar (update inventory) |
| NectarChangedEvent | HUD (animate counter), ShopPanel (recalculate affordability), ZoneSelector (check thresholds) |
| PlantHarvestedEvent | NectarManager (add yield → fires NectarChanged), SpawnSystem (remove plant's insects) |

The EventBus implementation adds a `static event Action<T>` and matching `Publish(T e)` method for each event type. Subscribers **must unsubscribe in `_ExitTree()`** to prevent memory leaks from static delegates holding node references.

## 10. Godot 4 C# technical patterns and performance

**ProcessMode is critical** for multi-zone performance. Setting `ProcessModeEnum.Disabled` on inactive zone roots stops all processing in their subtrees. But static C# EventBus handlers still fire—add guards:

```csharp
private void OnSomeEvent(SomeEvent e)
{
    if (ProcessMode == ProcessModeEnum.Disabled) return;
    // handle event
}
```

**QueueRedraw() best practices**: Call only when state changes, never unconditionally in `_Process()`. The one exception is water animation, which requires continuous redraws but only for the active Pond zone. Use property setters with dirty-checking to batch redraws efficiently:

```csharp
private int _highlightX = -1;
public int HighlightX {
    get => _highlightX;
    set { if (_highlightX == value) return; _highlightX = value; QueueRedraw(); }
}
```

**Three zones in memory is negligible.** A 6×6 grid of `CellData` objects (the largest zone) uses roughly **2 KB**. Even with all three zones loaded plus 25+ insect data records, total static data fits comfortably under **50 KB**. The performance concern is not memory but rather `_Draw()` call count—with 20+ plants and insects per zone, each calling `_Draw()`, keep draw operations simple (filled rects, circles, arcs) and avoid complex path drawing.

**Configurable Garden class** should accept grid dimensions from ZoneData rather than hardcoding 4×4. The Garden's `_Ready()` registers itself with ZoneManager, retrieves its ZoneData, and uses `GridWidth`/`GridHeight` for all loop bounds and draw calculations. Each zone gets its own Garden instance with an `[Export] ZoneId` property set in the scene tree.

## Conclusion

Sprint 4's architecture rests on three clean abstractions: **ZoneManager** as the single authority for zone state and transitions, **static registries** (PlantRegistry with 20 species, InsectRegistry with 25) as the canonical data source, and **EventBus records** as the glue connecting UI, economy, and gameplay systems. The economy targets a **2× seed ROI** with day-gated harvesting, putting Meadow at ~15 minutes and Pond at ~30 minutes—generous pacing that rewards engagement without grinding. The multi-tile White Birch plant, water-dependent insects, and tiered spawn conditions (MinInsectsPresent, AllNightPlants, MultiPlantCombo) create emergent complexity from simple, composable rules. Every system follows the project's established patterns: pure C# static EventBus, `_Draw()` placeholders, programmatic UI, and `_UnhandledInput()` for world interaction.