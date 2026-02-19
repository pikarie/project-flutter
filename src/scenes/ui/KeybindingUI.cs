using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class KeybindingUI : VBoxContainer
{
	private string _listeningAction;
	private int _listeningSlot;
	private Button _listeningButton;
	private Label _feedbackLabel;
	private CanvasLayer _blockerLayer;
	private ColorRect _inputBlocker;
	private readonly Dictionary<string, (Button Primary, Button Alternate)> _bindingButtons = new();

	public override void _Ready()
	{
		BuildUI();
	}

	private void BuildUI()
	{
		AddThemeConstantOverride("separation", 8);

		// Header
		var header = new HBoxContainer();
		AddChild(header);

		var title = new Label
		{
			Text = "Key Bindings",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.15f));
		header.AddChild(title);

		// Two-column layout
		var columnsContainer = new HBoxContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		columnsContainer.AddThemeConstantOverride("separation", 24);
		AddChild(columnsContainer);

		// Left column: Mouse + Gameplay
		var leftColumn = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		leftColumn.AddThemeConstantOverride("separation", 3);
		columnsContainer.AddChild(leftColumn);

		// Right column: Navigation + Garden
		var rightColumn = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		rightColumn.AddThemeConstantOverride("separation", 3);
		columnsContainer.AddChild(rightColumn);

		// Distribute groups: first 2 groups left, last 2 right
		for (int groupIndex = 0; groupIndex < InputManager.ActionGroups.Length; groupIndex++)
		{
			var (groupHeader, actions) = InputManager.ActionGroups[groupIndex];
			var targetColumn = groupIndex < 2 ? leftColumn : rightColumn;

			AddGroupHeader(targetColumn, groupHeader);
			foreach (var actionName in actions)
			{
				AddBindingRow(targetColumn, actionName);
			}
		}

		// Footer
		var footer = new HBoxContainer();
		footer.AddThemeConstantOverride("separation", 12);
		AddChild(footer);

		var resetButton = new Button { Text = "Reset to Defaults" };
		resetButton.Pressed += OnResetDefaults;
		footer.AddChild(resetButton);

		_feedbackLabel = new Label { Text = "" };
		_feedbackLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
		_feedbackLabel.AddThemeFontSizeOverride("font_size", 13);
		_feedbackLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		footer.AddChild(_feedbackLabel);

		// CanvasLayer + full-rect ColorRect = guaranteed full-screen overlay
		_blockerLayer = new CanvasLayer { Layer = 100 };

		_inputBlocker = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.15f),
			MouseFilter = MouseFilterEnum.Stop,
		};
		_inputBlocker.SetAnchorsPreset(LayoutPreset.FullRect);
		_blockerLayer.AddChild(_inputBlocker);

		var blockerLabel = new Label
		{
			Text = "Press a key or mouse button...\nESC to cancel",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		blockerLabel.SetAnchorsPreset(LayoutPreset.FullRect);
		blockerLabel.AddThemeFontSizeOverride("font_size", 20);
		blockerLabel.AddThemeColorOverride("font_color", Colors.White);
		_inputBlocker.AddChild(blockerLabel);
	}

	private void AddGroupHeader(VBoxContainer parent, string headerText)
	{
		var separator = new HSeparator();
		parent.AddChild(separator);

		var label = new Label { Text = headerText };
		label.AddThemeFontSizeOverride("font_size", 15);
		label.AddThemeColorOverride("font_color", new Color(0.5f, 0.4f, 0.3f));
		parent.AddChild(label);
	}

	private void AddBindingRow(VBoxContainer parent, string actionName)
	{
		string displayName = InputManager.ActionDisplayNames.GetValueOrDefault(actionName, actionName);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 6);
		parent.AddChild(row);

		// Action name label
		var nameLabel = new Label
		{
			Text = displayName,
			CustomMinimumSize = new Vector2(120, 0),
		};
		nameLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.25f, 0.2f));
		nameLabel.AddThemeFontSizeOverride("font_size", 13);
		row.AddChild(nameLabel);

		// Primary binding button
		var primaryButton = new Button
		{
			CustomMinimumSize = new Vector2(80, 26),
			FocusMode = FocusModeEnum.None,
		};
		primaryButton.AddThemeFontSizeOverride("font_size", 12);
		string action = actionName;
		primaryButton.Pressed += () => StartListening(action, 0, primaryButton);
		row.AddChild(primaryButton);

		// Alternate binding button
		var alternateButton = new Button
		{
			CustomMinimumSize = new Vector2(80, 26),
			FocusMode = FocusModeEnum.None,
		};
		alternateButton.AddThemeFontSizeOverride("font_size", 12);
		alternateButton.Pressed += () => StartListening(action, 1, alternateButton);
		row.AddChild(alternateButton);

		// Clear alternate button
		var clearButton = new Button
		{
			Text = "x",
			CustomMinimumSize = new Vector2(24, 24),
			FocusMode = FocusModeEnum.None,
		};
		clearButton.AddThemeFontSizeOverride("font_size", 11);
		clearButton.Pressed += () => ClearAlternate(action);
		row.AddChild(clearButton);

		_bindingButtons[actionName] = (primaryButton, alternateButton);
	}

	public void Refresh()
	{
		foreach (var (actionName, (primaryButton, alternateButton)) in _bindingButtons)
		{
			var bindings = InputManager.Instance.GetCurrentBindings(actionName);
			primaryButton.Text = bindings.Count > 0
				? InputManager.GetInputDisplayName(bindings[0])
				: "—";
			alternateButton.Text = bindings.Count > 1
				? InputManager.GetInputDisplayName(bindings[1])
				: "—";
		}
		_feedbackLabel.Text = "";
	}

	private void StartListening(string actionName, int slotIndex, Button button)
	{
		if (_listeningAction != null)
			CancelListening();

		_listeningAction = actionName;
		_listeningSlot = slotIndex;
		_listeningButton = button;
		button.Text = "...";
		_feedbackLabel.Text = "Press a key or click...";

		// Show the full-screen input blocker to intercept all mouse events
		ShowInputBlocker();
	}

	private void CancelListening()
	{
		if (_listeningAction == null) return;
		_listeningAction = null;
		_listeningButton = null;
		_feedbackLabel.Text = "";
		HideInputBlocker();
		Refresh();
	}

	private void ShowInputBlocker()
	{
		// Add the CanvasLayer to the scene tree (layer 100 = above everything)
		if (_blockerLayer.GetParent() == null)
			GetTree().Root.AddChild(_blockerLayer);
	}

	private void HideInputBlocker()
	{
		if (_blockerLayer.GetParent() != null)
			_blockerLayer.GetParent().RemoveChild(_blockerLayer);
	}

	public override void _Input(InputEvent @event)
	{
		// Only intercept when listening for input AND blocker is visible
		if (_listeningAction == null) return;
		if (_blockerLayer.GetParent() == null) return;

		// Cancel with ESC
		if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
		{
			CancelListening();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Capture key press
		if (@event is InputEventKey { Pressed: true } keyEvent)
		{
			ApplyBinding(keyEvent);
			GetViewport().SetInputAsHandled();
			return;
		}

		// Capture mouse button press (not release, not motion)
		if (@event is InputEventMouseButton { Pressed: true } mouseEvent)
		{
			ApplyBinding(mouseEvent);
			GetViewport().SetInputAsHandled();
		}
	}

	private void ApplyBinding(InputEvent newEvent)
	{
		string conflictAction = InputManager.Instance.RebindAction(
			_listeningAction, newEvent, _listeningSlot);

		if (conflictAction != null)
		{
			string conflictName = InputManager.ActionDisplayNames
				.GetValueOrDefault(conflictAction, conflictAction);
			_feedbackLabel.Text = $"Unbound from {conflictName}";
		}
		else
		{
			_feedbackLabel.Text = "Binding saved";
		}

		_listeningAction = null;
		_listeningButton = null;
		HideInputBlocker();
		Refresh();
	}

	private void ClearAlternate(string actionName)
	{
		InputManager.Instance.ClearBinding(actionName, 1);
		Refresh();
		_feedbackLabel.Text = "Alternate binding cleared";
	}

	private void OnResetDefaults()
	{
		InputManager.Instance.ResetToDefaults();
		Refresh();
		_feedbackLabel.Text = "All bindings reset to defaults";
	}

	public override void _ExitTree()
	{
		// Clean up CanvasLayer from root if still attached
		if (_blockerLayer?.GetParent() != null)
		{
			_blockerLayer.GetParent().RemoveChild(_blockerLayer);
			_blockerLayer.QueueFree();
		}
	}
}
