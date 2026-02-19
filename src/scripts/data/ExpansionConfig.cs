using System.Collections.Generic;
using Godot;

public record ExpansionTierData(
	string Name,
	int GridWidth,
	int GridHeight,
	int NectarCost,
	int InsectCapBonus
);

public static class ExpansionConfig
{
	public static readonly Dictionary<ZoneType, ExpansionTierData[]> Expansions = new()
	{
		// Starter — rings (square, concentric)
		{ ZoneType.Starter, new[]
		{
			new ExpansionTierData("Back Garden", 9, 9, 100, 4),
			new ExpansionTierData("Flower Beds", 13, 13, 250, 4),
		}},
		// Meadow — lateral (wide, then taller)
		{ ZoneType.Meadow, new[]
		{
			new ExpansionTierData("Wild Meadow", 14, 6, 150, 5),
			new ExpansionTierData("Golden Fields", 14, 10, 400, 5),
		}},
		// Forest — rings (square, concentric)
		{ ZoneType.Forest, new[]
		{
			new ExpansionTierData("Mossy Clearing", 10, 10, 150, 4),
			new ExpansionTierData("Deep Canopy", 14, 14, 350, 4),
		}},
		// Deep Wood — lateral horizontal (wide)
		{ ZoneType.DeepWood, new[]
		{
			new ExpansionTierData("Rotting Grove", 11, 5, 200, 3),
		}},
		// Rock Garden — rings
		{ ZoneType.RockGarden, new[]
		{
			new ExpansionTierData("Stone Terrace", 9, 9, 200, 3),
		}},
		// Pond — rings
		{ ZoneType.Pond, new[]
		{
			new ExpansionTierData("Lily Shore", 9, 9, 175, 4),
		}},
		// Tropical — lateral (wide, then taller)
		{ ZoneType.Tropical, new[]
		{
			new ExpansionTierData("Orchid Wing", 15, 7, 300, 5),
			new ExpansionTierData("Canopy Walk", 15, 11, 600, 5),
		}},
	};

	public static int MaxExpansionTier(ZoneType zone)
		=> Expansions.TryGetValue(zone, out var tiers) ? tiers.Length : 0;

	public static ExpansionTierData GetTierData(ZoneType zone, int tier)
		=> Expansions.TryGetValue(zone, out var tiers) && tier >= 1 && tier <= tiers.Length
			? tiers[tier - 1]
			: null;
}
