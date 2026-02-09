using Godot;

public class CellState
{
	public enum State { Empty, Tilled, Planted, Watered, Growing, Blooming }

	public State CurrentState { get; set; } = State.Empty;
	public string PlantType { get; set; } = "";
	public int GrowthStage { get; set; }
	public bool IsWatered { get; set; }
	public Node2D PlantNode { get; set; }

	public bool CanPlant() => CurrentState is State.Tilled or State.Watered;
}
