using System;
using Godot;
using ProjectFlutter;

public partial class HUD : Control
{
	private Label _nectarLabel;
	private Button _photoButton;
	private Button _journalButton;
	private Button _shopButton;
	private Label _photoModeLabel;
	private AnalogClock _analogClock;
	private ShopUI _shopUI;
	private Label _placingLabel;

	private Action<NectarChangedEvent> _onNectarChanged;
	private Action<GameStateChangedEvent> _onStateChanged;

	public override void _Ready()
	{
		_nectarLabel = GetNode<Label>("NectarLabel");
		_photoButton = GetNode<Button>("PhotoButton");
		_journalButton = GetNode<Button>("JournalButton");
		_shopButton = GetNode<Button>("ShopButton");
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

		// Create ShopUI overlay (full-rect so centered panel works)
		_shopUI = new ShopUI();
		_shopUI.LayoutMode = 1;
		_shopUI.AnchorsPreset = (int)LayoutPreset.FullRect;
		_shopUI.AnchorRight = 1.0f;
		_shopUI.AnchorBottom = 1.0f;
		_shopUI.OffsetRight = 0;
		_shopUI.OffsetBottom = 0;
		AddChild(_shopUI);

		// Sprinkler placing mode label
		_placingLabel = new Label
		{
			Text = "Click to place sprinkler — Right-click to cancel",
			Visible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_placingLabel.SetAnchorsPreset(LayoutPreset.CenterTop);
		_placingLabel.OffsetLeft = -180;
		_placingLabel.OffsetRight = 180;
		_placingLabel.OffsetTop = 45;
		_placingLabel.OffsetBottom = 70;
		_placingLabel.AddThemeFontSizeOverride("font_size", 15);
		_placingLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 1f));
		AddChild(_placingLabel);

		_photoButton.Pressed += OnPhotoButtonPressed;
		_journalButton.Pressed += OnJournalButtonPressed;
		_shopButton.Pressed += () => ToggleShop();

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
				case Key.S:
					ToggleShop();
					GetViewport().SetInputAsHandled();
					break;
				case Key.L:
					if (GameManager.Instance.HasLantern)
					{
						GameManager.Instance.ToggleLantern();
						GetViewport().SetInputAsHandled();
					}
					break;
				case Key.Space:
					TimeManager.Instance.CycleSpeed();
					GetViewport().SetInputAsHandled();
					break;
				case Key.Q:
					ZoneManager.Instance.CycleZonePrevious();
					GetViewport().SetInputAsHandled();
					break;
				case Key.E:
					ZoneManager.Instance.CycleZoneNext();
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
				case Key.F6:
					GameManager.Instance.AddNectar(500);
					GD.Print("DEBUG: +500 nectar");
					GetViewport().SetInputAsHandled();
					break;
				case Key.Escape:
					if (_shopUI.IsPlacingSprinkler)
					{
						_shopUI.ExitPlacingMode();
						_placingLabel.Visible = false;
						GetViewport().SetInputAsHandled();
					}
					else if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
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

	private void ToggleShop()
	{
		var gameManager = GameManager.Instance;
		if (gameManager.CurrentState == GameManager.GameState.Shop)
			_shopUI.CloseShop();
		else
			_shopUI.OpenShop();
	}

	private void OnPhotoButtonPressed() => TogglePhotoMode();

	private void OnJournalButtonPressed() => ToggleJournal();

	private void OnStateChanged(GameStateChangedEvent evt)
	{
		UpdatePhotoModeUI();
		_placingLabel.Visible = _shopUI.IsPlacingSprinkler;
	}

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

		var data = InsectRegistry.GetById("western_honeybee");
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
		GD.Print("Debug: Spawned static Western Honeybee at camera center");
	}
}
