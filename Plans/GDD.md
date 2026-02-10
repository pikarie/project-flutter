# Project Flutter — Game Design Document
**Version:** 1.0
**Date:** 2026-02-08
**Engine:** Godot 4.x (C# / .NET 8)
**Developer:** Karianne (solo)
**Target platforms:** Windows (Steam), potential Linux/Mac
**Price point:** $4.99–$6.99
**Target playtime:** 3–6 hours to complete journal
**Languages:** English + French (UI + journal text only, no dialogue)

---

## 1. Vision

**One-sentence pitch:** Grow a hand-drawn garden to attract and photograph collectible insects in a cozy, no-pressure nature sim.

**Elevator pitch:** Project Flutter is a top-down garden game where you plant flowers and herbs to attract real-world insects — bees, butterflies, moths, dragonflies, fireflies. Each plant attracts specific species. You photograph insects to document them in a beautifully illustrated field journal. A day/night cycle transforms your garden: bees and butterflies visit by day, moths and fireflies emerge at night. Balance harvesting nectar for currency against keeping flowers blooming to attract rare species. Expand from a small starter plot to forest clearings, pond edges, and moonlit gardens. No dialogue, no story, no fail states — just the satisfaction of growing, discovering, and completing your collection.

**Core fantasy:** "I built this beautiful garden, and look what came to visit."

**Comparable games:**
- Neko Atsume (place items → creatures arrive → collect them)
- APICO / Mudborne (nature collection journal, discovery through experimentation)
- Stardew Valley (visual style reference, top-down slight angle)
- Viridi (plant caretaking, real-time growth)

---

## 2. Core Loop

```
PLANT seeds → GROW & TEND flowers → ATTRACT insects →
PHOTOGRAPH to document → EARN nectar → BUY new seeds/zones → REPEAT
```

**Session flow (15–30 min):**
1. Check garden — see what's blooming, what insects have arrived
2. Photograph any new/undocumented insects
3. Harvest nectar from select flowers (makes them less attractive temporarily)
4. Plant new seeds with earned currency
5. Wait/fast-forward for growth and new arrivals
6. Night falls — new insects appear, different flowers bloom
7. Photograph nocturnal species
8. End session or continue

---

## 3. Art Style

- **Hand-drawn digital illustration** (Paint Tool SAI, drawing tablet)
- **NOT pixel art** — smooth illustrated style
- **Top-down with slight angle** (Stardew Valley perspective, ~30° tilt)
- **Color palette:** Warm, natural tones. Greens, earth browns, flower colors pop against soil
- **Resolution:** TBD based on art pipeline tests (suggest 64x64 or 96x96 per grid tile)
- **Insect sprites:** Small but detailed enough to identify species. ~32x32 to 48x48 on the garden view
- **Journal illustrations:** Larger, more detailed versions of each insect for the journal entries (~256x256)
- **UI:** Clean, minimal. Wooden/natural frame aesthetic. Icons over text where possible

---

## 4. Game Mechanics

### 4.1 Garden Grid
- **Snap-to-grid** placement system with square tiles
- Each tile can hold: one plant, one infrastructure item (pond, path, decoration), or be empty (soil/grass)
- Plants occupy 1x1 tiles
- **No player character** — direct cursor interaction with the garden (click to plant, click to water, click to harvest)
- Custom cursor art (small trowel for planting, watering can for watering, camera for photographing)

### 4.2 Plant Growth
- Plants grow through **4 visual stages:** Seed → Sprout → Growing → Blooming
- Only **Blooming** plants attract insects
- Growth speed varies by plant type:
  - Common plants: ~1–2 day/night cycles to bloom
  - Uncommon: ~2–3 cycles
  - Rare: ~4–5 cycles
- Plants require **watering** to advance stages (simple click interaction)
- Blooming plants stay blooming indefinitely unless harvested for nectar
- **Harvested plants** revert to Growing stage and must re-bloom (takes ~1 cycle)
- Plants do NOT die from neglect (cozy, no-pressure design)
- Unwatered plants simply pause growth; they never wilt or disappear

### 4.3 Insect Spawning System
- Each blooming plant has a limited number of **insect slots** (1–3 depending on plant)
- Every few seconds (in game-time), the system checks each blooming plant and may spawn an appropriate insect if:
  - There is a free slot on the plant
  - The insect's conditions are met (time of day, required plants nearby, zone)
  - RNG roll passes the insect's rarity weight
- Insects **arrive gradually** and **depart after a set duration** (1–5 minutes real-time)
  - Common insects stay longer (~3–5 min), rare ones are fleeting (~30 sec–2 min)
- **Population cap** per garden zone prevents overcrowding (e.g., max 8–12 insects visible at once)
- When a player returns after being away, the garden is at natural capacity — not flooded with 200 insects
- Rare insects have lower spawn chance AND shorter visit duration = must be alert to photograph them

### 4.4 Photography Mechanic
- Switch to **photo mode** (toggle or hold a key/button)
- **Click and hold** on an insect to begin focusing
- A **concentric circle closes** around the insect (1–2 second duration)
- The insect **continues to move** during focus (idle animations: walking on petals, fluttering wings, brief flights)
- When the circle fully closes: **shutter sound + brief white flash + insect freezes momentarily**
- **Photo quality** rating based on how centered the insect was when the shutter triggered:
  - ★☆☆ — insect was near the edge (documented, but basic entry)
  - ★★☆ — insect was reasonably centered
  - ★★★ — perfect center (unlocks bonus journal detail, fun fact, or alternate illustration)
- First successful photo of a species = **new journal entry unlocked** (main reward)
- Can re-photograph for better star rating at any time
- **Behavior patterns by species affect difficulty:**
  - Bees: predictable, stay on flower, pause often → easy to photograph
  - Butterflies: flutter between flowers, pause briefly on each → medium
  - Moths: erratic flight at night, attracted to light → medium-hard
  - Dragonflies: fast, hover briefly then zoom away → hard
  - Fireflies: only visible during flash pattern → timing-based

### 4.5 Field Journal
- Central collectathon interface accessible via a book icon
- **Grid of entries**: discovered species show illustrated portraits; undiscovered show grey silhouettes
- Each entry contains:
  - Species name
  - Hand-drawn illustration (large, detailed)
  - Star rating from best photo taken (★☆☆ to ★★★)
  - Flavor text / real-world fun fact
  - Habitat hint ("Found near lavender during daytime")
  - First discovery date
- **Discovery hints** for undiscovered species: vague clues appear when you unlock adjacent species or reach certain journal milestones
  - Example: "Something shimmers near water at dusk..."
- **Completion tracker**: "17/25 species documented" with percentage
- **Completing the journal 100%** = final reward (TBD: special visual effect on garden, legendary insect appearance, credits scene)

### 4.6 Currency & Economy
- **Currency: Nectar** (universal currency)
- Earned by: harvesting blooming flowers (click on a blooming plant → collect nectar → plant reverts to Growing stage)
- **Core tension:** Harvesting gives you nectar to buy new seeds and unlock zones, BUT the plant stops blooming temporarily = fewer insects visit = harder to photograph
- Nectar costs for purchases:
  - Common seed packet: 5–10 nectar
  - Uncommon seed packet: 20–30 nectar
  - Rare seed packet: 50–75 nectar
  - Zone unlock: 100–200 nectar
  - Infrastructure (pond, lamp): 50–100 nectar
- **No real-money transactions.** Nectar is the only currency.
- Economy should feel **generous, not grindy** — a full garden of blooming flowers produces enough nectar passively (from periodic bonus drops?) to slowly progress even without active harvesting. Active harvesting just speeds things up.

### 4.7 Day/Night Cycle
- **Configurable cycle duration** (default: ~5 minutes real-time per full day/night, adjustable via config variable `DayCycleDuration`)
- **Fast-forward button** (x1, x2, x3 speed) for waiting periods
- **Visual transition:** CanvasModulate with smooth color tweening (Tween, sine easing)
- **Time periods:** night → dawn → morning → noon → golden_hour → dusk → night
- **Gameplay impact:**
  - **Daytime insects:** bees, butterflies, ladybugs, dragonflies
  - **Nighttime insects:** moths, fireflies, crickets, luna moth
  - **Dawn/dusk overlap:** some species appear during transitions
  - **Night-blooming plants** only open at night (moonflower, evening primrose)
  - Day-blooming plants close at night (but don't lose progress)
- **Ambient audio shifts:** birdsong → crickets/frogs → dawn chorus

### 4.8 Zone Progression
- **Zone 1 — Starter Garden (4x4 grid)**
  - Unlocked from start
  - Basic flowers: lavender, sunflower, daisy, coneflower
  - Common insects: honeybee, bumblebee, cabbage white butterfly, ladybug

- **Zone 2 — Meadow (6x6 grid)**
  - Unlock cost: 100 nectar + 5 journal entries
  - Introduces: milkweed, wildflowers, goldenrod, dill
  - New insects: monarch butterfly, swallowtail, grasshopper, hoverfly

- **Zone 3 — Pond Edge (5x5 grid + water tiles)**
  - Unlock cost: 150 nectar + 12 journal entries
  - Introduces: water lily, cattail, iris, pond infrastructure
  - New insects: dragonfly, damselfly, water strider, pond skater

- Night-blooming plants and nocturnal insects appear in any zone during the night cycle
- Player can switch between zones freely once unlocked
- Each zone has unique background art and ambient sounds
- Some insects can ONLY appear in specific zones

---

## 5. Content List

### 5.1 Plants (20 total)

| # | Plant | Zone | Rarity | Attracts (primary) | Night? |
|---|-------|------|--------|-------------------|--------|
| 1 | Lavender | Starter | Common | Honeybee, Bumblebee | No |
| 2 | Sunflower | Starter | Common | Multiple bees, Ladybug | No |
| 3 | Daisy | Starter | Common | Cabbage White Butterfly | No |
| 4 | Coneflower | Starter | Common | Multiple butterflies, bees | No |
| 5 | Marigold | Starter | Common | Ladybug, Hoverfly | No |
| 6 | Milkweed | Meadow | Uncommon | Monarch Butterfly (exclusive) | No |
| 7 | Dill | Meadow | Uncommon | Swallowtail, Ladybug | No |
| 8 | Goldenrod | Meadow | Uncommon | Many species (universal attractor) | No |
| 9 | Wildflower Mix | Meadow | Common | Grasshopper, misc. butterflies | No |
| 10 | Black-Eyed Susan | Meadow | Uncommon | Hoverfly, bees | No |
| 11 | Water Lily | Pond | Uncommon | Dragonfly, Damselfly | No |
| 12 | Cattail | Pond | Common | Dragonfly, Water Strider | No |
| 13 | Iris | Pond | Uncommon | Damselfly, butterflies | No |
| 14 | Lotus | Pond | Rare | Dragonfly (rare variant) | No |
| 15 | Passionflower | Pond | Rare | Gulf Fritillary (exclusive) | No |
| 16 | Moonflower | Starter | Uncommon | Sphinx Moth (exclusive) | Yes |
| 17 | Evening Primrose | Starter | Uncommon | Moths, Fireflies | Yes |
| 18 | Night-Blooming Jasmine | Meadow | Rare | Luna Moth | Yes |
| 19 | White Birch (2x2) | Meadow | Rare | Luna Moth (required pair with Jasmine) | Yes |
| 20 | Switchgrass | Pond | Common | Crickets, Fireflies | Yes |

### 5.2 Insects (25 total)

| # | Insect | Zone | Rarity | Time | Required Plants | Photo Difficulty |
|---|--------|------|--------|------|----------------|-----------------|
| 1 | Honeybee | Starter | Common | Day | Lavender, Sunflower, any flower | Easy |
| 2 | Bumblebee | Starter | Common | Day | Lavender, Coneflower | Easy |
| 3 | Cabbage White Butterfly | Starter | Common | Day | Daisy, any flower | Easy |
| 4 | Ladybug | Starter | Common | Day | Sunflower, Dill, Marigold | Easy |
| 5 | Garden Spider | Starter | Uncommon | Day | Any 3+ blooming plants | Medium |
| 6 | Monarch Butterfly | Meadow | Uncommon | Day | Milkweed (exclusive) | Medium |
| 7 | Swallowtail Butterfly | Meadow | Uncommon | Day | Dill, Coneflower | Medium |
| 8 | Hoverfly | Meadow | Common | Day | Goldenrod, Black-Eyed Susan | Easy |
| 9 | Grasshopper | Meadow | Common | Day | Wildflower Mix, Switchgrass | Medium |
| 10 | Painted Lady Butterfly | Meadow | Uncommon | Day | Goldenrod, Coneflower | Medium |
| 11 | Praying Mantis | Meadow | Rare | Day | 5+ insects present in zone | Hard |
| 12 | Dragonfly | Pond | Uncommon | Day | Water Lily, Cattail | Hard |
| 13 | Damselfly | Pond | Uncommon | Day | Iris, Water Lily | Medium |
| 14 | Water Strider | Pond | Common | Day | Cattail (+ water tiles) | Medium |
| 15 | Pond Skater | Pond | Common | Both | Water tiles present | Easy |
| 16 | Gulf Fritillary | Pond | Rare | Day | Passionflower (exclusive) | Hard |
| 17 | Emperor Dragonfly | Pond | Rare | Day | Lotus + 2 water tiles | Hard |
| 18 | Sphinx Moth | Starter | Uncommon | Night | Moonflower (exclusive) | Medium |
| 19 | Firefly | Pond | Uncommon | Night | Evening Primrose + Switchgrass | Medium (timing) |
| 20 | Cricket | Pond | Common | Night | Switchgrass, any grass | Easy (audio first) |
| 21 | Luna Moth | Meadow | Rare | Night | Night-Blooming Jasmine + White Birch | Hard |
| 22 | Owl Moth | Starter | Uncommon | Night | Any night-blooming flower | Medium |
| 23 | Atlas Moth | Meadow | Very Rare | Night | All 4 night plants blooming | Very Hard |
| 24 | Jewel Beetle | Meadow | Rare | Day | Sunflower + Goldenrod cluster | Hard |
| 25 | Monarch Migration | Meadow | Very Rare | Day | 3+ Milkweed + Goldenrod (event) | Special |

**Rarity distribution:** 6 Common / 9 Uncommon / 7 Rare / 3 Very Rare

### 5.3 Art Asset List (Estimated)

| Asset Type | Count | Size | Notes |
|------------|-------|------|-------|
| Plant growth stages (4 per plant × 20) | 80 | ~64x64 | Core art workload |
| Insect garden sprites (25) | 25 | ~32x48 | Small, animated (2-4 frames idle) |
| Insect journal illustrations (25) | 25 | ~256x256 | Detailed, the "reward" art |
| Insect silhouettes (25) | 25 | ~256x256 | Grey versions of journal art |
| Zone backgrounds (3) | 3 | Full screen | Starter, Meadow, Pond |
| Tile sprites (soil, grass, water, path) | ~15 | ~64x64 | Reusable across zones |
| UI elements (buttons, frames, icons) | ~20 | Various | Journal book, seed shop, etc. |
| Cursor art (3 modes) | 3 | ~32x32 | Trowel, watering can, camera |
| Photography circle animation | 1 | Programmatic | Concentric circle + flash effect |
| **Total unique art assets** | **~198** | | |

---

## 6. Audio Design

### 6.1 Music
- **ASMR-inspired ambient soundscapes** rather than traditional game music
- **Layered system:** base ambient + insect layers that activate based on garden contents
- Day: soft wind, distant birdsong, gentle acoustic guitar loops
- Night: crickets, frog chorus, owl hoots, soft piano/harp
- Transition: gradual crossfade between day and night layers (during dusk/dawn)

### 6.2 Sound Effects
- **Garden interaction:** satisfying dirt sounds for planting, water splashing for watering, soft pop for harvesting nectar
- **Photography:** mechanical shutter click, lens focus whir, film advance sound
- **Insects:** species-specific ambient sounds (bee buzz, cricket chirp, firefly twinkle)
- **UI:** soft wooden clicks for buttons, page turn for journal, gentle chime for new discovery
- **Discovery fanfare:** special jingle when a new species is documented for the first time

### 6.3 Audio Sources
- Free/affordable nature sound banks (freesound.org, Epidemic Sound, etc.)
- Consider: simple original compositions or licensed ambient tracks
- Priority: get placeholder sounds in early, polish/replace later

---

## 7. Technical Architecture (Godot)

### 7.1 Project Structure
```
project-flutter/
├── scenes/
│   ├── main/
│   │   ├── Main.tscn              # Main game scene, manages zone switching
│   │   └── Main.cs
│   ├── garden/
│   │   ├── Garden.tscn            # Garden grid scene (TileMapLayer + Dictionary state)
│   │   ├── GardenGrid.cs          # Grid manager: TileMapLayer rendering + CellState dictionary
│   │   └── GardenCamera.cs        # Camera with zoom/pan
│   ├── plants/
│   │   ├── Plant.tscn             # Base plant scene
│   │   └── Plant.cs               # Growth logic, watering, harvesting
│   ├── insects/
│   │   ├── Insect.tscn            # Base insect scene
│   │   └── Insect.cs              # Movement, behavior patterns, slot system
│   ├── photography/
│   │   ├── PhotoMode.tscn         # Photo circle overlay
│   │   └── PhotoMode.cs           # Focus mechanic, quality calculation
│   └── ui/
│       ├── Journal.tscn           # Field journal interface
│       ├── SeedShop.tscn          # Seed purchasing UI
│       ├── HUD.tscn               # Nectar counter, time display, zone selector
│       └── MainMenu.tscn          # Title screen, settings, load game
├── resources/
│   ├── plant_data/                # .tres Resource files for each plant
│   ├── insect_data/               # .tres Resource files for each insect
│   └── zone_data/                 # .tres Resource files for zone configs
├── scripts/
│   ├── autoload/
│   │   ├── GameManager.cs         # Global state, save/load
│   │   ├── TimeManager.cs         # Day/night cycle, speed control
│   │   ├── JournalManager.cs      # Discovery tracking, completion %
│   │   ├── EventBus.cs            # Pure static C# event bus (Subscribe/Publish/Unsubscribe)
│   │   └── Events.cs             # All event record types
│   ├── data/
│   │   ├── PlantData.cs           # Plant Resource class
│   │   ├── InsectData.cs          # Insect Resource class
│   │   ├── ZoneType.cs            # Zone enum (Starter, Meadow, Pond)
│   │   ├── MovementPattern.cs     # Movement enum (Hover, Flutter, Crawl, Erratic)
│   │   └── CellState.cs           # Per-cell garden state + insect slot tracking
│   └── systems/
│       ├── SpawnSystem.cs         # Insect spawn logic, slot management
│       ├── IMovementBehavior.cs   # Movement interface + factory
│       ├── HoverBehavior.cs       # Noise-based hovering (bee)
│       ├── FlutterBehavior.cs     # Sine-wave path (butterfly)
│       ├── CrawlBehavior.cs       # Elliptical crawl (ladybug)
│       ├── ErraticBehavior.cs     # Random with tether (moth)
│       ├── NectarEconomy.cs       # Currency management
│       └── PhotoSystem.cs         # Photo quality calculation
├── art/
│   ├── plants/                    # Growth stage sprites
│   ├── insects/                   # Garden sprites + journal illustrations
│   ├── tiles/                     # Ground tiles, water, paths
│   ├── ui/                        # Interface elements
│   └── backgrounds/               # Zone backgrounds
├── audio/
│   ├── music/                     # Ambient loops (day, night, transition)
│   ├── sfx/                       # Sound effects
│   └── insects/                   # Per-species ambient sounds
└── localization/
    ├── en.csv                     # English strings
    └── fr.csv                     # French strings
```

### 7.2 Architecture: 3-Layer Pattern
- **TileMapLayer** for visual rendering (ground tiles, soil, water)
- **Dictionary<Vector2I, CellState>** for per-cell game state (what's planted, growth stage, watered, insect slots)
- **Scene instances** (Plant.tscn, Insect.tscn) for entities with their own logic
- **CanvasModulate** for day/night visual tinting (UI on separate CanvasLayer)
- **`_UnhandledInput()`** for all game-world clicks (prevents UI click-through)
- **Pure C# EventBus** for decoupled cross-system communication (not Godot signals)
- **Strategy pattern** for insect movement (IMovementBehavior interface + factory)
- **Enum FSM** for insect lifecycle (Arriving → Visiting → Departing → Freed)

### 7.3 Key Data-Driven Design
- **All plant and insect data defined in Resource files (.tres)** — no hardcoding
- Adding a new plant or insect = create a new .tres file + add art assets
- This makes post-launch content additions trivial

### 7.3 PlantData Resource Example
```csharp
[GlobalClass]
public partial class PlantData : Resource
{
    [Export] public string Id { get; set; }                    // "lavender"
    [Export] public string DisplayName { get; set; }           // "Lavender"
    [Export] public ZoneType Zone { get; set; }                // ZoneType.Starter
    [Export] public string Rarity { get; set; }                // "common"
    [Export] public int SeedCost { get; set; }                 // 5
    [Export] public int NectarYield { get; set; }              // 3
    [Export] public int GrowthCycles { get; set; }             // 1
    [Export] public int InsectSlots { get; set; }              // 2
    [Export] public bool NightBlooming { get; set; }           // false
    [Export] public Texture2D[] GrowthSprites { get; set; }    // 4 stages
    [Export] public string[] AttractedInsects { get; set; }    // ["honeybee", "bumblebee"]
}
```

### 7.4 InsectData Resource Example
```csharp
[GlobalClass]
public partial class InsectData : Resource
{
    [Export] public string Id { get; set; }                    // "monarch_butterfly"
    [Export] public string DisplayName { get; set; }           // "Monarch Butterfly"
    [Export] public ZoneType Zone { get; set; }                // ZoneType.Meadow
    [Export] public string Rarity { get; set; }                // "uncommon"
    [Export] public string TimeOfDay { get; set; }             // "day" / "night" / "both"
    [Export] public string[] RequiredPlants { get; set; }      // ["milkweed"]
    [Export] public float SpawnWeight { get; set; }            // 0.3 (lower = rarer)
    [Export] public float VisitDurationMin { get; set; }       // 60.0 (game-time seconds)
    [Export] public float VisitDurationMax { get; set; }       // 180.0
    [Export] public string PhotoDifficulty { get; set; }       // "medium"
    [Export] public MovementPattern MovementPattern { get; set; } // MovementPattern.Flutter
    [Export] public float MovementSpeed { get; set; }          // 30.0 (pixels/sec)
    [Export] public float PauseFrequency { get; set; }         // 0.4 (chance to pause each cycle)
    [Export] public float PauseDuration { get; set; }          // 2.0 (seconds)
    [Export] public SpriteFrames GardenSprite { get; set; }    // Animated sprite for garden
    [Export] public Texture2D JournalIllustration { get; set; } // Large detailed art
    [Export] public Texture2D JournalSilhouette { get; set; }   // Grey silhouette
    [Export] public string JournalText { get; set; }           // Fun fact text
    [Export] public string HintText { get; set; }              // Discovery hint
    [Export] public AudioStream AmbientSound { get; set; }     // Bee buzz, cricket chirp, etc.
}
```

### 7.5 Save System
- JSON-based save file
- Saves: planted plants per zone (position, growth stage), journal entries (discovered, star ratings), nectar balance, unlocked zones, unlocked seeds, play time, settings
- Auto-save every N minutes + save on quit
- Single save slot (keep it simple)

---

## 8. Development Sprints

**Based on ~20h/week (1-2h weeknights + 10-15h weekends)**

### Sprint 1 — Core Grid & Planting (Week 1-2, ~40h)
- [ ] Godot project setup, folder structure, autoloads
- [ ] Garden grid system (4x4 TileMap or Node2D grid)
- [ ] Tile interaction (click to select, plant, remove)
- [ ] Plant scene with 4 growth stages (use placeholder art)
- [ ] Watering mechanic (click blooming plant → advance stage)
- [ ] Basic day/night cycle (visual tint change + time variable)
- [ ] Fast-forward button (x1, x2, x3)
- **Deliverable:** Can place plants, watch them grow, see day/night change

### Sprint 2 — Insects & Spawning (Week 3-4, ~40h)
- [ ] Insect base scene with movement patterns (flutter, hover, crawl)
- [ ] Spawn system: check blooming plants → roll for insects → spawn in slots
- [ ] Insect departure after visit duration
- [ ] Population cap per zone
- [ ] 3-4 test insects with different movement behaviors
- [ ] Basic insect-plant attraction matching from data resources
- **Deliverable:** Insects arrive and leave based on what's planted

### Sprint 3 — Photography & Journal (Week 5-6, ~40h)
- [ ] Photo mode toggle
- [ ] Concentric circle focus mechanic (click & hold)
- [ ] Quality rating calculation (distance from center)
- [ ] Shutter sound + flash effect
- [ ] Journal UI (grid of entries, silhouettes, discovered entries)
- [ ] Journal entry detail view (illustration, name, fun fact, stars)
- [ ] Discovery tracking (JournalManager autoload)
- [ ] New discovery notification/fanfare
- **Deliverable:** Full photograph → journal → collection loop working

### Sprint 4 — Economy & Zones (Week 7-8, ~40h)
- [ ] Nectar currency system (harvest flowers, earn nectar)
- [ ] Seed shop UI (buy seeds with nectar)
- [ ] Zone unlock system (nectar cost + journal entry requirements)
- [ ] Build all 3 zones with proper backgrounds
- [ ] Zone switching UI/navigation
- [ ] Pond zone water tiles (special infrastructure)
- **Deliverable:** Full progression loop from starter to all zones

### Sprint 5 — Content & Art (Week 9-10, ~40h)
- [ ] Final art for all 20 plants (80 growth sprites)
- [ ] Final art for all 25 insects (garden sprites + journal illustrations)
- [ ] All insect data resources configured and balanced
- [ ] Discovery hints for all species
- [ ] Journal text (EN + FR) for all entries
- [ ] Localization system setup (CSV-based)
- **Deliverable:** All content in-game, playable start to finish

### Sprint 6 — Polish & Ship (Week 11-12, ~40h)
- [ ] Sound design: all SFX, ambient layers, shutter sounds
- [ ] Music/ambient audio: day loop, night loop, transitions
- [ ] Main menu, settings screen (volume, language, fullscreen)
- [ ] Save/load system
- [ ] Tutorial hints (first-time contextual tooltips)
- [ ] Journal completion reward (100% special event)
- [ ] Steam integration (achievements, store page assets)
- [ ] Playtesting, balancing spawn rates and economy
- [ ] Bug fixing
- **Deliverable:** Ship-ready build

### Stretch Goals (Post-launch or if ahead of schedule)
- [ ] Steam achievements (first photo, complete zone, 100% journal, all 3-star photos)
- [ ] Seasonal events (monarch migration, firefly season)
- [ ] Photo album feature (save favorite photos)
- [ ] Memento/gift system (insects leave behind collectible items)
- [ ] Additional zones (desert garden, tropical greenhouse)
- [ ] Controller support

---

## 9. Scope Rules (TAPE TO MONITOR)

1. **20 plants. 25 insects. 3 zones. No more at launch.**
2. **No dialogue. No story. No NPCs.** Just garden, journal, and insects.
3. **No multiplayer.** Single-player only.
4. **No procedural generation.** Fixed zones, fixed grid sizes.
5. **If a feature takes more than 1 day to implement, question if it's needed for v1.0.**
6. **Art is the bottleneck.** Prioritize programming systems first with placeholder art, then replace with final art in Sprint 5.
7. **Playtest the core loop by end of Sprint 3.** If plant → insect → photo → journal doesn't feel good by week 6, simplify before adding more content.
8. **Ship imperfect.** A finished 80% game beats an unfinished 100% game every time.

---

## 10. Marketing Milestones

| When | Action |
|------|--------|
| Week 1 | Register Steamworks account, pay $100 fee (30-day timer starts) |
| Week 3 | First GIF of garden + insect on r/IndieDev, r/indiegames |
| Week 5 | Steam "Coming Soon" page live (minimum 2 weeks before launch) |
| Week 6 | Second GIF showing photo mechanic + journal |
| Week 8 | Post on niche subreddits: r/gardening, r/insects, r/entomology |
| Week 10 | Send demo/keys to cozy game content creators |
| Week 11 | Launch trailer (30 seconds, show the loop) |
| Week 12 | **LAUNCH** on Steam |
| Post-launch | Submit to Wholesome Games, post on r/cozygaming |

---

## 11. Config Variables (Easy to Tweak)

```csharp
// TimeManager.cs
public const float DayCycleDuration = 300.0f;    // seconds per full cycle (5 min default)
// Time periods: night (<5.5h), dawn (5.5-7h), morning (7-10h),
//               noon (10-14h), golden_hour (14-17h), dusk (17-19.5h), night (>19.5h)

// SpawnSystem.cs
public const float SpawnCheckInterval = 5.0f;     // game-time seconds between spawn attempts
public const int MaxInsectsPerZone = 10;           // population cap
public const int MaxInsectSlotsPerPlant = 2;       // default insect slots per blooming plant
// 40% quiet ticks (no spawn) for anticipation pacing

// NectarEconomy.cs
public const int HarvestNectarBase = 3;            // nectar per common plant harvest
public const int RegrowCycles = 1;                 // cycles to re-bloom after harvest

// PhotoSystem.cs
public const float FocusDuration = 1.5f;           // seconds to close the circle
public const float ThreeStarRadius = 0.15f;        // % of circle radius for perfect shot
public const float TwoStarRadius = 0.40f;          // % for 2-star
```

---

*This document is the single source of truth for Project Flutter. When in doubt, refer here. When scope creeps, re-read Section 9.*
