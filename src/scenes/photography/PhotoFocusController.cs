using System;
using Godot;
using ProjectFlutter;

public partial class PhotoFocusController : Control
{
	public const float FocusDuration = 1.5f;
	public const float InitialRadius = 80f;
	public const float FinalRadius = 20f;
	public const float ThreeStarPct = 0.15f;
	public const float TwoStarPct = 0.40f;

	private bool _isFocusing;
	private float _focusElapsed;
	private Insect _targetInsect;

	private Action<InsectDepartingEvent> _onInsectDeparting;
	private Action<GameStateChangedEvent> _onStateChanged;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_onInsectDeparting = OnInsectDeparting;
		_onStateChanged = OnStateChanged;
		EventBus.Subscribe(_onInsectDeparting);
		EventBus.Subscribe(_onStateChanged);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onInsectDeparting);
		EventBus.Unsubscribe(_onStateChanged);
	}

	public override void _Process(double delta)
	{
		if (!_isFocusing) return;

		// Validate target still exists and is photographable
		if (!GodotObject.IsInstanceValid(_targetInsect) || !_targetInsect.IsPhotographable)
		{
			CancelFocus();
			return;
		}

		_focusElapsed += (float)delta; // real-time, not game-time
		QueueRedraw();

		// Check if focus is complete
		if (_focusElapsed >= FocusDuration)
		{
			TakePhoto();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.CurrentState != GameManager.GameState.PhotoMode) return;

		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed)
				TryStartFocus();
			else
				CancelFocus();

			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Draw()
	{
		if (!_isFocusing || !GodotObject.IsInstanceValid(_targetInsect)) return;

		Vector2 cursor = GetViewport().GetMousePosition();
		float progress = Mathf.Clamp(_focusElapsed / FocusDuration, 0f, 1f);
		float radius = Mathf.Lerp(InitialRadius, FinalRadius, progress);

		// Outer ring fades out
		var outerColor = new Color(1f, 1f, 1f, 0.3f * (1f - progress));
		DrawArc(cursor, radius + 10f, 0f, Mathf.Tau, 64, outerColor, 2f, true);

		// Main ring
		var mainColor = new Color(0.8f, 0.95f, 0.8f, 0.6f + 0.4f * progress);
		DrawArc(cursor, radius, 0f, Mathf.Tau, 64, mainColor, 3f, true);

		// Inner ring appears after 30%
		if (progress > 0.3f)
		{
			float innerAlpha = (progress - 0.3f) / 0.7f;
			DrawArc(cursor, radius * 0.6f, 0f, Mathf.Tau, 64,
				new Color(0.5f, 1f, 0.5f, innerAlpha * 0.8f), 2f, true);
		}

		// Crosshairs
		float crosshairLength = radius * 0.3f;
		var crossColor = new Color(1f, 1f, 1f, 0.5f);
		DrawLine(cursor + new Vector2(-crosshairLength, 0), cursor + new Vector2(crosshairLength, 0), crossColor, 1f);
		DrawLine(cursor + new Vector2(0, -crosshairLength), cursor + new Vector2(0, crosshairLength), crossColor, 1f);

		// Corner brackets
		float bracketSize = radius * 0.4f;
		float bracketOffset = radius * 0.7f;
		var bracketColor = new Color(1f, 1f, 1f, 0.7f);
		DrawCornerBracket(cursor + new Vector2(-bracketOffset, -bracketOffset), bracketSize, bracketColor, false, false);
		DrawCornerBracket(cursor + new Vector2(bracketOffset, -bracketOffset), bracketSize, bracketColor, true, false);
		DrawCornerBracket(cursor + new Vector2(-bracketOffset, bracketOffset), bracketSize, bracketColor, false, true);
		DrawCornerBracket(cursor + new Vector2(bracketOffset, bracketOffset), bracketSize, bracketColor, true, true);
	}

	private void DrawCornerBracket(Vector2 corner, float size, Color color, bool flipX, bool flipY)
	{
		float directionX = flipX ? -size : size;
		float directionY = flipY ? -size : size;
		DrawLine(corner, corner + new Vector2(directionX, 0), color, 2f);
		DrawLine(corner, corner + new Vector2(0, directionY), color, 2f);
	}

	private void TryStartFocus()
	{
		// Find nearest photographable insect to cursor in world space
		var camera = GetViewport().GetCamera2D();
		var worldPosition = camera.GetGlobalMousePosition();
		Insect best = null;
		float bestDistance = InitialRadius / camera.Zoom.X; // screen radius → world distance

		var insectContainer = GetTree().GetFirstNodeInGroup("insect_container");
		if (insectContainer == null)
		{
			// Fallback: find InsectContainer by path
			var garden = GetTree().Root.GetNodeOrNull("Main/Garden");
			insectContainer = garden?.GetNodeOrNull("InsectContainer");
		}
		if (insectContainer == null) return;

		foreach (var child in insectContainer.GetChildren())
		{
			if (child is Insect insect && insect.IsPhotographable)
			{
				float distance = worldPosition.DistanceTo(insect.GlobalPosition);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = insect;
				}
			}
		}

		if (best == null) return;

		_targetInsect = best;
		_isFocusing = true;
		_focusElapsed = 0f;
	}

	private void TakePhoto()
	{
		_isFocusing = false;

		if (!GodotObject.IsInstanceValid(_targetInsect) || !_targetInsect.IsPhotographable)
		{
			EventBus.Publish(new PhotoMissedEvent(GetViewport().GetCamera2D().GetGlobalMousePosition()));
			QueueRedraw();
			return;
		}

		// Calculate quality based on cursor-to-insect distance
		var camera = GetViewport().GetCamera2D();
		var cursorWorld = camera.GetGlobalMousePosition();
		float worldRadius = InitialRadius / camera.Zoom.X;
		float distance = cursorWorld.DistanceTo(_targetInsect.GlobalPosition);

		// Miss if insect is outside the photo frame
		if (distance > worldRadius)
		{
			EventBus.Publish(new PhotoMissedEvent(cursorWorld));
			_targetInsect = null;
			QueueRedraw();
			return;
		}

		float normalized = distance / worldRadius;
		int stars = normalized <= ThreeStarPct ? 3 : normalized <= TwoStarPct ? 2 : 1;

		// Freeze the insect
		_targetInsect.Freeze(0.5f);

		// Publish photo taken
		var data = _targetInsect.Data;
		EventBus.Publish(new PhotoTakenEvent(data.Id, data.DisplayName, stars, _targetInsect.GlobalPosition));

		// Record in journal
		JournalManager.Instance.DiscoverSpecies(data.Id, stars);

		_targetInsect = null;
		QueueRedraw();
	}

	private void CancelFocus()
	{
		_isFocusing = false;
		_targetInsect = null;
		QueueRedraw();
	}

	private void OnInsectDeparting(InsectDepartingEvent departingEvent)
	{
		if (GodotObject.IsInstanceValid(_targetInsect) && departingEvent.Insect == _targetInsect)
			CancelFocus();
	}

	private void OnStateChanged(GameStateChangedEvent stateEvent)
	{
		if (stateEvent.NewState != GameManager.GameState.PhotoMode)
			CancelFocus();
	}
}
