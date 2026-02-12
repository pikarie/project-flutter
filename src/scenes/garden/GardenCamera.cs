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

	public override void _Process(double delta)
	{
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
		if (@event is InputEventMouseButton mouseBtn)
		{
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
	}
}
