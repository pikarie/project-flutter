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
		// === Starter Zone — Common (day) ===
		CreatePlant("lavender", "Lavender", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "honeybee", "bumblebee" },
			new Color(0.6f, 0.4f, 0.8f)),

		CreatePlant("sunflower", "Sunflower", ZoneType.Starter, "common",
			6, 3, 2, 2, false,
			new[] { "honeybee", "ladybug", "bumblebee" },
			new Color(1.0f, 0.85f, 0.1f)),

		CreatePlant("daisy", "Daisy", ZoneType.Starter, "common",
			5, 3, 2, 2, false,
			new[] { "honeybee", "cabbage_white" },
			new Color(0.95f, 0.95f, 0.9f)),

		CreatePlant("coneflower", "Coneflower", ZoneType.Starter, "common",
			7, 3, 2, 2, false,
			new[] { "cabbage_white", "garden_spider", "bumblebee" },
			new Color(0.85f, 0.35f, 0.6f)),

		CreatePlant("marigold", "Marigold", ZoneType.Starter, "common",
			6, 3, 2, 2, false,
			new[] { "ladybug", "garden_spider" },
			new Color(1.0f, 0.6f, 0.1f)),

		// === Starter Zone — Uncommon (night) ===
		CreatePlant("moonflower", "Moonflower", ZoneType.Starter, "uncommon",
			25, 5, 3, 2, true,
			new[] { "sphinx_moth" },
			new Color(0.85f, 0.85f, 1.0f)),

		CreatePlant("evening_primrose", "Evening Primrose", ZoneType.Starter, "uncommon",
			20, 4, 3, 2, true,
			new[] { "sphinx_moth", "owl_moth" },
			new Color(0.95f, 0.95f, 0.5f)),

		// === Meadow Zone — Common ===
		CreatePlant("wildflower_mix", "Wildflower Mix", ZoneType.Meadow, "common",
			10, 3, 2, 3, false,
			new[] { "grasshopper", "painted_lady", "cabbage_white" },
			new Color(0.8f, 0.5f, 0.9f)),

		// === Meadow Zone — Uncommon ===
		CreatePlant("milkweed", "Milkweed", ZoneType.Meadow, "uncommon",
			25, 5, 3, 3, false,
			new[] { "monarch" },
			new Color(0.9f, 0.5f, 0.6f)),

		CreatePlant("dill", "Dill", ZoneType.Meadow, "uncommon",
			20, 4, 3, 2, false,
			new[] { "swallowtail", "ladybug" },
			new Color(0.6f, 0.8f, 0.3f)),

		CreatePlant("goldenrod", "Goldenrod", ZoneType.Meadow, "uncommon",
			22, 4, 3, 2, false,
			new[] { "monarch", "painted_lady", "hoverfly" },
			new Color(0.95f, 0.8f, 0.15f)),

		CreatePlant("black_eyed_susan", "Black-Eyed Susan", ZoneType.Meadow, "uncommon",
			25, 5, 3, 2, false,
			new[] { "hoverfly", "honeybee" },
			new Color(1.0f, 0.7f, 0.0f)),

		// === Meadow Zone — Rare (night) ===
		CreatePlant("night_jasmine", "Night Jasmine", ZoneType.Meadow, "rare",
			60, 8, 4, 3, true,
			new[] { "luna_moth" },
			new Color(1.0f, 1.0f, 0.9f)),

		CreatePlant("white_birch", "White Birch", ZoneType.Meadow, "rare",
			75, 10, 5, 3, true,
			new[] { "luna_moth", "atlas_moth" },
			new Color(0.85f, 0.9f, 0.85f)),
		// Note: GDD specifies 2x2 tile, implemented as 1x1 for now

		// === Pond Zone — Common ===
		CreatePlant("cattail", "Cattail", ZoneType.Pond, "common",
			10, 3, 2, 2, false,
			new[] { "dragonfly", "water_strider" },
			new Color(0.55f, 0.4f, 0.2f)),

		CreatePlant("switchgrass", "Switchgrass", ZoneType.Pond, "common",
			8, 3, 2, 2, true,
			new[] { "cricket", "firefly" },
			new Color(0.5f, 0.7f, 0.3f)),

		// === Pond Zone — Uncommon ===
		CreatePlant("water_lily", "Water Lily", ZoneType.Pond, "uncommon",
			30, 5, 3, 2, false,
			new[] { "dragonfly", "damselfly" },
			new Color(0.95f, 0.7f, 0.8f)),

		CreatePlant("iris", "Iris", ZoneType.Pond, "uncommon",
			25, 5, 3, 2, false,
			new[] { "damselfly", "cabbage_white" },
			new Color(0.5f, 0.3f, 0.8f)),

		// === Pond Zone — Rare ===
		CreatePlant("lotus", "Lotus", ZoneType.Pond, "rare",
			60, 8, 5, 3, false,
			new[] { "emperor_dragonfly" },
			new Color(1.0f, 0.6f, 0.7f)),

		CreatePlant("passionflower", "Passionflower", ZoneType.Pond, "rare",
			50, 7, 4, 3, false,
			new[] { "gulf_fritillary" },
			new Color(0.6f, 0.3f, 0.9f)),
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
