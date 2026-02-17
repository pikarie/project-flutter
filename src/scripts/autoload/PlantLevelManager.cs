using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class PlantLevelManager : Node
{
	public static PlantLevelManager Instance { get; private set; }

	private static readonly int[] LevelThresholds = { 0, 5, 15, 30, 50 };
	private static readonly float[] NectarMultipliers = { 1.0f, 1.25f, 1.5f, 1.75f, 2.0f };

	private readonly Dictionary<string, int> _harvestCounts = new();

	private Action<PlantHarvestedEvent> _onPlantHarvested;

	public override void _Ready()
	{
		Instance = this;

		_onPlantHarvested = OnPlantHarvested;
		EventBus.Subscribe(_onPlantHarvested);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onPlantHarvested);
	}

	private void OnPlantHarvested(PlantHarvestedEvent harvestEvent)
	{
		string plantId = harvestEvent.PlantType;
		if (string.IsNullOrEmpty(plantId)) return;

		int previousLevel = GetLevel(plantId);

		_harvestCounts.TryGetValue(plantId, out int count);
		_harvestCounts[plantId] = count + 1;

		int newLevel = GetLevel(plantId);
		if (newLevel > previousLevel)
		{
			EventBus.Publish(new PlantLevelUpEvent(plantId, newLevel));
			var plantData = PlantRegistry.GetById(plantId);
			GD.Print($"{plantData?.DisplayName ?? plantId} reached level {newLevel}! (×{GetNectarMultiplier(plantId):F2} nectar)");
		}
	}

	public int GetHarvestCount(string plantId)
	{
		return _harvestCounts.TryGetValue(plantId, out int count) ? count : 0;
	}

	public int GetLevel(string plantId)
	{
		int count = GetHarvestCount(plantId);
		for (int i = LevelThresholds.Length - 1; i >= 0; i--)
		{
			if (count >= LevelThresholds[i]) return i + 1;
		}
		return 1;
	}

	public float GetNectarMultiplier(string plantId)
	{
		int level = GetLevel(plantId);
		return NectarMultipliers[Mathf.Clamp(level - 1, 0, NectarMultipliers.Length - 1)];
	}
}
