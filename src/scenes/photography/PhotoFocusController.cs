using System;
using Godot;
using ProjectFlutter;

public partial class PhotoFocusController : Control
{
	public const float FocusDuration = 1.5f;
	public const float WorldRadius = 80f;
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
		var camera = GetViewport().GetCamera2D();
		float zoom = camera?.Zoom.X ?? 1f;

		// All radii in screen space, derived from fixed world radius
		float frameScreenRadius = WorldRadius * zoom;
		float threeStarScreenRadius = WorldRadius * ThreeStarPct * zoom;
		float twoStarScreenRadius = WorldRadius * TwoStarPct * zoom;

		// Current distance cursor↔insect in world space for live feedback
		var cursorWorld = camera.GetGlobalMousePosition();
		float currentDistance = cursorWorld.DistanceTo(_targetInsect.GlobalPosition);
		float normalizedDistance = currentDistance / WorldRadius;

		// Live quality color: green (3★) → orange (2★) → red (1★) → grey (miss)
		Color qualityColor;
		if (normalizedDistance <= ThreeStarPct)
			qualityColor = new Color(0.2f, 1f, 0.3f); // green
		else if (normalizedDistance <= TwoStarPct)
			qualityColor = new Color(1f, 0.7f, 0.1f); // orange
		else if (normalizedDistance <= 1f)
			qualityColor = new Color(1f, 0.25f, 0.2f); // red
		else
			qualityColor = new Color(0.5f, 0.5f, 0.5f); // grey — miss

		// 3-star zone — subtle green disc
		DrawCircle(cursor, threeStarScreenRadius, new Color(0.3f, 1f, 0.3f, 0.08f));
		DrawArc(cursor, threeStarScreenRadius, 0f, Mathf.Tau, 64,
			new Color(0.3f, 1f, 0.3f, 0.3f), 1f, true);

		// 2-star zone — subtle orange ring
		DrawArc(cursor, twoStarScreenRadius, 0f, Mathf.Tau, 64,
			new Color(1f, 0.7f, 0.1f, 0.25f), 1f, true);

		// Frame boundary — uses live quality color
		DrawArc(cursor, frameScreenRadius, 0f, Mathf.Tau, 64,
			new Color(qualityColor.R, qualityColor.G, qualityColor.B, 0.4f), 2f, true);

		// Focus progress arc — uses live quality color, bright
		float progressAngle = Mathf.Tau * progress;
		DrawArc(cursor, frameScreenRadius, -Mathf.Pi / 2f, -Mathf.Pi / 2f + progressAngle, 64,
			new Color(qualityColor.R, qualityColor.G, qualityColor.B, 0.8f), 3.5f, true);

		// Crosshairs
		float crosshairLength = frameScreenRadius * 0.3f;
		var crossColor = new Color(1f, 1f, 1f, 0.5f);
		DrawLine(cursor + new Vector2(-crosshairLength, 0), cursor + new Vector2(crosshairLength, 0), crossColor, 1f);
		DrawLine(cursor + new Vector2(0, -crosshairLength), cursor + new Vector2(0, crosshairLength), crossColor, 1f);

		// Corner brackets — uses live quality color
		float bracketSize = frameScreenRadius * 0.4f;
		float bracketOffset = frameScreenRadius * 0.7f;
		var bracketColor = new Color(qualityColor.R, qualityColor.G, qualityColor.B, 0.7f);
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
		float bestDistance = WorldRadius; // fixed world-space search radius

		// Find InsectContainer in the active (visible) zone
		Node insectContainer = null;
		var gameWorld = GetTree().Root.GetNodeOrNull("Main/GameWorld");
		if (gameWorld != null)
		{
			foreach (var child in gameWorld.GetChildren())
			{
				if (child is Node2D zoneNode && zoneNode.Visible)
				{
					insectContainer = zoneNode.GetNodeOrNull("InsectContainer");
					break;
				}
			}
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
		float distance = cursorWorld.DistanceTo(_targetInsect.GlobalPosition);

		// Miss if insect is outside the frame (fixed world radius)
		if (distance > WorldRadius)
		{
			EventBus.Publish(new PhotoMissedEvent(cursorWorld));
			_targetInsect = null;
			QueueRedraw();
			return;
		}

		// Star rating based on fixed world radius
		float normalized = distance / WorldRadius;
		int stars = normalized <= ThreeStarPct ? 3 : normalized <= TwoStarPct ? 2 : 1;

		// Night photography cap (GDD §4.4):
		// Without lantern: max 2★ (except firefly during pulse → handled later)
		// With lantern active: 3★ possible
		// Firefly + lantern active: max 2★ (washed out by light)
		bool isNight = TimeManager.Instance.IsNighttime();
		if (isNight && stars == 3)
		{
			bool isFirefly = _targetInsect.Data.Id == "firefly";
			bool lanternOn = GameManager.Instance.HasLantern && GameManager.Instance.LanternActive;

			if (isFirefly && lanternOn)
				stars = 2; // Firefly washed out by lantern
			else if (!lanternOn)
				stars = 2; // No lantern at night → max 2★
			// else: non-firefly + lantern = 3★ OK
		}

		// Freeze the insect
		_targetInsect.Freeze(0.5f);

		// Publish photo taken (check discovery before recording)
		var data = _targetInsect.Data;
		bool isNewDiscovery = !JournalManager.Instance.IsDiscovered(data.Id);
		EventBus.Publish(new PhotoTakenEvent(data.Id, data.DisplayName, stars, _targetInsect.GlobalPosition, isNewDiscovery));

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
