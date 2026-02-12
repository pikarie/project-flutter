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
			Position += dir.Normalized() * PanSpeed * (float)delta / Zoom.X;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsInteractiveState) return;

		if (@event is InputEventMouseButton mouseBtn)
		{
			if (mouseBtn.ButtonIndex == MouseButton.Middle)
			{
				_isDragging = mouseBtn.Pressed;
				_lastDragPosition = mouseBtn.GlobalPosition;
				GetViewport().SetInputAsHandled();
				return;
			}

			float zoomDelta = mouseBtn.ButtonIndex switch
			{
				MouseButton.WheelUp => ZoomSpeed,
				MouseButton.WheelDown => -ZoomSpeed,
				_ => 0
			};

			if (zoomDelta != 0)
			{
				float newZoom = Mathf.Clamp(Zoom.X + zoomDelta, MinZoom, MaxZoom);
				Zoom = new Vector2(newZoom, newZoom);
			}
		}

		if (@event is InputEventMouseMotion mouseMotion && _isDragging)
		{
			Vector2 delta = mouseMotion.GlobalPosition - _lastDragPosition;
			Position -= delta / Zoom;
			_lastDragPosition = mouseMotion.GlobalPosition;
			GetViewport().SetInputAsHandled();
		}
	}
}
