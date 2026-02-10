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
	private List<InsectData> _testInsects;

	public override void _Ready()
	{
		_rng.Randomize();
		_gardenGrid = GetParent().GetNode<GardenGrid>("GardenGrid");
		_insectContainer = GetParent().GetNode<Node2D>("InsectContainer");
		_insectScene = GD.Load<PackedScene>("res://scenes/insects/Insect.tscn");

		_testInsects = CreateTestInsects();
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

		// Find blooming cells
		var bloomingCells = _gardenGrid.GetCells()
			.Where(kv => kv.Value.CurrentState == CellState.State.Blooming)
			.ToList();

		if (bloomingCells.Count == 0) return;

		// 40% quiet ticks for anticipation
		if (_rng.Randf() > 0.6f) return;

		// Pick a random blooming cell
		var target = bloomingCells[_rng.RandiRange(0, bloomingCells.Count - 1)];
		Vector2 plantWorldPos = _gardenGrid.GlobalPosition + _gardenGrid.GridToWorld(target.Key);

		// Filter eligible insects by time of day
		var eligible = _testInsects
			.Where(i => MatchesTimeOfDay(i.TimeOfDay))
			.ToList();

		if (eligible.Count == 0) return;

		// Weighted random selection
		var selected = WeightedRandomSelect(eligible);
		SpawnInsect(selected, plantWorldPos, target.Key);
	}

	private void SpawnInsect(InsectData data, Vector2 plantWorldPos, Vector2I cellPos)
	{
		var insect = _insectScene.Instantiate<Insect>();

		// Random entry from screen edge
		Vector2 entryPos = GetRandomEntryPosition(plantWorldPos);

		insect.Initialize(data, plantWorldPos, entryPos, cellPos);
		_insectContainer.AddChild(insect);

		GD.Print($"Spawned {data.DisplayName} ({data.MovementPattern}) at cell {cellPos}");
	}

	private Vector2 GetRandomEntryPosition(Vector2 targetPos)
	{
		// Pick a random direction and place the entry 400px away
		float angle = _rng.RandfRange(0f, Mathf.Tau);
		return targetPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 400f;
	}

	private bool MatchesTimeOfDay(string insectTimeOfDay)
	{
		if (insectTimeOfDay == "both") return true;
		bool isDaytime = TimeManager.Instance.IsDaytime();
		return insectTimeOfDay == "day" ? isDaytime : !isDaytime;
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

	private static List<InsectData> CreateTestInsects()
	{
		return new List<InsectData>
		{
			CreateInsect("honeybee", "Honeybee", MovementPattern.Hover,
				"day", 1.0f, 90f, 240f, 30f),

			CreateInsect("cabbage_white", "Cabbage White", MovementPattern.Flutter,
				"day", 0.8f, 60f, 180f, 40f),

			CreateInsect("ladybug", "Ladybug", MovementPattern.Crawl,
				"day", 0.7f, 120f, 300f, 15f),

			CreateInsect("sphinx_moth", "Sphinx Moth", MovementPattern.Erratic,
				"night", 0.5f, 45f, 120f, 50f),
		};
	}

	private static InsectData CreateInsect(
		string id, string name, MovementPattern pattern,
		string timeOfDay, float spawnWeight,
		float visitMin, float visitMax, float speed)
	{
		var data = new InsectData
		{
			Id = id,
			DisplayName = name,
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
		};
		return data;
	}
}
