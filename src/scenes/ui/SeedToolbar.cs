using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectFlutter;

public partial class SeedToolbar : Control
{
	private string _selectedPlantId;
	private HBoxContainer _slotContainer;
	private readonly List<Button> _slotButtons = new();
	private List<PlantData> _availablePlants = new();

	private Action<GameStateChangedEvent> _onStateChanged;
	private Action<NectarChangedEvent> _onNectarChanged;
	private Action<ZoneChangedEvent> _onZoneChanged;
	private Action<SeedSelectedEvent> _onSeedSelected;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		BuildUI();

		_onStateChanged = OnStateChanged;
		_onNectarChanged = _ => UpdateButtonStates();
		_onZoneChanged = OnZoneChanged;
		_onSeedSelected = OnSeedSelected;
		EventBus.Subscribe(_onStateChanged);
		EventBus.Subscribe(_onNectarChanged);
		EventBus.Subscribe(_onZoneChanged);
		EventBus.Subscribe(_onSeedSelected);

		RebuildSlots(ZoneType.Starter);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onStateChanged);
		EventBus.Unsubscribe(_onNectarChanged);
		EventBus.Unsubscribe(_onZoneChanged);
		EventBus.Unsubscribe(_onSeedSelected);
	}

	private void OnZoneChanged(ZoneChangedEvent zoneEvent)
	{
		_selectedPlantId = null;
		EventBus.Publish(new SeedSelectedEvent(null));
		RebuildSlots(zoneEvent.To);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

		if (@event is InputEventKey { Pressed: true } keyEvent)
		{
			int slotIndex = keyEvent.Keycode switch
			{
				Key.Key1 => 0,
				Key.Key2 => 1,
				Key.Key3 => 2,
				Key.Key4 => 3,
				Key.Key5 => 4,
				Key.Key6 => 5,
				Key.Key7 => 6,
				Key.Key8 => 7,
				Key.Key9 => 8,
				_ => -1
			};

			if (slotIndex >= 0 && slotIndex < _availablePlants.Count)
			{
				SelectSeed(_availablePlants[slotIndex].Id);
				GetViewport().SetInputAsHandled();
			}

			if (keyEvent.Keycode == Key.Escape && _selectedPlantId != null)
			{
				_selectedPlantId = null;
				UpdateButtonStates();
				EventBus.Publish(new SeedSelectedEvent(null));
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void BuildUI()
	{
		var anchor = new CenterContainer();
		anchor.SetAnchorsPreset(LayoutPreset.BottomWide);
		anchor.OffsetTop = -120;
		anchor.OffsetBottom = -8;
		anchor.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(anchor);

		var background = new PanelContainer();
		var backgroundStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.15f, 0.12f, 0.08f, 0.75f),
		};
		backgroundStyle.SetCornerRadiusAll(10);
		backgroundStyle.SetContentMarginAll(8);
		background.AddThemeStyleboxOverride("panel", backgroundStyle);
		anchor.AddChild(background);

		_slotContainer = new HBoxContainer();
		_slotContainer.AddThemeConstantOverride("separation", 6);
		background.AddChild(_slotContainer);
	}

	private void RebuildSlots(ZoneType zone)
	{
		foreach (var child in _slotContainer.GetChildren())
			((Node)child).QueueFree();
		_slotButtons.Clear();

		_availablePlants = PlantRegistry.GetByZone(zone).ToList();

		for (int i = 0; i < _availablePlants.Count; i++)
		{
			var plant = _availablePlants[i];
			var button = CreateSlotButton(plant, i + 1);
			_slotContainer.AddChild(button);
			_slotButtons.Add(button);
		}

		UpdateButtonStates();
	}

	private Button CreateSlotButton(PlantData plant, int slotNumber)
	{
		var button = new Button
		{
			CustomMinimumSize = new Vector2(120, 90),
			MouseFilter = MouseFilterEnum.Stop,
			FocusMode = FocusModeEnum.None,
		};

		// Normal style
		var normalStyle = new StyleBoxFlat
		{
			BgColor = plant.DrawColor.Darkened(0.6f),
			BorderColor = new Color(0.4f, 0.35f, 0.25f),
		};
		normalStyle.SetBorderWidthAll(2);
		normalStyle.SetCornerRadiusAll(4);
		normalStyle.SetContentMarginAll(4);
		button.AddThemeStyleboxOverride("normal", normalStyle);

		// Hover style
		var hoverStyle = new StyleBoxFlat
		{
			BgColor = plant.DrawColor.Darkened(0.4f),
			BorderColor = new Color(0.9f, 0.8f, 0.4f),
		};
		hoverStyle.SetBorderWidthAll(2);
		hoverStyle.SetCornerRadiusAll(4);
		hoverStyle.SetContentMarginAll(4);
		button.AddThemeStyleboxOverride("hover", hoverStyle);

		button.Text = $"{slotNumber}: {plant.DisplayName}\n{plant.SeedCost} nectar";
		button.AddThemeFontSizeOverride("font_size", 15);
		button.AddThemeColorOverride("font_color", Colors.White);

		string plantId = plant.Id;
		button.Pressed += () => SelectSeed(plantId);

		return button;
	}

	private void SelectSeed(string plantId)
	{
		if (_selectedPlantId == plantId)
		{
			_selectedPlantId = null;
		}
		else
		{
			_selectedPlantId = plantId;
		}

		UpdateButtonStates();
		EventBus.Publish(new SeedSelectedEvent(_selectedPlantId));
	}

	private void UpdateButtonStates()
	{
		int nectar = GameManager.Instance.Nectar;

		for (int i = 0; i < _slotButtons.Count && i < _availablePlants.Count; i++)
		{
			var button = _slotButtons[i];
			var plant = _availablePlants[i];
			bool isSelected = _selectedPlantId == plant.Id;
			bool canAfford = nectar >= plant.SeedCost;

			// Selected style: yellow border
			var style = new StyleBoxFlat
			{
				BgColor = isSelected ? plant.DrawColor.Darkened(0.3f) : plant.DrawColor.Darkened(0.6f),
				BorderColor = isSelected ? new Color(1.0f, 0.9f, 0.2f) : new Color(0.4f, 0.35f, 0.25f),
			};
			style.SetBorderWidthAll(isSelected ? 3 : 2);
			style.SetCornerRadiusAll(4);
			style.SetContentMarginAll(4);
			button.AddThemeStyleboxOverride("normal", style);

			button.Modulate = canAfford ? Colors.White : new Color(1f, 1f, 1f, 0.5f);
		}
	}

	private void OnSeedSelected(SeedSelectedEvent seedEvent)
	{
		if (_selectedPlantId == seedEvent.PlantId) return;
		_selectedPlantId = seedEvent.PlantId;
		UpdateButtonStates();
	}

	private void OnStateChanged(GameStateChangedEvent stateEvent)
	{
		bool shouldShow = stateEvent.NewState == GameManager.GameState.Playing;
		Visible = shouldShow;

		// Deselect seed when leaving Playing state
		if (!shouldShow && _selectedPlantId != null)
		{
			_selectedPlantId = null;
			UpdateButtonStates();
			EventBus.Publish(new SeedSelectedEvent(null));
		}
	}
}
