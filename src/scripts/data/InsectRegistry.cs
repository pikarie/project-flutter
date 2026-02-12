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
		// ── Starter Zone — Day ──────────────────────────────────────

		CreateInsect("honeybee", "Honeybee", ZoneType.Starter, "common",
			MovementPattern.Hover, "day", 1.0f, 90f, 240f, 30f,
			"A busy pollinator that dances to communicate flower locations to its hive.",
			"Visits blooming flowers during the day."),

		CreateInsect("bumblebee", "Bumblebee", ZoneType.Starter, "common",
			MovementPattern.Hover, "day", 0.9f, 100f, 260f, 25f,
			"Rounder and fuzzier than honeybees, bumblebees can fly in cooler weather.",
			"Buzzes around lavender and coneflowers in daylight."),

		CreateInsect("cabbage_white", "Cabbage White", ZoneType.Starter, "common",
			MovementPattern.Flutter, "day", 0.8f, 60f, 180f, 40f,
			"A delicate butterfly with a fluttering flight pattern, often seen in gardens.",
			"Flutters around gardens in daylight hours."),

		CreateInsect("ladybug", "Ladybug", ZoneType.Starter, "common",
			MovementPattern.Crawl, "day", 0.7f, 120f, 300f, 15f,
			"A gardener's best friend, this spotted beetle crawls slowly but stays long.",
			"Crawls on blooming plants during the day."),

		CreateInsect("garden_spider", "Garden Spider", ZoneType.Starter, "uncommon",
			MovementPattern.Crawl, "day", 0.4f, 80f, 200f, 10f,
			"Patient and still, the garden spider waits motionless among the petals.",
			"Appears in gardens with many blooming plants.",
			requiredPlants: new[] { "lavender", "sunflower", "daisy" }),

		// ── Starter Zone — Night ────────────────────────────────────

		CreateInsect("sphinx_moth", "Sphinx Moth", ZoneType.Starter, "uncommon",
			MovementPattern.Erratic, "night", 0.5f, 45f, 120f, 50f,
			"A mysterious night visitor with erratic movements, drawn to moonlit blooms.",
			"Only appears at night near moonflowers.",
			requiredPlants: new[] { "moonflower" }),

		CreateInsect("owl_moth", "Owl Moth", ZoneType.Starter, "uncommon",
			MovementPattern.Erratic, "night", 0.4f, 40f, 100f, 45f,
			"Named for the eye-like patterns on its wings that ward off predators.",
			"Visits night-blooming flowers after dark.",
			requiredPlants: new[] { "evening_primrose" }),

		// ── Meadow Zone — Day ───────────────────────────────────────

		CreateInsect("monarch_butterfly", "Monarch Butterfly", ZoneType.Meadow, "uncommon",
			MovementPattern.Flutter, "day", 0.5f, 50f, 150f, 45f,
			"Famous for its incredible migration, this orange beauty depends on milkweed.",
			"Exclusively visits milkweed in meadow zones.",
			requiredPlants: new[] { "milkweed" }),

		CreateInsect("swallowtail", "Swallowtail Butterfly", ZoneType.Meadow, "uncommon",
			MovementPattern.Flutter, "day", 0.5f, 55f, 160f, 42f,
			"Elegant tail-like extensions on its hindwings make it unmistakable in flight.",
			"Attracted to dill in the meadow.",
			requiredPlants: new[] { "dill" }),

		CreateInsect("hoverfly", "Hoverfly", ZoneType.Meadow, "common",
			MovementPattern.Hover, "day", 0.8f, 70f, 200f, 35f,
			"Often mistaken for a bee, this harmless fly is an excellent pollinator.",
			"Hovers near goldenrod and black-eyed susans.",
			requiredPlants: new[] { "goldenrod" }),

		CreateInsect("grasshopper", "Grasshopper", ZoneType.Meadow, "common",
			MovementPattern.Crawl, "day", 0.7f, 80f, 220f, 18f,
			"A powerful jumper that creates a distinctive chirping sound by rubbing its legs.",
			"Found among wildflowers and grasses.",
			requiredPlants: new[] { "wildflower_mix" }),

		CreateInsect("painted_lady", "Painted Lady", ZoneType.Meadow, "uncommon",
			MovementPattern.Flutter, "day", 0.45f, 50f, 140f, 44f,
			"One of the most widespread butterflies, found on every continent except Antarctica.",
			"Visits goldenrod in meadows.",
			requiredPlants: new[] { "goldenrod" }),

		CreateInsect("praying_mantis", "Praying Mantis", ZoneType.Meadow, "rare",
			MovementPattern.Crawl, "day", 0.2f, 60f, 180f, 8f,
			"A patient predator that holds perfectly still before striking with lightning speed.",
			"Only appears in gardens teeming with other insects.",
			requiredPlants: new[] { "wildflower_mix", "milkweed", "goldenrod" }),

		CreateInsect("jewel_beetle", "Jewel Beetle", ZoneType.Meadow, "rare",
			MovementPattern.Crawl, "day", 0.25f, 40f, 120f, 12f,
			"Its iridescent shell was used in ancient times as living jewelry.",
			"Attracted to sunflowers and goldenrod together.",
			requiredPlants: new[] { "goldenrod" }),

		// ── Meadow Zone — Night ─────────────────────────────────────

		CreateInsect("luna_moth", "Luna Moth", ZoneType.Meadow, "rare",
			MovementPattern.Flutter, "night", 0.2f, 30f, 90f, 38f,
			"A ghostly green moth with long trailing tails, rarely seen and short-lived.",
			"Appears only near night-blooming jasmine and white birch.",
			requiredPlants: new[] { "night_jasmine", "white_birch" }),

		CreateInsect("atlas_moth", "Atlas Moth", ZoneType.Meadow, "very_rare",
			MovementPattern.Erratic, "night", 0.08f, 20f, 60f, 55f,
			"One of the largest moths in the world, its wingtips resemble snake heads.",
			"Requires night-blooming jasmine and white birch together.",
			requiredPlants: new[] { "night_jasmine", "white_birch" }),

		// ── Meadow Zone — Special ───────────────────────────────────

		CreateInsect("monarch_migration", "Monarch Migration", ZoneType.Meadow, "very_rare",
			MovementPattern.Flutter, "day", 0.05f, 15f, 45f, 50f,
			"A breathtaking swarm of monarchs passing through on their annual journey south.",
			"Appears when milkweed and goldenrod bloom together in abundance.",
			requiredPlants: new[] { "milkweed", "goldenrod" }),

		// ── Pond Zone — Day ─────────────────────────────────────────

		CreateInsect("dragonfly", "Dragonfly", ZoneType.Pond, "uncommon",
			MovementPattern.Hover, "day", 0.5f, 30f, 90f, 55f,
			"An ancient aerial predator that can fly in any direction, even backwards.",
			"Patrols near water lilies and cattails.",
			requiredPlants: new[] { "water_lily", "cattail" }),

		CreateInsect("damselfly", "Damselfly", ZoneType.Pond, "uncommon",
			MovementPattern.Flutter, "day", 0.5f, 40f, 120f, 38f,
			"More delicate than its dragonfly cousin, it rests with wings folded above its body.",
			"Found near iris and water lilies.",
			requiredPlants: new[] { "iris" }),

		CreateInsect("water_strider", "Water Strider", ZoneType.Pond, "common",
			MovementPattern.Crawl, "day", 0.7f, 80f, 220f, 20f,
			"Uses surface tension to walk on water, sensing vibrations from trapped insects.",
			"Skates near cattails along the water's edge.",
			requiredPlants: new[] { "cattail" }, requiredWaterTiles: 1),

		CreateInsect("pond_skater", "Pond Skater", ZoneType.Pond, "common",
			MovementPattern.Crawl, "both", 0.8f, 100f, 260f, 18f,
			"A nimble water surface dweller that hunts day and night.",
			"Always present near the pond edge.",
			requiredWaterTiles: 1),

		CreateInsect("gulf_fritillary", "Gulf Fritillary", ZoneType.Pond, "rare",
			MovementPattern.Flutter, "day", 0.2f, 30f, 100f, 46f,
			"A vibrant orange butterfly with silver-spotted underwings, bound to passionflower.",
			"Exclusively visits passionflowers near the pond.",
			requiredPlants: new[] { "passionflower" }),

		CreateInsect("emperor_dragonfly", "Emperor Dragonfly", ZoneType.Pond, "rare",
			MovementPattern.Hover, "day", 0.2f, 25f, 80f, 60f,
			"The largest dragonfly species, a powerful blue-green hunter of the skies.",
			"Hunts over lotus ponds.",
			requiredPlants: new[] { "lotus" }, requiredWaterTiles: 2),

		// ── Pond Zone — Night ───────────────────────────────────────

		CreateInsect("firefly", "Firefly", ZoneType.Pond, "uncommon",
			MovementPattern.Erratic, "night", 0.5f, 45f, 130f, 35f,
			"Creates a magical display of bioluminescent flashes to attract a mate.",
			"Glows near switchgrass at night.",
			requiredPlants: new[] { "switchgrass" }),

		CreateInsect("cricket", "Cricket", ZoneType.Pond, "common",
			MovementPattern.Crawl, "night", 0.9f, 100f, 280f, 12f,
			"Its rhythmic chirping is the soundtrack of summer nights.",
			"Hides in switchgrass after dark.",
			requiredPlants: new[] { "switchgrass" }),
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
