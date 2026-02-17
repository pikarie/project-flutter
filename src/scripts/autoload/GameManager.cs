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
	public int Nectar { get; private set; }

	private Action<SpeciesDiscoveredEvent> _onSpeciesDiscovered;
	private Action<PhotoTakenEvent> _onPhotoTaken;
	private Action<DayEndedEvent> _onDayEnded;

	public override void _Ready()
	{
		Instance = this;
		Nectar = StartingNectar;

		_onSpeciesDiscovered = OnSpeciesDiscovered;
		_onPhotoTaken = OnPhotoTaken;
		_onDayEnded = OnDayEnded;
		EventBus.Subscribe(_onSpeciesDiscovered);
		EventBus.Subscribe(_onPhotoTaken);
		EventBus.Subscribe(_onDayEnded);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onSpeciesDiscovered);
		EventBus.Unsubscribe(_onPhotoTaken);
		EventBus.Unsubscribe(_onDayEnded);
	}

	private void OnSpeciesDiscovered(SpeciesDiscoveredEvent discoveryEvent)
	{
		AddNectar(5);
		GD.Print($"Bonus: +5 nectar for discovering {discoveryEvent.InsectId}");
	}

	private void OnPhotoTaken(PhotoTakenEvent photoEvent)
	{
		if (photoEvent.StarRating >= 3 && photoEvent.IsNewDiscovery)
		{
			AddNectar(3);
			GD.Print($"Bonus: +3 nectar for first 3-star photo of {photoEvent.InsectId}");
		}
	}

	private void OnDayEnded(DayEndedEvent dayEvent)
	{
		AddNectar(2);
		GD.Print($"Bonus: +2 nectar for completing day {dayEvent.DayNumber}");
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
