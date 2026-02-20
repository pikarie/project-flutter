using Godot;
using System.Collections.Generic;

public class CellState
{
	public enum State { Empty, Tilled, Planted, Watered, Growing, Blooming }

	public State CurrentState { get; set; } = State.Empty;
	public string PlantType { get; set; } = "";
	public int GrowthStage { get; set; }
	public bool IsWatered { get; set; }
	public bool IsWater { get; set; }
	public Node2D PlantNode { get; set; }

	// Sprinkler
	public bool HasSprinkler { get; set; }
	public int SprinklerTier { get; set; } // 1=3×3, 2=5×5, 3=7×7

	// Log / decomposition (Deep Wood zone)
	public bool HasLog { get; set; }
	public int DecompositionStage { get; set; } // 0=fresh, 1=moldy, 2=rotten
	public float DecompositionTimer { get; set; }

	// Heated stone (Rock Garden zone)
	public bool HasHeatedStone { get; set; }
	public float StoneHeat { get; set; } // 0.0–1.0, rises in day, falls at night

	// UV Lamp (Tropical zone)
	public bool HasUVLamp { get; set; }

	// Insect slot tracking
	public int MaxInsectSlots { get; set; } = 2;
	private readonly List<Node2D> _occupyingInsects = new();

	public bool CanPlant() => CurrentState is State.Tilled or State.Watered;

	public bool HasAvailableSlot() => _occupyingInsects.Count < MaxInsectSlots;

	public int OccupiedSlotCount => _occupyingInsects.Count;

	public void OccupySlot(Node2D insect)
	{
		if (_occupyingInsects.Count < MaxInsectSlots)
			_occupyingInsects.Add(insect);
	}

	public void VacateSlot(Node2D insect)
	{
		_occupyingInsects.Remove(insect);
	}

	public void ClearSlots()
	{
		_occupyingInsects.Clear();
	}
}
