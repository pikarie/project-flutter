using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ProjectFlutter;

public static class PlantRegistry
{
	private static List<PlantData> _allPlants;

	public static IReadOnlyList<PlantData> AllPlants =>
		_allPlants ??= CreatePlantList();

	public static int TotalPlantCount => AllPlants.Count;

	public static PlantData GetById(string id) =>
		AllPlants.FirstOrDefault(plant => plant.Id == id);

	public static IEnumerable<PlantData> GetByZone(ZoneType zone) =>
		AllPlants.Where(plant => plant.Zone == zone);

	private static List<PlantData> CreatePlantList() => new()
	{
		// ── Starter Zone ────────────────────────────────────────────

		CreatePlant("lavender", "Lavender", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "cabbage_white", "red_admiral", "japanese_beetle" },
			new Color(0.6f, 0.4f, 0.8f)),

		CreatePlant("sunflower", "Sunflower", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "golden_tortoise_beetle", "japanese_beetle", "western_honeybee" },
			new Color(1.0f, 0.85f, 0.1f)),

		CreatePlant("daisy", "Daisy", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "cabbage_white", "orange_tip", "rose_chafer", "marmalade_hoverfly" },
			new Color(0.95f, 0.95f, 0.9f)),

		CreatePlant("marigold", "Marigold", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "orange_tip", "rose_chafer", "marmalade_hoverfly" },
			new Color(1.0f, 0.6f, 0.1f)),

		CreatePlant("calendula", "Calendula", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "seven_spot_ladybug", "twenty_two_spot_ladybug" },
			new Color(1.0f, 0.7f, 0.2f)),

		CreatePlant("evening_primrose", "Evening Primrose", ZoneType.Starter, "common",
			5, 3, 2, 1, true,
			new[] { "rosy_maple_moth", "garden_tiger_moth" },
			new Color(0.95f, 0.95f, 0.5f)),

		// ── Meadow Zone ─────────────────────────────────────────────

		CreatePlant("milkweed", "Milkweed", ZoneType.Meadow, "uncommon",
			15, 5, 3, 2, false,
			new[] { "monarch" },
			new Color(0.9f, 0.5f, 0.6f)),

		CreatePlant("goldenrod", "Goldenrod", ZoneType.Meadow, "common",
			5, 3, 2, 2, false,
			new[] { "old_world_swallowtail", "long_horned_bee" },
			new Color(0.95f, 0.8f, 0.15f)),

		CreatePlant("clover", "Clover", ZoneType.Meadow, "common",
			5, 3, 2, 2, false,
			new[] { "clouded_sulphur", "common_blue", "wool_carder_bee" },
			new Color(0.8f, 0.5f, 0.7f)),

		CreatePlant("bluebell", "Bluebell", ZoneType.Meadow, "common",
			5, 3, 2, 2, false,
			new[] { "clouded_sulphur", "common_blue", "wool_carder_bee" },
			new Color(0.4f, 0.5f, 0.85f)),

		CreatePlant("ragwort", "Ragwort", ZoneType.Meadow, "common",
			5, 3, 2, 1, true,
			new[] { "cinnabar_moth" },
			new Color(0.85f, 0.8f, 0.2f)),

		CreatePlant("thistle", "Thistle", ZoneType.Meadow, "uncommon",
			10, 5, 3, 2, false,
			new[] { "old_world_swallowtail", "long_horned_bee" },
			new Color(0.7f, 0.3f, 0.7f)),

		// ── Forest Zone ─────────────────────────────────────────────

		CreatePlant("fern", "Fern", ZoneType.Forest, "common",
			5, 3, 2, 1, false,
			new[] { "walking_stick", "spotted_lanternfly" },
			new Color(0.3f, 0.7f, 0.3f)),

		CreatePlant("lily_of_the_valley", "Lily of the Valley", ZoneType.Forest, "uncommon",
			15, 5, 3, 2, false,
			new[] { "eastern_tiger_swallowtail", "luna_moth" },
			new Color(0.95f, 0.97f, 0.92f)),

		CreatePlant("digitalis", "Digitalis", ZoneType.Forest, "uncommon",
			10, 4, 3, 2, false,
			new[] { "elephant_hawk_moth" },
			new Color(0.8f, 0.4f, 0.7f)),

		CreatePlant("violet", "Violet", ZoneType.Forest, "uncommon",
			10, 4, 3, 1, false,
			new[] { "question_mark_butterfly" },
			new Color(0.5f, 0.3f, 0.8f)),

		CreatePlant("honeysuckle", "Honeysuckle", ZoneType.Forest, "uncommon",
			10, 5, 3, 2, false,
			new[] { "elephant_hawk_moth", "mason_bee" },
			new Color(1.0f, 0.9f, 0.6f)),

		// ── Deep Wood Zone ──────────────────────────────────────────

		CreatePlant("dead_log", "Dead Log", ZoneType.DeepWood, "common",
			0, 0, 1, 2, false,
			new[] { "polyphemus_moth", "cecropia_moth", "deaths_head_hawkmoth", "carpenter_bee" },
			new Color(0.45f, 0.3f, 0.15f)),

		CreatePlant("compost_pile", "Compost Pile", ZoneType.DeepWood, "common",
			30, 0, 1, 2, false,
			new[] { "rhinoceros_beetle", "colorado_potato_beetle" },
			new Color(0.35f, 0.25f, 0.1f)),

		CreatePlant("mushroom_cluster", "Mushroom Cluster", ZoneType.DeepWood, "common",
			5, 1, 1, 1, true,
			new[] { "weevil" },
			new Color(0.8f, 0.7f, 0.5f)),

		CreatePlant("moss_patch", "Moss Patch", ZoneType.DeepWood, "common",
			5, 1, 1, 1, false,
			new[] { "firefly", "leaf_insect" },
			new Color(0.2f, 0.55f, 0.2f)),

		// ── Rock Garden Zone ────────────────────────────────────────

		CreatePlant("thyme", "Thyme", ZoneType.RockGarden, "common",
			5, 3, 2, 1, false,
			new[] { "leafcutter_bee", "teddy_bear_bee", "ant" },
			new Color(0.7f, 0.5f, 0.75f)),

		CreatePlant("edelweiss", "Edelweiss", ZoneType.RockGarden, "uncommon",
			15, 5, 3, 1, false,
			new[] { "black_swallowtail", "teddy_bear_bee" },
			new Color(0.95f, 0.95f, 0.9f)),

		CreatePlant("saxifrage", "Saxifrage", ZoneType.RockGarden, "uncommon",
			10, 4, 3, 1, false,
			new[] { "pillbug", "cone_headed_grasshopper" },
			new Color(0.9f, 0.6f, 0.7f)),

		CreatePlant("sea_lavender", "Sea Lavender", ZoneType.RockGarden, "uncommon",
			10, 4, 3, 1, false,
			new[] { "leafcutter_bee", "black_swallowtail" },
			new Color(0.7f, 0.55f, 0.85f)),

		// ── Pond Zone ───────────────────────────────────────────────

		CreatePlant("water_lily", "Water Lily", ZoneType.Pond, "common",
			10, 3, 2, 2, false,
			new[] { "azure_damselfly", "blue_dasher" },
			new Color(0.95f, 0.7f, 0.8f)),

		CreatePlant("cattail", "Cattail", ZoneType.Pond, "common",
			8, 2, 2, 1, false,
			new[] { "azure_damselfly", "water_strider" },
			new Color(0.55f, 0.4f, 0.2f)),

		CreatePlant("water_iris", "Water Iris", ZoneType.Pond, "uncommon",
			15, 5, 3, 2, false,
			new[] { "flame_skimmer", "halloween_pennant" },
			new Color(0.5f, 0.3f, 0.8f)),

		// ── Tropical Zone ───────────────────────────────────────────

		CreatePlant("orchid", "Orchid", ZoneType.Tropical, "rare",
			40, 10, 5, 3, false,
			new[] { "orchid_bee", "orchid_mantis", "queen_alexandras_birdwing", "comet_moth" },
			new Color(0.95f, 0.75f, 0.9f)),

		CreatePlant("passionflower", "Passionflower", ZoneType.Tropical, "rare",
			40, 10, 5, 2, false,
			new[] { "zebra_longwing" },
			new Color(0.6f, 0.3f, 0.9f)),

		CreatePlant("hibiscus", "Hibiscus", ZoneType.Tropical, "uncommon",
			30, 8, 4, 2, false,
			new[] { "blue_banded_bee", "ulysses_butterfly" },
			new Color(0.9f, 0.3f, 0.3f)),

		CreatePlant("lantana", "Lantana", ZoneType.Tropical, "rare",
			40, 10, 5, 2, false,
			new[] { "madagascan_sunset_moth", "atlas_moth" },
			new Color(1.0f, 0.5f, 0.4f)),

		CreatePlant("bougainvillea", "Bougainvillea", ZoneType.Tropical, "rare",
			40, 10, 5, 1, false,
			new[] { "hercules_beetle" },
			new Color(0.85f, 0.2f, 0.6f)),
	};

	private static PlantData CreatePlant(
		string id, string displayName, ZoneType zone, string rarity,
		int seedCost, int nectarYield, int growthCycles, int insectSlots,
		bool nightBlooming, string[] attractedInsects, Color drawColor)
	{
		return new PlantData
		{
			Id = id,
			DisplayName = displayName,
			Zone = zone,
			Rarity = rarity,
			SeedCost = seedCost,
			NectarYield = nectarYield,
			GrowthCycles = growthCycles,
			InsectSlots = insectSlots,
			NightBlooming = nightBlooming,
			AttractedInsects = attractedInsects,
			DrawColor = drawColor,
		};
	}
}
