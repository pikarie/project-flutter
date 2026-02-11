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
		CreateInsect("honeybee", "Honeybee", MovementPattern.Hover,
			"day", 1.0f, 90f, 240f, 30f,
			"A busy pollinator that dances to communicate flower locations to its hive.",
			"Visits blooming flowers during the day."),

		CreateInsect("cabbage_white", "Cabbage White", MovementPattern.Flutter,
			"day", 0.8f, 60f, 180f, 40f,
			"A delicate butterfly with a fluttering flight pattern, often seen in gardens.",
			"Flutters around gardens in daylight hours."),

		CreateInsect("ladybug", "Ladybug", MovementPattern.Crawl,
			"day", 0.7f, 120f, 300f, 15f,
			"A gardener's best friend, this spotted beetle crawls slowly but stays long.",
			"Crawls on blooming plants during the day."),

		CreateInsect("sphinx_moth", "Sphinx Moth", MovementPattern.Erratic,
			"night", 0.5f, 45f, 120f, 50f,
			"A mysterious night visitor with erratic movements, drawn to moonlit blooms.",
			"Only appears at dusk and night near flowers."),
	};

	private static InsectData CreateInsect(
		string id, string displayName, MovementPattern pattern,
		string timeOfDay, float spawnWeight,
		float visitMin, float visitMax, float speed,
		string journalText, string hintText)
	{
		return new InsectData
		{
			Id = id,
			DisplayName = displayName,
			Zone = ZoneType.Starter,
			Rarity = "common",
			TimeOfDay = timeOfDay,
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
