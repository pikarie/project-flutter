using System;
using Godot;
using ProjectFlutter;

public partial class HUD : Control
{
	private Label _timeLabel;
	private Label _nectarLabel;
	private Button _speedButton;
	private Button _photoButton;
	private Button _journalButton;
	private Label _photoModeLabel;
	private int _currentSpeedIndex;
	private readonly float[] _speeds = { 1.0f, 2.0f, 3.0f, 10.0f };
	private readonly string[] _speedLabels = { "x1", "x2", "x3", "x10" };

	private Action<HourPassedEvent> _onHourPassed;
	private Action<TimeOfDayChangedEvent> _onPeriodChanged;
	private Action<NectarChangedEvent> _onNectarChanged;
	private Action<GameStateChangedEvent> _onStateChanged;

	public override void _Ready()
	{
		_timeLabel = GetNode<Label>("TimeLabel");
		_nectarLabel = GetNode<Label>("NectarLabel");
		_speedButton = GetNode<Button>("SpeedButton");
		_photoButton = GetNode<Button>("PhotoButton");
		_journalButton = GetNode<Button>("JournalButton");
		_photoModeLabel = GetNode<Label>("PhotoModeLabel");

		_speedButton.Pressed += OnSpeedButtonPressed;
		_photoButton.Pressed += OnPhotoButtonPressed;
		_journalButton.Pressed += OnJournalButtonPressed;

		_onHourPassed = OnHourPassed;
		_onPeriodChanged = OnPeriodChanged;
		_onNectarChanged = OnNectarChanged;
		_onStateChanged = OnStateChanged;

		EventBus.Subscribe(_onHourPassed);
		EventBus.Subscribe(_onPeriodChanged);
		EventBus.Subscribe(_onNectarChanged);
		EventBus.Subscribe(_onStateChanged);

		UpdateTimeDisplay();
		UpdateNectarDisplay();
		UpdateSpeedButton();
		UpdatePhotoModeUI();
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onHourPassed);
		EventBus.Unsubscribe(_onPeriodChanged);
		EventBus.Unsubscribe(_onNectarChanged);
		EventBus.Unsubscribe(_onStateChanged);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true } keyEvent)
		{
			switch (keyEvent.Keycode)
			{
				case Key.C:
					TogglePhotoMode();
					GetViewport().SetInputAsHandled();
					break;
				case Key.J:
					ToggleJournal();
					GetViewport().SetInputAsHandled();
					break;
				case Key.F1:
					ZoneManager.Instance.DebugUnlockAll();
					GetViewport().SetInputAsHandled();
					break;
				case Key.F2:
					DebugSpawnBee();
					GetViewport().SetInputAsHandled();
					break;
				case Key.Escape:
					if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
					{
						GameManager.Instance.ChangeState(GameManager.GameState.Playing);
						GetViewport().SetInputAsHandled();
					}
					break;
			}
		}
	}

	private void TogglePhotoMode()
	{
		var gameManager = GameManager.Instance;
		if (gameManager.CurrentState == GameManager.GameState.PhotoMode)
			gameManager.ChangeState(GameManager.GameState.Playing);
		else
			gameManager.ChangeState(GameManager.GameState.PhotoMode);
	}

	private void ToggleJournal()
	{
		var gameManager = GameManager.Instance;
		if (gameManager.CurrentState == GameManager.GameState.Journal)
			gameManager.ChangeState(GameManager.GameState.Playing);
		else
			gameManager.ChangeState(GameManager.GameState.Journal);
	}

	private void OnPhotoButtonPressed() => TogglePhotoMode();

	private void OnJournalButtonPressed() => ToggleJournal();

	private void OnStateChanged(GameStateChangedEvent evt) => UpdatePhotoModeUI();

	private void UpdatePhotoModeUI()
	{
		bool isPhoto = GameManager.Instance.CurrentState == GameManager.GameState.PhotoMode;
		_photoModeLabel.Visible = isPhoto;
		_photoButton.Text = isPhoto ? "Exit Photo" : "Photo";
	}

	private void OnSpeedButtonPressed()
	{
		_currentSpeedIndex = (_currentSpeedIndex + 1) % _speeds.Length;
		TimeManager.Instance.SetSpeed(_speeds[_currentSpeedIndex]);
		UpdateSpeedButton();
	}

	private void OnHourPassed(HourPassedEvent evt) => UpdateTimeDisplay();

	private void OnPeriodChanged(TimeOfDayChangedEvent evt) => UpdateTimeDisplay();

	private void OnNectarChanged(NectarChangedEvent evt) => UpdateNectarDisplay();

	private void UpdateTimeDisplay()
	{
		var timeManager = TimeManager.Instance;
		int hour = (int)(timeManager.CurrentTimeNormalized * 24.0f);
		int minute = (int)((timeManager.CurrentTimeNormalized * 24.0f - hour) * 60.0f);
		_timeLabel.Text = $"{hour:D2}:{minute:D2} ({timeManager.CurrentPeriod})";
	}

	private void UpdateNectarDisplay()
	{
		_nectarLabel.Text = $"Nectar: {GameManager.Instance.Nectar}";
	}

	private void UpdateSpeedButton()
	{
		_speedButton.Text = _speedLabels[_currentSpeedIndex];
	}

	private void DebugSpawnBee()
	{
		var camera = GetViewport().GetCamera2D();
		if (camera == null) return;

		// Spawn at camera center
		Vector2 spawnPosition = camera.GlobalPosition;

		var insectScene = GD.Load<PackedScene>("res://scenes/insects/Insect.tscn");
		var insect = insectScene.Instantiate<Insect>();

		var data = InsectRegistry.GetById("honeybee");
		if (data == null) return;

		// Initialize with very long visit time (static debug bee)
		insect.Initialize(data, spawnPosition, spawnPosition, Vector2I.Zero);

		// Find the active zone's InsectContainer
		var gameWorld = GetTree().Root.GetNodeOrNull("Main/GameWorld");
		if (gameWorld == null) return;

		Node insectContainer = null;
		foreach (var child in gameWorld.GetChildren())
		{
			if (child is Node2D zoneNode && zoneNode.Visible)
			{
				insectContainer = zoneNode.GetNodeOrNull("InsectContainer");
				break;
			}
		}

		if (insectContainer == null) return;
		insectContainer.AddChild(insect);
		GD.Print("Debug: Spawned static Honeybee at camera center");
	}
}
