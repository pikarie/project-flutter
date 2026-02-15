using System.Collections.Generic;
using System.Linq;

namespace ProjectFlutter;

public static class InsectRegistry
{
	private static List<InsectData> _allSpecies;

	public static IReadOnlyList<InsectData> AllSpecies =>
		_allSpecies ??= CreateSpeciesList();

	public static int TotalSpeciesCount => AllSpecies.Count;

	public static InsectData GetById(string id) =>
		AllSpecies.FirstOrDefault(species => species.Id == id);

	private static List<InsectData> CreateSpeciesList() => new()
	{
		// ── Starter Zone — Day (11 species) ─────────────────────────

		CreateInsect("cabbage_white", "Cabbage White", ZoneType.Starter, "common",
			MovementPattern.Flutter, "day", 0.8f, 60f, 180f, 40f,
			"A delicate butterfly with pristine white wings, often the first visitor to a new garden.",
			"Flutters around lavender and daisies in daylight.",
			requiredPlants: new[] { "lavender" }),

		CreateInsect("orange_tip", "Orange Tip", ZoneType.Starter, "common",
			MovementPattern.Flutter, "day", 0.8f, 60f, 180f, 38f,
			"Males sport vivid orange wingtips while females are pure white, a spring herald.",
			"Visits daisies and marigolds on sunny days.",
			requiredPlants: new[] { "daisy" }),

		CreateInsect("red_admiral", "Red Admiral", ZoneType.Starter, "uncommon",
			MovementPattern.Erratic, "day", 0.5f, 50f, 150f, 45f,
			"A striking butterfly with bold red bands that migrates thousands of kilometers.",
			"Drawn to lavender with erratic, powerful flight.",
			requiredPlants: new[] { "lavender" }),

		CreateInsect("seven_spot_ladybug", "Seven-Spot Ladybug", ZoneType.Starter, "common",
			MovementPattern.Crawl, "day", 0.8f, 120f, 300f, 15f,
			"The classic red-and-black garden guardian, devouring aphids by the hundreds.",
			"Crawls on any blooming plant during the day."),

		CreateInsect("twenty_two_spot_ladybug", "22-Spot Ladybug", ZoneType.Starter, "common",
			MovementPattern.Crawl, "day", 0.8f, 120f, 300f, 14f,
			"A tiny bright yellow ladybug with exactly 22 spots, feeding on mildew rather than aphids.",
			"Crawls on any blooming plant during the day."),

		CreateInsect("golden_tortoise_beetle", "Golden Tortoise Beetle", ZoneType.Starter, "uncommon",
			MovementPattern.Crawl, "day", 0.4f, 80f, 200f, 12f,
			"A living jewel that can shift its color from gold to reddish-brown when disturbed.",
			"Found on sunflowers in gardens.",
			requiredPlants: new[] { "sunflower" }),

		CreateInsect("rose_chafer", "Rose Chafer", ZoneType.Starter, "common",
			MovementPattern.Crawl, "day", 0.7f, 100f, 260f, 16f,
			"A metallic green beetle that loves to bask on petals in the warm sun.",
			"Visits daisies and marigolds.",
			requiredPlants: new[] { "daisy" }),

		CreateInsect("japanese_beetle", "Japanese Beetle", ZoneType.Starter, "common",
			MovementPattern.Crawl, "day", 0.7f, 100f, 260f, 15f,
			"An iridescent copper-green beetle, beautiful but voracious in its appetite.",
			"Attracted to sunflowers and lavender.",
			requiredPlants: new[] { "sunflower" }),

		CreateInsect("marmalade_hoverfly", "Marmalade Hoverfly", ZoneType.Starter, "common",
			MovementPattern.Hover, "day", 0.8f, 70f, 200f, 35f,
			"A bee mimic with orange-banded abdomen, capable of hovering perfectly still in midair.",
			"Hovers near daisies and marigolds.",
			requiredPlants: new[] { "daisy" }),

		CreateInsect("green_lacewing", "Green Lacewing", ZoneType.Starter, "uncommon",
			MovementPattern.Hover, "both", 0.5f, 50f, 150f, 30f,
			"Delicate transparent wings shimmer green at twilight as this beneficial predator hunts.",
			"Appears at dawn and dusk near flowering plants."),

		CreateInsect("european_mantis", "European Mantis", ZoneType.Starter, "uncommon",
			MovementPattern.Crawl, "day", 0.3f, 60f, 180f, 8f,
			"A patient ambush predator that holds perfectly still before striking with lightning speed.",
			"Only appears in gardens with multiple blooming plants.",
			requiredPlants: new[] { "lavender", "sunflower", "daisy" }),

		// ── Starter Zone — Night (2 species) ────────────────────────

		CreateInsect("rosy_maple_moth", "Rosy Maple Moth", ZoneType.Starter, "common",
			MovementPattern.Hover, "night", 0.7f, 80f, 200f, 30f,
			"A cotton-candy-colored moth in pink and yellow, like a tiny plush toy come to life.",
			"Visits evening primrose after dark.",
			requiredPlants: new[] { "evening_primrose" }),

		CreateInsect("garden_tiger_moth", "Garden Tiger Moth", ZoneType.Starter, "common",
			MovementPattern.Hover, "night", 0.7f, 80f, 200f, 32f,
			"Bold orange hindwings flash as a warning to predators — this moth is toxic.",
			"Drawn to evening primrose at night.",
			requiredPlants: new[] { "evening_primrose" }),

		// ── Starter Zone — Day (1 more) ─────────────────────────────

		CreateInsect("western_honeybee", "Western Honeybee", ZoneType.Starter, "common",
			MovementPattern.Hover, "day", 0.9f, 90f, 240f, 30f,
			"A tireless pollinator that dances to communicate flower locations to its hive.",
			"Buzzes around sunflowers and marigolds in daylight.",
			requiredPlants: new[] { "sunflower" }),

		// ── Meadow Zone — Day (8 species) ───────────────────────────

		CreateInsect("clouded_sulphur", "Clouded Sulphur", ZoneType.Meadow, "common",
			MovementPattern.Flutter, "day", 0.8f, 60f, 180f, 42f,
			"A pale yellow butterfly that flutters low over meadow flowers in gentle arcs.",
			"Visits clover and bluebells in meadows.",
			requiredPlants: new[] { "clover" }),

		CreateInsect("common_blue", "Common Blue", ZoneType.Meadow, "common",
			MovementPattern.Flutter, "day", 0.8f, 60f, 180f, 40f,
			"Tiny wings of sky blue that fold to reveal intricate spotted undersides.",
			"Found on clover and bluebells.",
			requiredPlants: new[] { "clover" }),

		CreateInsect("monarch", "Monarch", ZoneType.Meadow, "uncommon",
			MovementPattern.Flutter, "day", 0.5f, 50f, 150f, 45f,
			"Famous for its incredible multi-generational migration spanning thousands of kilometers.",
			"Exclusively visits milkweed — its only larval food plant.",
			requiredPlants: new[] { "milkweed" }),

		CreateInsect("gulf_fritillary", "Gulf Fritillary", ZoneType.Meadow, "uncommon",
			MovementPattern.Erratic, "day", 0.4f, 50f, 140f, 46f,
			"A vibrant orange butterfly with silver-spotted underwings that catch the light.",
			"Passes through meadows during migration."),

		CreateInsect("old_world_swallowtail", "Old World Swallowtail", ZoneType.Meadow, "rare",
			MovementPattern.Erratic, "day", 0.2f, 30f, 100f, 50f,
			"Elegant tail-like extensions on its hindwings make it unmistakable in powerful flight.",
			"Attracted to goldenrod and thistle together.",
			requiredPlants: new[] { "goldenrod", "thistle" }),

		CreateInsect("wool_carder_bee", "Wool Carder Bee", ZoneType.Meadow, "uncommon",
			MovementPattern.Erratic, "day", 0.5f, 60f, 180f, 35f,
			"Named for scraping plant fibers to line its nest, this bee aggressively guards flower patches.",
			"Territories around clover and bluebells.",
			requiredPlants: new[] { "clover" }),

		CreateInsect("long_horned_bee", "Long-Horned Bee", ZoneType.Meadow, "uncommon",
			MovementPattern.Erratic, "day", 0.5f, 60f, 180f, 33f,
			"Males sport absurdly long antennae used to detect female pheromones at great distances.",
			"Visits goldenrod and thistle.",
			requiredPlants: new[] { "goldenrod" }),

		CreateInsect("meadow_grasshopper", "Meadow Grasshopper", ZoneType.Meadow, "common",
			MovementPattern.Crawl, "day", 0.7f, 80f, 220f, 18f,
			"A powerful jumper that creates a distinctive chirping sound by rubbing its legs together.",
			"Found among meadow wildflowers."),

		// ── Meadow Zone — Day continued (2 more) ────────────────────

		CreateInsect("band_winged_grasshopper", "Band-Winged Grasshopper", ZoneType.Meadow, "uncommon",
			MovementPattern.Crawl, "day", 0.4f, 70f, 200f, 20f,
			"Flashes colorful hindwings in flight as a startling defense, then vanishes when it lands.",
			"Hops through meadow vegetation."),

		CreateInsect("six_spotted_tiger_beetle", "Six-Spotted Tiger Beetle", ZoneType.Meadow, "uncommon",
			MovementPattern.Erratic, "day", 0.4f, 50f, 150f, 55f,
			"A brilliant metallic green predator that sprints, stops, and sprints again to hunt.",
			"Darts across open meadow ground."),

		// ── Meadow Zone — Night (1 species) ─────────────────────────

		CreateInsect("cinnabar_moth", "Cinnabar Moth", ZoneType.Meadow, "common",
			MovementPattern.Hover, "night", 0.7f, 80f, 200f, 28f,
			"Crimson and black wings warn predators of the toxins absorbed from ragwort as a caterpillar.",
			"Appears near ragwort at night.",
			requiredPlants: new[] { "ragwort" }),

		// ── Forest Zone — Day (6 species) ───────────────────────────

		CreateInsect("eastern_tiger_swallowtail", "Eastern Tiger Swallowtail", ZoneType.Forest, "uncommon",
			MovementPattern.Flutter, "day", 0.5f, 50f, 150f, 44f,
			"Bold yellow wings with black tiger stripes glide through dappled forest light.",
			"Flutters through forest glades."),

		CreateInsect("question_mark_butterfly", "Question Mark", ZoneType.Forest, "rare",
			MovementPattern.Erratic, "day", 0.2f, 30f, 100f, 48f,
			"Named for the tiny silver question mark on its underwing — a forest mystery.",
			"Requires violets in the forest understory.",
			requiredPlants: new[] { "violet" }),

		CreateInsect("ebony_jewelwing", "Ebony Jewelwing", ZoneType.Forest, "uncommon",
			MovementPattern.Hover, "day", 0.5f, 50f, 150f, 40f,
			"A damselfly with jet-black wings that flutters like a dark butterfly along forest streams.",
			"Found near moist forest areas."),

		CreateInsect("annual_cicada", "Annual Cicada", ZoneType.Forest, "uncommon",
			MovementPattern.Crawl, "day", 0.5f, 60f, 180f, 12f,
			"Its buzzing chorus is the unmistakable sound of summer in the forest canopy.",
			"Heard before seen in the forest."),

		CreateInsect("spotted_lanternfly", "Spotted Lanternfly", ZoneType.Forest, "common",
			MovementPattern.Crawl, "day", 0.7f, 80f, 220f, 15f,
			"Strikingly spotted forewings hide vivid red hindwings — an invasive but beautiful species.",
			"Crawls on ferns and forest plants.",
			requiredPlants: new[] { "fern" }),

		CreateInsect("mason_bee", "Mason Bee", ZoneType.Forest, "common",
			MovementPattern.Hover, "day", 0.8f, 80f, 220f, 32f,
			"A gentle solitary bee that nests in hollow stems and is an exceptional pollinator.",
			"Visits honeysuckle and forest flowers.",
			requiredPlants: new[] { "honeysuckle" }),

		// ── Forest Zone — Night/Twilight (5 species) ────────────────

		CreateInsect("peppered_moth", "Peppered Moth", ZoneType.Forest, "common",
			MovementPattern.Hover, "night", 0.7f, 80f, 200f, 28f,
			"Famous for evolving darker coloring during industrial pollution — natural selection in action.",
			"A nocturnal forest moth."),

		CreateInsect("luna_moth", "Luna Moth", ZoneType.Forest, "rare",
			MovementPattern.Flutter, "night", 0.15f, 25f, 80f, 35f,
			"A ghostly green moth with long trailing tails, rarely seen in its short adult life.",
			"Appears on moonlit nights near lily of the valley.",
			requiredPlants: new[] { "lily_of_the_valley" }),

		CreateInsect("elephant_hawk_moth", "Elephant Hawk-Moth", ZoneType.Forest, "uncommon",
			MovementPattern.Hover, "both", 0.4f, 50f, 140f, 38f,
			"A stunning pink-and-olive moth that hovers like a hummingbird at twilight flowers.",
			"Visits digitalis and honeysuckle at dusk.",
			requiredPlants: new[] { "digitalis" }),

		CreateInsect("stag_beetle", "Stag Beetle", ZoneType.Forest, "uncommon",
			MovementPattern.Crawl, "both", 0.4f, 60f, 180f, 10f,
			"Males wield enormous antler-like mandibles in ritualistic wrestling matches.",
			"Active at twilight near forest undergrowth."),

		CreateInsect("walking_stick", "Walking Stick", ZoneType.Forest, "rare",
			MovementPattern.Crawl, "night", 0.15f, 40f, 120f, 5f,
			"A master of disguise, this insect is nearly indistinguishable from a twig.",
			"Hides among ferns at night — look very carefully.",
			requiredPlants: new[] { "fern" }),

		// ── Deep Wood Zone — Day (3 species) ────────────────────────

		CreateInsect("colorado_potato_beetle", "Colorado Potato Beetle", ZoneType.DeepWood, "common",
			MovementPattern.Crawl, "day", 0.7f, 80f, 220f, 12f,
			"Bold yellow-and-black striped wing cases make this a recognizable forest floor dweller.",
			"Crawls through deep wood vegetation."),

		CreateInsect("weevil", "Weevil", ZoneType.DeepWood, "common",
			MovementPattern.Crawl, "day", 0.7f, 80f, 220f, 10f,
			"A tiny beetle with a comically long snout used for boring into wood and fungi.",
			"Found on mushroom clusters in the deep wood.",
			requiredPlants: new[] { "mushroom_cluster" }),

		CreateInsect("carpenter_bee", "Carpenter Bee", ZoneType.DeepWood, "uncommon",
			MovementPattern.Erratic, "day", 0.5f, 60f, 180f, 35f,
			"A large, robust bee that excavates nesting tunnels in dead wood with powerful mandibles.",
			"Nests near dead logs.",
			requiredPlants: new[] { "dead_log" }),

		// ── Deep Wood Zone — Night (6 species) ──────────────────────

		CreateInsect("polyphemus_moth", "Polyphemus Moth", ZoneType.DeepWood, "rare",
			MovementPattern.Hover, "night", 0.2f, 30f, 90f, 35f,
			"Giant eyespots on its hindwings stare like the cyclops of myth, startling predators.",
			"Appears near dead logs at night.",
			requiredPlants: new[] { "dead_log" }),

		CreateInsect("cecropia_moth", "Cecropia Moth", ZoneType.DeepWood, "very_rare",
			MovementPattern.Hover, "night", 0.08f, 20f, 60f, 40f,
			"North America's largest moth, with wings painted in russet, cream, and red crescents.",
			"Extremely rare — requires dead logs in deep wood.",
			requiredPlants: new[] { "dead_log" }),

		CreateInsect("deaths_head_hawkmoth", "Death's-Head Hawkmoth", ZoneType.DeepWood, "very_rare",
			MovementPattern.Erratic, "night", 0.08f, 15f, 50f, 50f,
			"A skull-shaped marking on its thorax and the ability to squeak make this moth legendary.",
			"Haunts the deep wood on dark nights.",
			requiredPlants: new[] { "dead_log" }),

		CreateInsect("rhinoceros_beetle", "Rhinoceros Beetle", ZoneType.DeepWood, "rare",
			MovementPattern.Crawl, "night", 0.2f, 40f, 120f, 8f,
			"One of the strongest animals relative to its size, lifting 850 times its own weight.",
			"Found near compost piles in the deep wood.",
			requiredPlants: new[] { "compost_pile" }),

		CreateInsect("firefly", "Firefly", ZoneType.DeepWood, "uncommon",
			MovementPattern.Erratic, "night", 0.5f, 45f, 130f, 35f,
			"Creates a magical display of bioluminescent flashes to attract a mate in the darkness.",
			"Glows near moss patches in the deep wood.",
			requiredPlants: new[] { "moss_patch" }),

		CreateInsect("leaf_insect", "Leaf Insect", ZoneType.DeepWood, "legendary",
			MovementPattern.Crawl, "night", 0.03f, 10f, 40f, 4f,
			"The ultimate camouflage artist — its body perfectly mimics a living leaf, veins and all.",
			"The rarest deep wood dweller. Search moss patches on dark nights.",
			requiredPlants: new[] { "moss_patch" }),

		// ── Rock Garden Zone — Day (6 species) ──────────────────────

		CreateInsect("black_swallowtail", "Black Swallowtail", ZoneType.RockGarden, "uncommon",
			MovementPattern.Flutter, "day", 0.5f, 50f, 150f, 44f,
			"Dark velvety wings with yellow spots and blue hindwing scales shimmer in alpine sun.",
			"Visits edelweiss and sea lavender in rock gardens.",
			requiredPlants: new[] { "edelweiss" }),

		CreateInsect("glasswing", "Glasswing", ZoneType.RockGarden, "very_rare",
			MovementPattern.Hover, "day", 0.08f, 15f, 50f, 45f,
			"Transparent wings make this butterfly nearly invisible in flight — a living ghost.",
			"Extremely rare. Appears near sun-warmed rocks."),

		CreateInsect("sacred_scarab", "Sacred Scarab", ZoneType.RockGarden, "rare",
			MovementPattern.Crawl, "day", 0.2f, 40f, 120f, 10f,
			"Revered in ancient Egypt, this beetle rolls dung balls with cosmic determination.",
			"Basks on sun-warmed rocks during the day."),

		CreateInsect("teddy_bear_bee", "Teddy Bear Bee", ZoneType.RockGarden, "rare",
			MovementPattern.Hover, "day", 0.2f, 40f, 120f, 32f,
			"Covered in dense golden-brown fur, this plump bee looks like a tiny flying teddy bear.",
			"Visits thyme and edelweiss in alpine areas.",
			requiredPlants: new[] { "thyme" }),

		CreateInsect("leafcutter_bee", "Leafcutter Bee", ZoneType.RockGarden, "uncommon",
			MovementPattern.Hover, "day", 0.5f, 60f, 180f, 30f,
			"Neatly cuts circular pieces from leaves to construct its elaborate nest chambers.",
			"Found near thyme and sea lavender.",
			requiredPlants: new[] { "thyme" }),

		CreateInsect("pillbug", "Pillbug", ZoneType.RockGarden, "common",
			MovementPattern.Crawl, "day", 0.7f, 100f, 280f, 8f,
			"Not actually an insect but a crustacean, it curls into a perfect armored ball when startled.",
			"Lives under damp rocks.",
			requiredPlants: new[] { "saxifrage" }),

		// ── Rock Garden Zone — Night (2 species) ────────────────────

		CreateInsect("cone_headed_grasshopper", "Cone-Headed Grasshopper", ZoneType.RockGarden, "uncommon",
			MovementPattern.Crawl, "night", 0.4f, 70f, 200f, 18f,
			"A distinctive pointed head helps this grasshopper blend perfectly with grass blades.",
			"Emerges from rock crevasses after dark.",
			requiredPlants: new[] { "saxifrage" }),

		CreateInsect("field_cricket", "Field Cricket", ZoneType.RockGarden, "common",
			MovementPattern.Crawl, "night", 0.7f, 80f, 240f, 14f,
			"Its rhythmic chirping is the quintessential soundtrack of warm summer nights.",
			"Chirps from sheltered spots among rocks."),

		// ── Rock Garden Zone — Day (1 more) ─────────────────────────

		CreateInsect("ant", "Ant", ZoneType.RockGarden, "common",
			MovementPattern.Crawl, "day", 0.8f, 100f, 300f, 10f,
			"A tireless worker carrying loads many times its weight along invisible chemical highways.",
			"Marches in lines around thyme and rocks.",
			requiredPlants: new[] { "thyme" }),

		// ── Pond Zone — Day (7 species) ─────────────────────────────

		CreateInsect("blue_dasher", "Blue Dasher", ZoneType.Pond, "common",
			MovementPattern.Hover, "day", 0.7f, 70f, 200f, 50f,
			"A powder-blue dragonfly that perches on reed tips and dashes out to catch prey.",
			"Patrols over water tiles.",
			requiredWaterTiles: 1),

		CreateInsect("twelve_spotted_skimmer", "Twelve-Spotted Skimmer", ZoneType.Pond, "common",
			MovementPattern.Hover, "day", 0.7f, 70f, 200f, 48f,
			"Twelve dark wing spots make this dragonfly easy to identify as it skims the water.",
			"Skims over water tiles.",
			requiredWaterTiles: 1),

		CreateInsect("flame_skimmer", "Flame Skimmer", ZoneType.Pond, "uncommon",
			MovementPattern.Hover, "day", 0.5f, 50f, 150f, 52f,
			"An entirely flame-orange dragonfly that seems to glow in afternoon sunlight.",
			"Prefers sunny water near iris.",
			requiredPlants: new[] { "water_iris" }, requiredWaterTiles: 1),

		CreateInsect("halloween_pennant", "Halloween Pennant", ZoneType.Pond, "uncommon",
			MovementPattern.Hover, "day", 0.5f, 50f, 150f, 45f,
			"Orange-and-brown patterned wings wave like tiny flags as it perches on grass tips.",
			"Hovers near water iris.",
			requiredPlants: new[] { "water_iris" }, requiredWaterTiles: 1),

		CreateInsect("red_veined_darter", "Red-Veined Darter", ZoneType.Pond, "uncommon",
			MovementPattern.Hover, "day", 0.5f, 50f, 150f, 55f,
			"Crimson veins thread through amber wings like a stained glass window in flight.",
			"Darts over the pond surface.",
			requiredWaterTiles: 1),

		CreateInsect("emperor_dragonfly", "Emperor Dragonfly", ZoneType.Pond, "rare",
			MovementPattern.Hover, "day", 0.2f, 25f, 80f, 60f,
			"The largest dragonfly species, a powerful blue-green hunter of the skies.",
			"Rules over deeper water areas.",
			requiredWaterTiles: 2),

		CreateInsect("azure_damselfly", "Azure Damselfly", ZoneType.Pond, "common",
			MovementPattern.Flutter, "day", 0.7f, 60f, 180f, 38f,
			"A delicate sky-blue damselfly that rests with wings folded above its slender body.",
			"Found near water lilies and cattails.",
			requiredPlants: new[] { "water_lily" }),

		// ── Pond Zone — Day (1 more) ────────────────────────────────

		CreateInsect("water_strider", "Water Strider", ZoneType.Pond, "common",
			MovementPattern.Crawl, "day", 0.8f, 80f, 220f, 20f,
			"Uses surface tension to walk on water, sensing vibrations from trapped insects.",
			"Skates on the water surface near cattails.",
			requiredWaterTiles: 1),

		// ── Tropical Zone — Day (6 species) ─────────────────────────

		CreateInsect("zebra_longwing", "Zebra Longwing", ZoneType.Tropical, "rare",
			MovementPattern.Flutter, "day", 0.2f, 30f, 100f, 42f,
			"Long narrow wings with zebra stripes — one of the few butterflies that eats pollen for extra nutrition.",
			"A passionflower specialist.",
			requiredPlants: new[] { "passionflower" }),

		CreateInsect("ulysses_butterfly", "Ulysses Butterfly", ZoneType.Tropical, "very_rare",
			MovementPattern.Erratic, "day", 0.08f, 15f, 50f, 55f,
			"Electric blue wings flash brilliantly in tropical sunlight then vanish when they fold shut.",
			"Drawn to hibiscus in the greenhouse.",
			requiredPlants: new[] { "hibiscus" }),

		CreateInsect("queen_alexandras_birdwing", "Queen Alexandra's Birdwing", ZoneType.Tropical, "legendary",
			MovementPattern.Flutter, "day", 0.03f, 10f, 40f, 40f,
			"The world's largest butterfly with a 30cm wingspan — an endangered living treasure.",
			"Requires rare orchids. The ultimate discovery.",
			requiredPlants: new[] { "orchid" }),

		CreateInsect("madagascan_sunset_moth", "Madagascan Sunset Moth", ZoneType.Tropical, "very_rare",
			MovementPattern.Erratic, "day", 0.08f, 15f, 50f, 50f,
			"Often called the most beautiful moth — its wings display every color of the rainbow.",
			"Active during the day near lantana.",
			requiredPlants: new[] { "lantana" }),

		CreateInsect("orchid_bee", "Orchid Bee", ZoneType.Tropical, "very_rare",
			MovementPattern.Hover, "day", 0.08f, 20f, 60f, 35f,
			"A metallic green bee that collects fragrant oils from orchids for its elaborate mating display.",
			"An orchid specialist — only visits orchids.",
			requiredPlants: new[] { "orchid" }),

		CreateInsect("blue_banded_bee", "Blue-Banded Bee", ZoneType.Tropical, "rare",
			MovementPattern.Hover, "day", 0.2f, 30f, 100f, 33f,
			"Striking turquoise bands glow against a dark body as it buzz-pollinates tropical flowers.",
			"Visits hibiscus and tropical flowers.",
			requiredPlants: new[] { "hibiscus" }),

		// ── Tropical Zone — Night (3 species) ───────────────────────

		CreateInsect("atlas_moth", "Atlas Moth", ZoneType.Tropical, "very_rare",
			MovementPattern.Hover, "night", 0.08f, 20f, 60f, 45f,
			"One of the largest moths in the world — its wingtips resemble snake heads to deter predators.",
			"Appears near lantana on tropical nights.",
			requiredPlants: new[] { "lantana" }),

		CreateInsect("comet_moth", "Comet Moth", ZoneType.Tropical, "legendary",
			MovementPattern.Flutter, "night", 0.03f, 10f, 40f, 38f,
			"A spectacular yellow moth with 20cm tail streamers that trail behind it like a comet.",
			"Visits orchids on rare tropical nights.",
			requiredPlants: new[] { "orchid" }),

		CreateInsect("hercules_beetle", "Hercules Beetle", ZoneType.Tropical, "legendary",
			MovementPattern.Crawl, "night", 0.03f, 10f, 40f, 6f,
			"The largest beetle on Earth, with a massive horn that can be longer than its body.",
			"Emerges in the greenhouse on dark nights.",
			requiredPlants: new[] { "bougainvillea" }),

		// ── Tropical Zone — Day (1 more) ────────────────────────────

		CreateInsect("orchid_mantis", "Orchid Mantis", ZoneType.Tropical, "legendary",
			MovementPattern.Crawl, "day", 0.03f, 10f, 40f, 5f,
			"A predator so perfectly disguised as a flower that insects come to it willingly.",
			"Hides among orchids — nearly impossible to spot.",
			requiredPlants: new[] { "orchid" }),
	};

	private static InsectData CreateInsect(
		string id, string displayName, ZoneType zone, string rarity,
		MovementPattern pattern, string timeOfDay, float spawnWeight,
		float visitMin, float visitMax, float speed,
		string journalText, string hintText,
		string[] requiredPlants = null, int requiredWaterTiles = 0)
	{
		return new InsectData
		{
			Id = id,
			DisplayName = displayName,
			Zone = zone,
			Rarity = rarity,
			TimeOfDay = timeOfDay,
			RequiredPlants = requiredPlants,
			RequiredWaterTiles = requiredWaterTiles,
			SpawnWeight = spawnWeight,
			VisitDurationMin = visitMin,
			VisitDurationMax = visitMax,
			MovementPattern = pattern,
			MovementSpeed = speed,
			PauseFrequency = 0.4f,
			PauseDuration = 2.0f,
			JournalText = journalText,
			HintText = hintText,
		};
	}
}
