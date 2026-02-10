using Godot;
using System.Collections.Generic;
using ProjectFlutter;

public partial class JournalManager : Node
{
	public static JournalManager Instance { get; private set; }

	private readonly Dictionary<string, int> _discoveredSpecies = new();

	public override void _Ready() => Instance = this;

	public void DiscoverSpecies(string insectId, int starRating)
	{
		if (!_discoveredSpecies.ContainsKey(insectId))
		{
			_discoveredSpecies[insectId] = starRating;
			EventBus.Publish(new SpeciesDiscoveredEvent(insectId));
		}
		else if (starRating > _discoveredSpecies[insectId])
		{
			_discoveredSpecies[insectId] = starRating;
			EventBus.Publish(new JournalUpdatedEvent(insectId, starRating));
		}
	}

	public bool IsDiscovered(string insectId) =>
		_discoveredSpecies.ContainsKey(insectId);

	public int GetStarRating(string insectId) =>
		_discoveredSpecies.TryGetValue(insectId, out int rating) ? rating : 0;

	public int GetDiscoveredCount() => _discoveredSpecies.Count;
}
