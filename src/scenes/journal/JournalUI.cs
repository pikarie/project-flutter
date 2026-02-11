using System;
using Godot;
using ProjectFlutter;

public partial class JournalUI : Control
{
	private ScrollContainer _gridView;
	private GridContainer _gridContainer;
	private VBoxContainer _detailView;
	private Label _footerLabel;
	private ColorRect _detailPortrait;
	private Label _detailName;
	private Label _detailStars;
	private Label _detailDescription;
	private Label _detailHint;

	private Action<SpeciesDiscoveredEvent> _onDiscovered;
	private Action<JournalUpdatedEvent> _onUpdated;
	private Action<GameStateChangedEvent> _onStateChanged;

	public override void _Ready()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;

		BuildUI();

		_onDiscovered = _ => RefreshGrid();
		_onUpdated = _ => RefreshGrid();
		_onStateChanged = OnStateChanged;
		EventBus.Subscribe(_onDiscovered);
		EventBus.Subscribe(_onUpdated);
		EventBus.Subscribe(_onStateChanged);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onDiscovered);
		EventBus.Unsubscribe(_onUpdated);
		EventBus.Unsubscribe(_onStateChanged);
	}

	private void OnStateChanged(GameStateChangedEvent stateEvent)
	{
		bool shouldShow = stateEvent.NewState == GameManager.GameState.Journal;
		Visible = shouldShow;
		if (shouldShow)
		{
			RefreshGrid();
			ShowGrid();
		}
	}

	private void BuildUI()
	{
		// Background overlay
		var background = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.6f),
			MouseFilter = MouseFilterEnum.Stop,
		};
		background.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(background);

		// Center container
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		// Panel
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(800, 550) };
		var panelStyle = new StyleBoxFlat { BgColor = new Color(0.96f, 0.93f, 0.88f), BorderColor = new Color(0.55f, 0.45f, 0.33f) };
		panelStyle.SetBorderWidthAll(3);
		panelStyle.SetCornerRadiusAll(8);
		panelStyle.SetContentMarginAll(20);
		panel.AddThemeStyleboxOverride("panel", panelStyle);
		center.AddChild(panel);

		var mainLayout = new VBoxContainer();
		mainLayout.AddThemeConstantOverride("separation", 12);
		panel.AddChild(mainLayout);

		// Header
		BuildHeader(mainLayout);

		// Grid view
		_gridView = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		mainLayout.AddChild(_gridView);

		_gridContainer = new GridContainer { Columns = 4 };
		_gridContainer.AddThemeConstantOverride("h_separation", 12);
		_gridContainer.AddThemeConstantOverride("v_separation", 12);
		_gridView.AddChild(_gridContainer);

		// Detail view
		BuildDetailView(mainLayout);

		// Footer
		_footerLabel = new Label();
		_footerLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.4f, 0.3f));
		mainLayout.AddChild(_footerLabel);
	}

	private void BuildHeader(VBoxContainer parent)
	{
		var header = new HBoxContainer();
		parent.AddChild(header);

		var title = new Label
		{
			Text = "Field Journal",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		title.AddThemeFontSizeOverride("font_size", 28);
		title.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.15f));
		header.AddChild(title);

		var closeButton = new Button { Text = "Close" };
		closeButton.Pressed += () => GameManager.Instance.ChangeState(GameManager.GameState.Playing);
		header.AddChild(closeButton);
	}

	private void BuildDetailView(VBoxContainer parent)
	{
		_detailView = new VBoxContainer
		{
			Visible = false,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_detailView.AddThemeConstantOverride("separation", 10);
		parent.AddChild(_detailView);

		var backButton = new Button { Text = "< Back" };
		backButton.Pressed += ShowGrid;
		_detailView.AddChild(backButton);

		_detailPortrait = new ColorRect { CustomMinimumSize = new Vector2(120, 120) };
		_detailView.AddChild(_detailPortrait);

		_detailName = new Label();
		_detailName.AddThemeFontSizeOverride("font_size", 24);
		_detailName.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.15f));
		_detailView.AddChild(_detailName);

		_detailStars = new Label();
		_detailStars.AddThemeFontSizeOverride("font_size", 20);
		_detailStars.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.1f));
		_detailView.AddChild(_detailStars);

		_detailDescription = new Label { AutowrapMode = TextServer.AutowrapMode.Word };
		_detailDescription.AddThemeColorOverride("font_color", new Color(0.3f, 0.25f, 0.2f));
		_detailView.AddChild(_detailDescription);

		_detailHint = new Label { AutowrapMode = TextServer.AutowrapMode.Word };
		_detailHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.4f));
		_detailView.AddChild(_detailHint);
	}

	private void RefreshGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
			child.QueueFree();

		var journal = JournalManager.Instance;
		foreach (var species in InsectRegistry.AllSpecies)
		{
			bool discovered = journal.IsDiscovered(species.Id);
			int stars = journal.GetStarRating(species.Id);
			_gridContainer.AddChild(CreateCell(species, discovered, stars));
		}

		_footerLabel.Text = $"Discovered: {journal.GetDiscoveredCount()} / {InsectRegistry.TotalSpeciesCount}";
	}

	private Button CreateCell(InsectData species, bool discovered, int stars)
	{
		var cell = new Button { CustomMinimumSize = new Vector2(170, 140) };

		var speciesColor = GetSpeciesColor(species.Id);

		var normalStyle = new StyleBoxFlat
		{
			BgColor = discovered ? speciesColor.Lightened(0.5f) : new Color(0.7f, 0.7f, 0.7f),
			BorderColor = new Color(0.55f, 0.45f, 0.33f),
		};
		normalStyle.SetBorderWidthAll(2);
		normalStyle.SetCornerRadiusAll(6);
		normalStyle.SetContentMarginAll(10);
		cell.AddThemeStyleboxOverride("normal", normalStyle);

		var hoverStyle = new StyleBoxFlat
		{
			BgColor = discovered ? speciesColor.Lightened(0.4f) : new Color(0.65f, 0.65f, 0.65f),
			BorderColor = new Color(0.85f, 0.65f, 0.1f),
		};
		hoverStyle.SetBorderWidthAll(3);
		hoverStyle.SetCornerRadiusAll(6);
		hoverStyle.SetContentMarginAll(10);
		cell.AddThemeStyleboxOverride("hover", hoverStyle);
		cell.AddThemeStyleboxOverride("pressed", hoverStyle);

		if (discovered)
		{
			string starText = new string('★', stars) + new string('☆', 3 - stars);
			cell.Text = $"{species.DisplayName}\n{starText}";
		}
		else
		{
			cell.Text = "???";
		}

		cell.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.15f));
		cell.AddThemeFontSizeOverride("font_size", 16);

		string speciesId = species.Id;
		cell.Pressed += () => ShowDetail(speciesId);

		return cell;
	}

	private void ShowDetail(string speciesId)
	{
		var species = InsectRegistry.GetById(speciesId);
		if (species == null) return;

		bool discovered = JournalManager.Instance.IsDiscovered(speciesId);
		int stars = JournalManager.Instance.GetStarRating(speciesId);

		_detailPortrait.Color = discovered ? GetSpeciesColor(speciesId) : new Color(0.5f, 0.5f, 0.5f);
		_detailName.Text = discovered ? species.DisplayName : "???";
		_detailStars.Text = discovered ? new string('★', stars) + new string('☆', 3 - stars) : "";
		_detailDescription.Text = discovered ? (species.JournalText ?? "") : "";
		_detailHint.Text = species.HintText ?? "";

		_gridView.Visible = false;
		_detailView.Visible = true;
	}

	private void ShowGrid()
	{
		_gridView.Visible = true;
		_detailView.Visible = false;
	}

	private static Color GetSpeciesColor(string speciesId)
	{
		return speciesId switch
		{
			"honeybee" => new Color(1.0f, 0.85f, 0.2f),
			"cabbage_white" => new Color(0.85f, 0.4f, 0.8f),
			"ladybug" => new Color(0.9f, 0.2f, 0.15f),
			"sphinx_moth" => new Color(0.6f, 0.55f, 0.45f),
			_ => Colors.Gray,
		};
	}
}
