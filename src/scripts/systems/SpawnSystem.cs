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
		if (TimeManager.Instance.Paused) return;
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
		// Population cap — dynamic based on zone expansion tier
		int insectCap = ZoneManager.Instance.GetInsectCap(GameManager.Instance.CurrentZone);
		if (_insectContainer.GetChildCount() >= insectCap) return;

		// 40% quiet ticks for anticipation
		if (_rng.Randf() > 0.6f) return;

		var allCells = _gardenGrid.GetCells();

		// Find blooming cells with available insect slots
		var bloomingCells = allCells
			.Where(kv => kv.Value.CurrentState == CellState.State.Blooming
						 && kv.Value.HasAvailableSlot())
			.ToList();

		// Find water tiles with available insect slots (for aquatic insects)
		var waterCells = allCells
			.Where(kv => kv.Value.IsWater && kv.Value.HasAvailableSlot())
			.ToList();

		// Find log tiles with available insect slots (for decomposition insects)
		var logCells = allCells
			.Where(kv => kv.Value.HasLog && kv.Value.HasAvailableSlot())
			.ToList();

		// Find heated stone tiles with available slots (for heat-loving insects)
		var stoneCells = allCells
			.Where(kv => kv.Value.HasHeatedStone && kv.Value.HasAvailableSlot())
			.ToList();

		// Check if any UV lamp exists in zone (for tropical moths)
		bool hasUVLamp = allCells.Any(kv => kv.Value.HasUVLamp);

		if (bloomingCells.Count == 0 && waterCells.Count == 0
			&& logCells.Count == 0 && stoneCells.Count == 0) return;

		// Build set of all blooming plant types for attraction matching
		var bloomingPlantTypes = allCells
			.Where(kv => kv.Value.CurrentState == CellState.State.Blooming)
			.Select(kv => kv.Value.PlantType)
			.Where(pt => !string.IsNullOrEmpty(pt))
			.ToHashSet();

		// Filter eligible insects
		int waterTileCount = _gardenGrid.CountWaterTiles();
		int maxDecompositionStage = logCells.Count > 0
			? logCells.Max(kv => kv.Value.DecompositionStage) : -1;
		float maxStoneHeat = stoneCells.Count > 0
			? stoneCells.Max(kv => kv.Value.StoneHeat) : 0f;
		var eligible = InsectRegistry.AllSpecies
			.Where(i => MatchesTimeOfDay(i.TimeOfDay))
			.Where(i => i.Zone == GameManager.Instance.CurrentZone || i.Zone == ZoneType.Starter)
			.Where(i => MeetsPlantRequirements(i, bloomingPlantTypes))
			.Where(i => MeetsWaterRequirements(i, waterTileCount))
			.Where(i => MeetsDecompositionRequirements(i, maxDecompositionStage))
			.Where(i => MeetsHeatedStoneRequirements(i, maxStoneHeat))
			.Where(i => MeetsUVLampRequirements(i, hasUVLamp))
			.ToList();

		if (eligible.Count == 0) return;

		// Weighted random selection
		var selected = WeightedRandomSelect(eligible);

		// Choose spawn target based on insect type
		bool isAquatic = selected.RequiredWaterTiles > 0
			&& (selected.RequiredPlants == null || selected.RequiredPlants.Length == 0);
		bool isDecomposition = selected.RequiredDecompositionStage >= 0;
		bool isHeatLover = selected.RequiresHeatedStone;

		KeyValuePair<Vector2I, CellState> target;
		if (isHeatLover && stoneCells.Count > 0)
		{
			var warmStones = stoneCells.Where(kv => kv.Value.StoneHeat >= 0.5f).ToList();
			if (warmStones.Count == 0) return;
			target = warmStones[_rng.RandiRange(0, warmStones.Count - 1)];
		}
		else if (isDecomposition && logCells.Count > 0)
		{
			// Pick a log with sufficient decomposition stage
			var validLogs = logCells
				.Where(kv => kv.Value.DecompositionStage >= selected.RequiredDecompositionStage)
				.ToList();
			if (validLogs.Count == 0) return;
			target = validLogs[_rng.RandiRange(0, validLogs.Count - 1)];
		}
		else if (isAquatic && waterCells.Count > 0)
		{
			target = waterCells[_rng.RandiRange(0, waterCells.Count - 1)];
		}
		else if (bloomingCells.Count > 0)
		{
			target = bloomingCells[_rng.RandiRange(0, bloomingCells.Count - 1)];
		}
		else
		{
			return;
		}

		Vector2 spawnWorldPos = _gardenGrid.GlobalPosition + _gardenGrid.GridToWorld(target.Key);
		SpawnInsect(selected, spawnWorldPos, target.Key, target.Value);
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

	private static bool MeetsDecompositionRequirements(InsectData insect, int maxDecompositionStage)
	{
		if (insect.RequiredDecompositionStage < 0) return true;
		return maxDecompositionStage >= insect.RequiredDecompositionStage;
	}

	private static bool MeetsHeatedStoneRequirements(InsectData insect, float maxStoneHeat)
	{
		if (!insect.RequiresHeatedStone) return true;
		return maxStoneHeat >= 0.5f;
	}

	private static bool MeetsUVLampRequirements(InsectData insect, bool hasUVLamp)
	{
		if (!insect.RequiresUVLamp) return true;
		return hasUVLamp;
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
