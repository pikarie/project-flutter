using System;
using Godot;
using ProjectFlutter;

public partial class GardenCamera : Camera2D
{
	[Export] public float PanSpeed { get; set; } = 400.0f;
	[Export] public float ZoomSpeed { get; set; } = 0.1f;
	[Export] public float MinZoom { get; set; } = 0.5f;
	[Export] public float MaxZoom { get; set; } = 3.0f;

	private Action<ZoneChangedEvent> _onZoneChanged;
	private bool _isDragging;
	private Vector2 _lastDragPosition;

	public override void _Ready()
	{
		PositionSmoothingEnabled = true;
		PositionSmoothingSpeed = 8.0f;

		_onZoneChanged = OnZoneChanged;
		EventBus.Subscribe(_onZoneChanged);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onZoneChanged);
	}

	private void OnZoneChanged(ZoneChangedEvent zoneEvent)
	{
		Position = Vector2.Zero;
	}

	private bool IsInteractiveState =>
		GameManager.Instance.CurrentState is GameManager.GameState.Playing or GameManager.GameState.PhotoMode;

	public override void _Process(double delta)
	{
		if (!IsInteractiveState) return;

		var dir = Vector2.Zero;
		if (Input.IsActionPressed("camera_left")) dir.X -= 1;
		if (Input.IsActionPressed("camera_right")) dir.X += 1;
		if (Input.IsActionPressed("camera_up")) dir.Y -= 1;
		if (Input.IsActionPressed("camera_down")) dir.Y += 1;

		if (dir != Vector2.Zero)
		{
			Position += dir.Normalized() * PanSpeed * (float)delta / Zoom.X;
			ClampToZoneBounds();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsInteractiveState) return;

		// Camera drag — rebindable (Middle-click by default)
		if (@event.IsActionPressed("camera_drag"))
		{
			_isDragging = true;
			if (@event is InputEventMouseButton mouseBtn)
				_lastDragPosition = mouseBtn.GlobalPosition;
			GetViewport().SetInputAsHandled();
			return;
		}
		if (@event.IsActionReleased("camera_drag"))
		{
			_isDragging = false;
			GetViewport().SetInputAsHandled();
			return;
		}

		// Zoom — rebindable (Scroll wheel by default)
		if (@event.IsActionPressed("zoom_in"))
		{
			float newZoom = Mathf.Clamp(Zoom.X + ZoomSpeed, MinZoom, MaxZoom);
			Zoom = new Vector2(newZoom, newZoom);
			ClampToZoneBounds();
			return;
		}
		if (@event.IsActionPressed("zoom_out"))
		{
			float newZoom = Mathf.Clamp(Zoom.X - ZoomSpeed, MinZoom, MaxZoom);
			Zoom = new Vector2(newZoom, newZoom);
			ClampToZoneBounds();
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion && _isDragging)
		{
			Vector2 delta = mouseMotion.GlobalPosition - _lastDragPosition;
			Position -= delta / Zoom;
			_lastDragPosition = mouseMotion.GlobalPosition;
			ClampToZoneBounds();
			GetViewport().SetInputAsHandled();
		}
	}

	private void ClampToZoneBounds()
	{
		var (_, width, height, _, _) = ZoneManager.ZoneConfig[ZoneManager.Instance.ActiveZone];
		const int tileSize = 128;
		float halfWidth = width * tileSize / 2f;
		float halfHeight = height * tileSize / 2f;
		const float padding = 64f;

		// Account for viewport size so camera doesn't show beyond grid + padding
		var viewportSize = GetViewportRect().Size;
		float halfViewportWidth = viewportSize.X / (2f * Zoom.X);
		float halfViewportHeight = viewportSize.Y / (2f * Zoom.Y);

		float limitX = Mathf.Max(halfWidth + padding - halfViewportWidth, 0f);
		float limitY = Mathf.Max(halfHeight + padding - halfViewportHeight, 0f);

		Position = new Vector2(
			Mathf.Clamp(Position.X, -limitX, limitX),
			Mathf.Clamp(Position.Y, -limitY, limitY)
		);
	}
}
