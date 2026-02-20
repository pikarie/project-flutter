using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class ZoneSelector : Control
{
	private readonly Dictionary<ZoneType, Button> _tabButtons = new();
	private PanelContainer _unlockPanel;
	private Label _unlockRequirementsLabel;
	private Button _unlockButton;
	private ZoneType _pendingUnlockZone;

	private Action<ZoneChangedEvent> _onZoneChanged;
	private Action<ZoneUnlockedEvent> _onZoneUnlocked;
	private Action<NectarChangedEvent> _onNectarChanged;
	private Action<JournalUpdatedEvent> _onJournalUpdated;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		BuildTabs();
		BuildUnlockPanel();

		_onZoneChanged = _ => UpdateTabs();
		_onZoneUnlocked = _ => UpdateTabs();
		_onNectarChanged = _ => UpdateUnlockPanel();
		_onJournalUpdated = _ => UpdateTabs();
		EventBus.Subscribe(_onZoneChanged);
		EventBus.Subscribe(_onZoneUnlocked);
		EventBus.Subscribe(_onNectarChanged);
		EventBus.Subscribe(_onJournalUpdated);

		UpdateTabs();
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onZoneChanged);
		EventBus.Unsubscribe(_onZoneUnlocked);
		EventBus.Unsubscribe(_onNectarChanged);
		EventBus.Unsubscribe(_onJournalUpdated);
	}

	private static readonly Dictionary<ZoneType, string> TabLabels = new()
	{
		{ ZoneType.Starter, "Garden" },
		{ ZoneType.Meadow, "Meadow" },
		{ ZoneType.Forest, "Forest" },
		{ ZoneType.DeepWood, "Deep Wood" },
		{ ZoneType.RockGarden, "Rock Garden" },
		{ ZoneType.Pond, "Pond" },
		{ ZoneType.Tropical, "Tropical" },
	};

	private HBoxContainer _tabContainer;

	private void BuildTabs()
	{
		_tabContainer = new HBoxContainer
		{
			Position = new Vector2(10, 50),
		};
		_tabContainer.AddThemeConstantOverride("separation", 6);
		AddChild(_tabContainer);

		foreach (var (zone, label) in TabLabels)
		{
			CreateTab(_tabContainer, zone, label);
		}
	}

	private void CreateTab(HBoxContainer parent, ZoneType zone, string label)
	{
		var button = new Button
		{
			Text = label,
			CustomMinimumSize = new Vector2(zone == ZoneType.Tropical ? 160 : 110, 36),
			ClipText = false,
			FocusMode = FocusModeEnum.None,
		};
		button.AddThemeFontSizeOverride("font_size", 14);

		button.Pressed += () => OnTabPressed(zone);
		parent.AddChild(button);
		_tabButtons[zone] = button;
	}

	private void BuildUnlockPanel()
	{
		_unlockPanel = new PanelContainer { Visible = false };
		_unlockPanel.SetAnchorsPreset(LayoutPreset.Center);
		_unlockPanel.OffsetLeft = -180;
		_unlockPanel.OffsetRight = 180;
		_unlockPanel.OffsetTop = -80;
		_unlockPanel.OffsetBottom = 80;

		var panelStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.12f, 0.1f, 0.08f, 0.9f),
			BorderColor = new Color(0.6f, 0.5f, 0.3f),
		};
		panelStyle.SetBorderWidthAll(2);
		panelStyle.SetCornerRadiusAll(8);
		panelStyle.SetContentMarginAll(16);
		_unlockPanel.AddThemeStyleboxOverride("panel", panelStyle);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 12);
		_unlockPanel.AddChild(layout);

		_unlockRequirementsLabel = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.Word,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_unlockRequirementsLabel.AddThemeColorOverride("font_color", Colors.White);
		_unlockRequirementsLabel.AddThemeFontSizeOverride("font_size", 15);
		layout.AddChild(_unlockRequirementsLabel);

		var buttonRow = new HBoxContainer();
		buttonRow.AddThemeConstantOverride("separation", 12);
		buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
		layout.AddChild(buttonRow);

		_unlockButton = new Button
		{
			Text = "Unlock",
			CustomMinimumSize = new Vector2(100, 36),
			FocusMode = FocusModeEnum.None,
		};
		_unlockButton.Pressed += OnUnlockPressed;
		buttonRow.AddChild(_unlockButton);

		var cancelButton = new Button
		{
			Text = "Cancel",
			CustomMinimumSize = new Vector2(100, 36),
			FocusMode = FocusModeEnum.None,
		};
		cancelButton.Pressed += () => _unlockPanel.Visible = false;
		buttonRow.AddChild(cancelButton);

		AddChild(_unlockPanel);
	}

	private void OnTabPressed(ZoneType zone)
	{
		var zoneManager = ZoneManager.Instance;
		if (zoneManager.IsUnlocked(zone))
		{
			zoneManager.SwitchToZone(zone);
			_unlockPanel.Visible = false;
		}
		else
		{
			ShowUnlockPanel(zone);
		}
	}

	private void ShowUnlockPanel(ZoneType zone)
	{
		_pendingUnlockZone = zone;
		var (name, _, _, cost, journalRequired) = ZoneManager.ZoneConfig[zone];

		int currentNectar = GameManager.Instance.Nectar;
		int currentJournal = JournalManager.Instance.GetDiscoveredCount();

		string nectarStatus = currentNectar >= cost ? "OK" : $"{currentNectar}/{cost}";
		string journalStatus = currentJournal >= journalRequired ? "OK" : $"{currentJournal}/{journalRequired}";

		_unlockRequirementsLabel.Text = $"Unlock {name}?\n"
			+ $"Nectar: {cost} ({nectarStatus})\n"
			+ $"Journal entries: {journalRequired} ({journalStatus})";

		_unlockButton.Disabled = !ZoneManager.Instance.CanUnlock(zone);
		_unlockPanel.Visible = true;
	}

	private void UpdateUnlockPanel()
	{
		if (_unlockPanel.Visible)
			ShowUnlockPanel(_pendingUnlockZone);
	}

	private void OnUnlockPressed()
	{
		if (ZoneManager.Instance.TryUnlock(_pendingUnlockZone))
		{
			_unlockPanel.Visible = false;
			if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
				GameManager.Instance.ChangeState(GameManager.GameState.Playing);
			ZoneManager.Instance.SwitchToZone(_pendingUnlockZone);
		}
	}

	private void UpdateTabs()
	{
		var zoneManager = ZoneManager.Instance;
		int journalCount = JournalManager.Instance.GetDiscoveredCount();

		foreach (var (zone, button) in _tabButtons)
		{
			var (name, _, _, _, journalRequired) = ZoneManager.ZoneConfig[zone];

			// Hide Tropical until player has enough journal entries to see it
			if (zone == ZoneType.Tropical)
			{
				button.Visible = journalCount >= journalRequired;
				if (!button.Visible) continue;
			}

			bool unlocked = zoneManager.IsUnlocked(zone);
			bool active = zone == zoneManager.ActiveZone;

			var style = new StyleBoxFlat
			{
				BgColor = active ? new Color(0.3f, 0.5f, 0.25f) : (unlocked ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : new Color(0.15f, 0.15f, 0.15f, 0.6f)),
				BorderColor = active ? new Color(0.6f, 0.9f, 0.4f) : new Color(0.4f, 0.35f, 0.25f),
			};
			style.SetBorderWidthAll(active ? 2 : 1);
			style.SetCornerRadiusAll(4);
			style.SetContentMarginAll(6);
			button.AddThemeStyleboxOverride("normal", style);

			button.AddThemeColorOverride("font_color",
				active ? Colors.White : (unlocked ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.5f, 0.5f, 0.5f)));

			button.Text = unlocked ? name : $"{name} (locked)";
		}
	}
}
