using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class InputManager : Node
{
	public static InputManager Instance { get; private set; }

	private const string SettingsPath = "user://settings.cfg";
	private const string SectionName = "keybindings";

	// Each default binding: list of InputEvents (primary + optional alternate)
	private static readonly Dictionary<string, InputEvent[]> DefaultBindings = new()
	{
		// Mouse actions
		{ "primary_action", new InputEvent[] { MouseButtonEvent(MouseButton.Left) } },
		{ "cancel_action", new InputEvent[] { MouseButtonEvent(MouseButton.Right) } },
		{ "camera_drag", new InputEvent[] { MouseButtonEvent(MouseButton.Middle) } },
		{ "zoom_in", new InputEvent[] { MouseButtonEvent(MouseButton.WheelUp) } },
		{ "zoom_out", new InputEvent[] { MouseButtonEvent(MouseButton.WheelDown) } },

		// Gameplay
		{ "toggle_photo_mode", new InputEvent[] { KeyEvent(Key.C) } },
		{ "toggle_journal", new InputEvent[] { KeyEvent(Key.J), KeyEvent(Key.Tab) } },
		{ "toggle_shop", new InputEvent[] { KeyEvent(Key.S) } },
		{ "toggle_lantern", new InputEvent[] { KeyEvent(Key.L) } },
		{ "cycle_speed", new InputEvent[] { KeyEvent(Key.Space) } },

		// Navigation
		{ "zone_next", new InputEvent[] { KeyEvent(Key.E) } },
		{ "zone_previous", new InputEvent[] { KeyEvent(Key.Q) } },
		{ "camera_left", new InputEvent[] { KeyEvent(Key.A), KeyEvent(Key.Left) } },
		{ "camera_right", new InputEvent[] { KeyEvent(Key.D), KeyEvent(Key.Right) } },
		{ "camera_up", new InputEvent[] { KeyEvent(Key.W), KeyEvent(Key.Up) } },
		{ "camera_down", new InputEvent[] { KeyEvent(Key.S), KeyEvent(Key.Down) } },

		// Garden
		{ "remove_plant", new InputEvent[] { KeyEvent(Key.X), KeyEvent(Key.Delete) } },
		{ "hotbar_1", new InputEvent[] { KeyEvent(Key.Key1) } },
		{ "hotbar_2", new InputEvent[] { KeyEvent(Key.Key2) } },
		{ "hotbar_3", new InputEvent[] { KeyEvent(Key.Key3) } },
		{ "hotbar_4", new InputEvent[] { KeyEvent(Key.Key4) } },
		{ "hotbar_5", new InputEvent[] { KeyEvent(Key.Key5) } },
		{ "hotbar_6", new InputEvent[] { KeyEvent(Key.Key6) } },
		{ "hotbar_7", new InputEvent[] { KeyEvent(Key.Key7) } },
		{ "hotbar_8", new InputEvent[] { KeyEvent(Key.Key8) } },
		{ "hotbar_9", new InputEvent[] { KeyEvent(Key.Key9) } },
	};

	// Display names for the rebinding UI
	public static readonly Dictionary<string, string> ActionDisplayNames = new()
	{
		{ "primary_action", "Primary Action" },
		{ "cancel_action", "Cancel / Deselect" },
		{ "camera_drag", "Camera Drag" },
		{ "zoom_in", "Zoom In" },
		{ "zoom_out", "Zoom Out" },
		{ "toggle_photo_mode", "Photo Mode" },
		{ "toggle_journal", "Journal" },
		{ "toggle_shop", "Shop" },
		{ "toggle_lantern", "Lantern" },
		{ "cycle_speed", "Time Speed" },
		{ "zone_next", "Next Zone" },
		{ "zone_previous", "Previous Zone" },
		{ "camera_left", "Camera Left" },
		{ "camera_right", "Camera Right" },
		{ "camera_up", "Camera Up" },
		{ "camera_down", "Camera Down" },
		{ "remove_plant", "Remove Plant" },
		{ "hotbar_1", "Hotbar 1" },
		{ "hotbar_2", "Hotbar 2" },
		{ "hotbar_3", "Hotbar 3" },
		{ "hotbar_4", "Hotbar 4" },
		{ "hotbar_5", "Hotbar 5" },
		{ "hotbar_6", "Hotbar 6" },
		{ "hotbar_7", "Hotbar 7" },
		{ "hotbar_8", "Hotbar 8" },
		{ "hotbar_9", "Hotbar 9" },
	};

	// Action groups for UI sections
	public static readonly (string Header, string[] Actions)[] ActionGroups =
	{
		("Mouse", new[] { "primary_action", "cancel_action", "camera_drag", "zoom_in", "zoom_out" }),
		("Gameplay", new[] { "toggle_photo_mode", "toggle_journal", "toggle_shop", "toggle_lantern", "cycle_speed" }),
		("Navigation", new[] { "zone_next", "zone_previous", "camera_left", "camera_right", "camera_up", "camera_down" }),
		("Garden", new[] { "remove_plant", "hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5", "hotbar_6", "hotbar_7", "hotbar_8", "hotbar_9" }),
	};

	public override void _Ready()
	{
		Instance = this;
		RegisterDefaultActions();
		LoadBindings();
	}

	private void RegisterDefaultActions()
	{
		foreach (var (actionName, events) in DefaultBindings)
		{
			if (InputMap.HasAction(actionName))
				InputMap.EraseAction(actionName);

			InputMap.AddAction(actionName);
			foreach (var inputEvent in events)
				InputMap.ActionAddEvent(actionName, inputEvent);
		}
	}

	private static InputEventKey KeyEvent(Key key)
	{
		var eventKey = new InputEventKey();
		eventKey.Keycode = key;
		return eventKey;
	}

	private static InputEventMouseButton MouseButtonEvent(MouseButton button)
	{
		var eventMouse = new InputEventMouseButton();
		eventMouse.ButtonIndex = button;
		return eventMouse;
	}

	public void LoadBindings()
	{
		var config = new ConfigFile();
		if (config.Load(SettingsPath) != Error.Ok) return;

		foreach (var actionName in DefaultBindings.Keys)
		{
			if (!config.HasSectionKey(SectionName, actionName)) continue;

			string savedValue = config.GetValue(SectionName, actionName).AsString();
			var events = ParseSavedBinding(savedValue);
			if (events.Count == 0) continue;

			InputMap.ActionEraseEvents(actionName);
			foreach (var inputEvent in events)
				InputMap.ActionAddEvent(actionName, inputEvent);
		}
	}

	public void SaveBindings()
	{
		var config = new ConfigFile();
		config.Load(SettingsPath); // Preserve non-keybinding settings

		foreach (var actionName in DefaultBindings.Keys)
		{
			var events = InputMap.ActionGetEvents(actionName);
			var parts = new List<string>();
			foreach (var inputEvent in events)
			{
				if (inputEvent is InputEventKey keyEvent)
					parts.Add($"key:{(int)keyEvent.Keycode}");
				else if (inputEvent is InputEventMouseButton mouseEvent)
					parts.Add($"mouse:{(int)mouseEvent.ButtonIndex}");
			}
			config.SetValue(SectionName, actionName, string.Join(",", parts));
		}

		config.Save(SettingsPath);
	}

	private static List<InputEvent> ParseSavedBinding(string savedValue)
	{
		var events = new List<InputEvent>();
		foreach (var part in savedValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
		{
			var trimmed = part.Trim();
			if (trimmed.StartsWith("key:") && int.TryParse(trimmed[4..], out int keyCode))
			{
				events.Add(KeyEvent((Key)keyCode));
			}
			else if (trimmed.StartsWith("mouse:") && int.TryParse(trimmed[6..], out int mouseCode))
			{
				events.Add(MouseButtonEvent((Godot.MouseButton)mouseCode));
			}
		}
		return events;
	}

	/// <summary>
	/// Rebind a specific action slot. Returns the name of the action that was
	/// previously using this input (conflict), or null if no conflict.
	/// </summary>
	public string RebindAction(string actionName, InputEvent newEvent, int slotIndex)
	{
		string conflictAction = FindActionWithEvent(newEvent, actionName);

		if (conflictAction != null)
			RemoveEventFromAction(conflictAction, newEvent);

		// Get current events for this action
		var currentEvents = new List<InputEvent>();
		foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
			currentEvents.Add(inputEvent);

		// Ensure slot exists
		while (currentEvents.Count <= slotIndex)
			currentEvents.Add(null);

		currentEvents[slotIndex] = newEvent;

		// Reapply
		InputMap.ActionEraseEvents(actionName);
		foreach (var inputEvent in currentEvents)
		{
			if (inputEvent != null)
				InputMap.ActionAddEvent(actionName, inputEvent);
		}

		SaveBindings();
		return conflictAction;
	}

	/// <summary>
	/// Remove a specific binding slot from an action.
	/// </summary>
	public void ClearBinding(string actionName, int slotIndex)
	{
		var currentEvents = new List<InputEvent>();
		foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
			currentEvents.Add(inputEvent);

		if (slotIndex < currentEvents.Count)
		{
			currentEvents.RemoveAt(slotIndex);
			InputMap.ActionEraseEvents(actionName);
			foreach (var inputEvent in currentEvents)
				InputMap.ActionAddEvent(actionName, inputEvent);
			SaveBindings();
		}
	}

	public void ResetToDefaults()
	{
		RegisterDefaultActions();
		SaveBindings();
		EventBus.Publish(new KeybindingsResetEvent());
	}

	/// <summary>
	/// Get the current input events for an action (for UI display).
	/// </summary>
	public List<InputEvent> GetCurrentBindings(string actionName)
	{
		var result = new List<InputEvent>();
		foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
			result.Add(inputEvent);
		return result;
	}

	/// <summary>
	/// Get a human-readable name for an input event.
	/// </summary>
	public static string GetInputDisplayName(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey keyEvent)
		{
			return keyEvent.Keycode switch
			{
				Key.Space => "Space",
				Key.Tab => "Tab",
				Key.Delete => "Delete",
				Key.Left => "Left",
				Key.Right => "Right",
				Key.Up => "Up",
				Key.Down => "Down",
				Key.Key1 => "1",
				Key.Key2 => "2",
				Key.Key3 => "3",
				Key.Key4 => "4",
				Key.Key5 => "5",
				Key.Key6 => "6",
				Key.Key7 => "7",
				Key.Key8 => "8",
				Key.Key9 => "9",
				_ => keyEvent.Keycode.ToString(),
			};
		}

		if (inputEvent is InputEventMouseButton mouseEvent)
		{
			return mouseEvent.ButtonIndex switch
			{
				Godot.MouseButton.Left => "LMB",
				Godot.MouseButton.Right => "RMB",
				Godot.MouseButton.Middle => "MMB",
				Godot.MouseButton.WheelUp => "Wheel Up",
				Godot.MouseButton.WheelDown => "Wheel Down",
				_ => mouseEvent.ButtonIndex.ToString(),
			};
		}

		return "?";
	}

	private string FindActionWithEvent(InputEvent targetEvent, string excludeAction)
	{
		foreach (var actionName in DefaultBindings.Keys)
		{
			if (actionName == excludeAction) continue;
			foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
			{
				if (EventsMatch(inputEvent, targetEvent))
					return actionName;
			}
		}
		return null;
	}

	private void RemoveEventFromAction(string actionName, InputEvent targetEvent)
	{
		foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
		{
			if (EventsMatch(inputEvent, targetEvent))
			{
				InputMap.ActionEraseEvent(actionName, inputEvent);
				break;
			}
		}
	}

	private static bool EventsMatch(InputEvent eventA, InputEvent eventB)
	{
		if (eventA is InputEventKey keyA && eventB is InputEventKey keyB)
			return keyA.Keycode == keyB.Keycode;

		if (eventA is InputEventMouseButton mouseA && eventB is InputEventMouseButton mouseB)
			return mouseA.ButtonIndex == mouseB.ButtonIndex;

		return false;
	}
}
