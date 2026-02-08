using Godot;
using System.Collections.Generic;

public partial class JournalManager : Node
{
	[Signal] public delegate void SpeciesDiscoveredEventHandler(string insectId);
	[Signal] public delegate void JournalUpdatedEventHandler(string insectId, int starRating);

	private readonly Dictionary<string, int> _discoveredSpecies = new();

	public void DiscoverSpecies(string insectId, int starRating)
	{
		if (!_discoveredSpecies.ContainsKey(insectId))
		{
			_discoveredSpecies[insectId] = starRating;
			EmitSignal(SignalName.SpeciesDiscovered, insectId);
		}
		else if (starRating > _discoveredSpecies[insectId])
		{
			_discoveredSpecies[insectId] = starRating;
			EmitSignal(SignalName.JournalUpdated, insectId, starRating);
		}
	}

	public bool IsDiscovered(string insectId) =>
		_discoveredSpecies.ContainsKey(insectId);

	public int GetStarRating(string insectId) =>
		_discoveredSpecies.TryGetValue(insectId, out int rating) ? rating : 0;

	public int GetDiscoveredCount() => _discoveredSpecies.Count;
}
