using Godot;
using ProjectFlutter;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public enum GameState { Playing, Paused, PhotoMode, Journal, Shop }

	public GameState CurrentState { get; private set; } = GameState.Playing;
	public ZoneType CurrentZone { get; set; } = ZoneType.Starter;
	public const int StartingNectar = 25;
	public int Nectar { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		Nectar = StartingNectar;
	}

	public void ChangeState(GameState newState)
	{
		CurrentState = newState;

		// Wire pause to TimeManager: PhotoMode/Journal/Shop/Paused freeze time
		bool shouldPause = newState is GameState.PhotoMode
			or GameState.Journal
			or GameState.Shop
			or GameState.Paused;
		TimeManager.Instance.Paused = shouldPause;

		EventBus.Publish(new PauseToggledEvent(shouldPause));
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
