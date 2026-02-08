using Godot;

public partial class GameManager : Node
{
	[Signal] public delegate void GameStateChangedEventHandler();

	public enum GameState { Playing, Paused, PhotoMode }

	public GameState CurrentState { get; private set; } = GameState.Playing;
	public ZoneType CurrentZone { get; set; } = ZoneType.Starter;
	public int Nectar { get; private set; }

	public void ChangeState(GameState newState)
	{
		CurrentState = newState;
		EmitSignal(SignalName.GameStateChanged);
	}

	public void AddNectar(int amount)
	{
		Nectar += amount;
	}

	public bool SpendNectar(int amount)
	{
		if (Nectar < amount) return false;
		Nectar -= amount;
		return true;
	}
}
