using System;
using Godot;
using ProjectFlutter;

public partial class HUD : Control
{
	private Label _nectarLabel;
	private Button _photoButton;
	private Button _journalButton;
	private Label _photoModeLabel;
	private AnalogClock _analogClock;

	private Action<NectarChangedEvent> _onNectarChanged;
	private Action<GameStateChangedEvent> _onStateChanged;

	public override void _Ready()
	{
		_nectarLabel = GetNode<Label>("NectarLabel");
		_photoButton = GetNode<Button>("PhotoButton");
		_journalButton = GetNode<Button>("JournalButton");
		_photoModeLabel = GetNode<Label>("PhotoModeLabel");

		// Create analog clock and anchor top-right
		_analogClock = new AnalogClock();
		_analogClock.LayoutMode = 1;
		_analogClock.AnchorsPreset = (int)LayoutPreset.TopRight;
		_analogClock.AnchorLeft = 1.0f;
		_analogClock.AnchorRight = 1.0f;
		_analogClock.OffsetLeft = -142f;
		_analogClock.OffsetTop = 4f;
		_analogClock.OffsetRight = -4f;
		_analogClock.OffsetBottom = 140f;
		AddChild(_analogClock);

		_photoButton.Pressed += OnPhotoButtonPressed;
		_journalButton.Pressed += OnJournalButtonPressed;

		_onNectarChanged = OnNectarChanged;
		_onStateChanged = OnStateChanged;

		EventBus.Subscribe(_onNectarChanged);
		EventBus.Subscribe(_onStateChanged);

		UpdateNectarDisplay();
		UpdatePhotoModeUI();
	}

	public override void _ExitTree()
	{
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
				case Key.F3:
					TimeManager.Instance.ToggleDebugSpeed(10.0f);
					GetViewport().SetInputAsHandled();
					break;
				case Key.F4:
					TimeManager.Instance.ToggleDebugSpeed(50.0f);
					GetViewport().SetInputAsHandled();
					break;
				case Key.F5:
					JournalManager.Instance.DebugFillJournal(53);
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

	private void OnNectarChanged(NectarChangedEvent evt) => UpdateNectarDisplay();

	private void UpdateNectarDisplay()
	{
		_nectarLabel.Text = $"Nectar: {GameManager.Instance.Nectar}";
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
