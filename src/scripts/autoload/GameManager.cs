using System;
using Godot;
using ProjectFlutter;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public enum GameState { Playing, Paused, PhotoMode, Journal, Shop }

	public GameState CurrentState { get; private set; } = GameState.Playing;
	public ZoneType CurrentZone { get; set; } = ZoneType.Starter;
	public const int StartingNectar = 25;
	public const int LanternCost = 50;
	public int Nectar { get; private set; }

	// Lantern: purchased once, toggled on/off at night
	public bool HasLantern { get; private set; }
	public bool LanternActive { get; private set; }

	public bool BuyLantern()
	{
		if (HasLantern) return false;
		if (!SpendNectar(LanternCost)) return false;
		HasLantern = true;
		GD.Print("Purchased garden lantern!");
		return true;
	}

	public void ToggleLantern()
	{
		if (!HasLantern) return;
		LanternActive = !LanternActive;
		EventBus.Publish(new LanternToggledEvent(LanternActive));
		GD.Print($"Lantern {(LanternActive ? "ON" : "OFF")}");
	}

	private Action<SpeciesDiscoveredEvent> _onSpeciesDiscovered;

	public override void _Ready()
	{
		Instance = this;
		Nectar = StartingNectar;

		_onSpeciesDiscovered = OnSpeciesDiscovered;
		EventBus.Subscribe(_onSpeciesDiscovered);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onSpeciesDiscovered);
	}

	private void OnSpeciesDiscovered(SpeciesDiscoveredEvent discoveryEvent)
	{
		AddNectar(10);
		GD.Print($"Bonus: +10 nectar for discovering {discoveryEvent.InsectId}");
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
