using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class ShopUI : Control
{
	public static ShopUI Instance { get; private set; }

	private PanelContainer _panel;
	private VBoxContainer _itemList;
	private Label _titleLabel;
	private Button _closeButton;
	private Label _nectarLabel;

	// Lantern section
	private Button _lanternBuyButton;
	private Button _lanternToggleButton;

	// Sprinkler buttons
	private readonly Dictionary<int, Button> _sprinklerButtons = new();

	private Action<NectarChangedEvent> _onNectarChanged;
	private Action<GameStateChangedEvent> _onStateChanged;
	private Action<LanternToggledEvent> _onLanternToggled;

	// Sprinkler costs per tier (GDD v3.3 §4.8)
	public static readonly Dictionary<int, int> SprinklerCosts = new()
	{
		{ 1, 40 },   // 3×3
		{ 2, 120 },  // 5×5
		{ 3, 300 },  // 7×7
	};

	private static readonly Dictionary<int, string> SprinklerNames = new()
	{
		{ 1, "Sprinkler I (3x3)" },
		{ 2, "Sprinkler II (5x5)" },
		{ 3, "Sprinkler III (7x7)" },
	};

	private int _selectedSprinklerTier;
	private bool _placingMode;

	public override void _Ready()
	{
		Instance = this;

		// Fill entire parent so centered children actually center on screen
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;

		BuildPanel();

		_onNectarChanged = _ => UpdateDisplay();
		_onStateChanged = OnStateChanged;
		_onLanternToggled = _ => UpdateDisplay();
		EventBus.Subscribe(_onNectarChanged);
		EventBus.Subscribe(_onStateChanged);
		EventBus.Subscribe(_onLanternToggled);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onNectarChanged);
		EventBus.Unsubscribe(_onStateChanged);
		EventBus.Unsubscribe(_onLanternToggled);
	}

	private void BuildPanel()
	{
		_panel = new PanelContainer();
		_panel.SetAnchorsPreset(LayoutPreset.Center);
		_panel.OffsetLeft = -200;
		_panel.OffsetRight = 200;
		_panel.OffsetTop = -220;
		_panel.OffsetBottom = 220;

		var panelStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.1f, 0.08f, 0.06f, 0.95f),
			BorderColor = new Color(0.6f, 0.5f, 0.3f),
		};
		panelStyle.SetBorderWidthAll(2);
		panelStyle.SetCornerRadiusAll(10);
		panelStyle.SetContentMarginAll(20);
		_panel.AddThemeStyleboxOverride("panel", panelStyle);

		var outerLayout = new VBoxContainer();
		outerLayout.AddThemeConstantOverride("separation", 12);
		_panel.AddChild(outerLayout);

		// Title
		_titleLabel = new Label
		{
			Text = "Shop",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_titleLabel.AddThemeFontSizeOverride("font_size", 22);
		_titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.5f));
		outerLayout.AddChild(_titleLabel);

		// Nectar display
		_nectarLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_nectarLabel.AddThemeFontSizeOverride("font_size", 16);
		_nectarLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.6f));
		outerLayout.AddChild(_nectarLabel);

		// Separator
		outerLayout.AddChild(new HSeparator());

		// Item list
		_itemList = new VBoxContainer();
		_itemList.AddThemeConstantOverride("separation", 8);
		outerLayout.AddChild(_itemList);

		// -- Sprinklers section --
		var sprinklerHeader = new Label { Text = "Sprinklers" };
		sprinklerHeader.AddThemeFontSizeOverride("font_size", 16);
		sprinklerHeader.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
		_itemList.AddChild(sprinklerHeader);

		foreach (var (tier, cost) in SprinklerCosts)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);

			var label = new Label
			{
				Text = $"{SprinklerNames[tier]}",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};
			label.AddThemeFontSizeOverride("font_size", 14);
			row.AddChild(label);

			var button = new Button
			{
				Text = $"Buy ({cost})",
				CustomMinimumSize = new Vector2(100, 32),
				FocusMode = FocusModeEnum.None,
			};
			button.AddThemeFontSizeOverride("font_size", 13);
			int capturedTier = tier;
			button.Pressed += () => OnBuySprinkler(capturedTier);
			row.AddChild(button);
			_sprinklerButtons[tier] = button;

			_itemList.AddChild(row);
		}

		// Separator
		_itemList.AddChild(new HSeparator());

		// -- Lantern section --
		var lanternHeader = new Label { Text = "Lantern" };
		lanternHeader.AddThemeFontSizeOverride("font_size", 16);
		lanternHeader.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
		_itemList.AddChild(lanternHeader);

		var lanternRow = new HBoxContainer();
		lanternRow.AddThemeConstantOverride("separation", 8);

		var lanternLabel = new Label
		{
			Text = "Garden Lantern (night 3★)",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		lanternLabel.AddThemeFontSizeOverride("font_size", 14);
		lanternRow.AddChild(lanternLabel);

		_lanternBuyButton = new Button
		{
			Text = $"Buy ({GameManager.LanternCost})",
			CustomMinimumSize = new Vector2(100, 32),
			FocusMode = FocusModeEnum.None,
		};
		_lanternBuyButton.AddThemeFontSizeOverride("font_size", 13);
		_lanternBuyButton.Pressed += OnBuyLantern;
		lanternRow.AddChild(_lanternBuyButton);

		_lanternToggleButton = new Button
		{
			Text = "Toggle",
			CustomMinimumSize = new Vector2(80, 32),
			FocusMode = FocusModeEnum.None,
			Visible = false,
		};
		_lanternToggleButton.AddThemeFontSizeOverride("font_size", 13);
		_lanternToggleButton.Pressed += () => GameManager.Instance.ToggleLantern();
		lanternRow.AddChild(_lanternToggleButton);

		_itemList.AddChild(lanternRow);

		// Separator + Close
		outerLayout.AddChild(new HSeparator());

		_closeButton = new Button
		{
			Text = "Close (S / Esc)",
			CustomMinimumSize = new Vector2(120, 36),
			FocusMode = FocusModeEnum.None,
		};
		_closeButton.AddThemeFontSizeOverride("font_size", 14);
		_closeButton.Pressed += CloseShop;
		outerLayout.AddChild(_closeButton);

		AddChild(_panel);
	}

	public void OpenShop()
	{
		_placingMode = false;
		_selectedSprinklerTier = 0;
		GameManager.Instance.ChangeState(GameManager.GameState.Shop);
		Visible = true;
		UpdateDisplay();
	}

	public void CloseShop()
	{
		Visible = false;
		_placingMode = false;
		_selectedSprinklerTier = 0;
		if (GameManager.Instance.CurrentState == GameManager.GameState.Shop)
			GameManager.Instance.ChangeState(GameManager.GameState.Playing);
	}

	public bool IsPlacingSprinkler => _placingMode;
	public int SelectedSprinklerTier => _selectedSprinklerTier;

	public void EnterPlacingMode(int tier)
	{
		_selectedSprinklerTier = tier;
		_placingMode = true;
		Visible = false;
		if (GameManager.Instance.CurrentState == GameManager.GameState.Shop)
			GameManager.Instance.ChangeState(GameManager.GameState.Playing);
		GD.Print($"Place your {SprinklerNames[tier]} — click on a tile. Right-click to cancel.");
	}

	public void ExitPlacingMode()
	{
		_placingMode = false;
		_selectedSprinklerTier = 0;
	}

	private void OnBuySprinkler(int tier)
	{
		int cost = SprinklerCosts[tier];
		if (GameManager.Instance.Nectar < cost)
		{
			GD.Print($"Not enough nectar for {SprinklerNames[tier]}! Need {cost}");
			return;
		}

		if (!GameManager.Instance.SpendNectar(cost))
			return;

		GD.Print($"Bought {SprinklerNames[tier]} for {cost} nectar — place it!");
		EnterPlacingMode(tier);
	}

	private void OnBuyLantern()
	{
		GameManager.Instance.BuyLantern();
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		if (!Visible) return;

		_nectarLabel.Text = $"Nectar: {GameManager.Instance.Nectar}";

		// Sprinkler buttons
		foreach (var (tier, button) in _sprinklerButtons)
		{
			int cost = SprinklerCosts[tier];
			button.Disabled = GameManager.Instance.Nectar < cost;
		}

		// Lantern
		bool hasLantern = GameManager.Instance.HasLantern;
		_lanternBuyButton.Visible = !hasLantern;
		_lanternBuyButton.Disabled = GameManager.Instance.Nectar < GameManager.LanternCost;
		_lanternToggleButton.Visible = hasLantern;
		_lanternToggleButton.Text = GameManager.Instance.LanternActive ? "Turn Off" : "Turn On";
	}

	private void OnStateChanged(GameStateChangedEvent stateEvent)
	{
		if (stateEvent.NewState != GameManager.GameState.Shop && Visible)
		{
			Visible = false;
			_placingMode = false;
		}
	}
}
