using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectFlutter;

public partial class SpawnSystem : Node
{
	[Export] public int MaxInsectsPerZone { get; set; } = 10;
	[Export] public float SpawnCheckInterval { get; set; } = 5.0f;

	private GardenGrid _gardenGrid;
	private Node2D _insectContainer;
	private PackedScene _insectScene;
	private RandomNumberGenerator _rng = new();
	private float _spawnTimer;

	public override void _Ready()
	{
		_rng.Randomize();
		_gardenGrid = GetParent().GetNode<GardenGrid>("GardenGrid");
		_insectContainer = GetParent().GetNode<Node2D>("InsectContainer");
		_insectScene = GD.Load<PackedScene>("res://scenes/insects/Insect.tscn");

		_spawnTimer = SpawnCheckInterval;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta * TimeManager.Instance.SpeedMultiplier;
		_spawnTimer -= dt;
		if (_spawnTimer <= 0f)
		{
			_spawnTimer = SpawnCheckInterval + _rng.RandfRange(-1f, 1f);
			TrySpawn();
		}
	}

	private void TrySpawn()
	{
		// Population cap
		if (_insectContainer.GetChildCount() >= MaxInsectsPerZone) return;

		// Find blooming cells with available insect slots
		var bloomingCells = _gardenGrid.GetCells()
			.Where(kv => kv.Value.CurrentState == CellState.State.Blooming
						 && kv.Value.HasAvailableSlot())
			.ToList();

		if (bloomingCells.Count == 0) return;

		// 40% quiet ticks for anticipation
		if (_rng.Randf() > 0.6f) return;

		// Pick a random blooming cell with a free slot
		var target = bloomingCells[_rng.RandiRange(0, bloomingCells.Count - 1)];
		Vector2 plantWorldPos = _gardenGrid.GlobalPosition + _gardenGrid.GridToWorld(target.Key);

		// Build set of all blooming plant types for attraction matching
		var bloomingPlantTypes = _gardenGrid.GetCells()
			.Where(kv => kv.Value.CurrentState == CellState.State.Blooming)
			.Select(kv => kv.Value.PlantType)
			.Where(pt => !string.IsNullOrEmpty(pt))
			.ToHashSet();

		// Filter eligible insects
		int waterTileCount = _gardenGrid.CountWaterTiles();
		var eligible = InsectRegistry.AllSpecies
			.Where(i => MatchesTimeOfDay(i.TimeOfDay))
			.Where(i => i.Zone == GameManager.Instance.CurrentZone || i.Zone == ZoneType.Starter)
			.Where(i => MeetsPlantRequirements(i, bloomingPlantTypes))
			.Where(i => MeetsWaterRequirements(i, waterTileCount))
			.ToList();

		if (eligible.Count == 0) return;

		// Weighted random selection
		var selected = WeightedRandomSelect(eligible);
		SpawnInsect(selected, plantWorldPos, target.Key, target.Value);
	}

	private void SpawnInsect(InsectData data, Vector2 plantWorldPos, Vector2I cellPos, CellState cell)
	{
		var insect = _insectScene.Instantiate<Insect>();

		// Random entry from screen edge
		Vector2 entryPos = GetRandomEntryPosition(plantWorldPos);

		insect.Initialize(data, plantWorldPos, entryPos, cellPos);
		_insectContainer.AddChild(insect);

		// Register slot occupancy and auto-vacate on departure
		cell.OccupySlot(insect);
		insect.TreeExiting += () => cell.VacateSlot(insect);

		GD.Print($"Spawned {data.DisplayName} ({data.MovementPattern}) at cell {cellPos} [{cell.OccupiedSlotCount}/{cell.MaxInsectSlots}]");
	}

	private Vector2 GetRandomEntryPosition(Vector2 targetPos)
	{
		float angle = _rng.RandfRange(0f, Mathf.Tau);
		return targetPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 400f;
	}

	private bool MatchesTimeOfDay(string insectTimeOfDay)
	{
		if (insectTimeOfDay == "both") return true;
		bool isDaytime = TimeManager.Instance.IsDaytime();
		return insectTimeOfDay == "day" ? isDaytime : !isDaytime;
	}

	private bool MeetsPlantRequirements(InsectData insect, HashSet<string> bloomingTypes)
	{
		if (insect.RequiredPlants == null || insect.RequiredPlants.Length == 0) return true;
		return insect.RequiredPlants.All(req => bloomingTypes.Contains(req));
	}

	private static bool MeetsWaterRequirements(InsectData insect, int waterTileCount)
	{
		return insect.RequiredWaterTiles <= 0 || waterTileCount >= insect.RequiredWaterTiles;
	}

	private InsectData WeightedRandomSelect(List<InsectData> candidates)
	{
		float total = candidates.Sum(c => c.SpawnWeight);
		float roll = _rng.RandfRange(0f, total);
		float cumulative = 0f;
		foreach (var c in candidates)
		{
			cumulative += c.SpawnWeight;
			if (roll < cumulative) return c;
		}
		return candidates[^1];
	}

}
