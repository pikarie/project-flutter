using Godot;
using ProjectFlutter;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public enum GameState { Playing, Paused, PhotoMode }

	public GameState CurrentState { get; private set; } = GameState.Playing;
	public ZoneType CurrentZone { get; set; } = ZoneType.Starter;
	public int Nectar { get; private set; }

	public override void _Ready() => Instance = this;

	public void ChangeState(GameState newState)
	{
		CurrentState = newState;
		EventBus.Publish(new GameStateChangedEvent(newState));
	}

	public void AddNectar(int amount)
	{
		Nectar += amount;
		EventBus.Publish(new NectarChangedEvent(Nectar));
	}

	public bool SpendNectar(int amount)
	{
		if (Nectar < amount) return false;
		Nectar -= amount;
		EventBus.Publish(new NectarChangedEvent(Nectar));
		return true;
	}
}
