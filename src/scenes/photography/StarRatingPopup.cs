using System;
using Godot;
using ProjectFlutter;

public partial class StarRatingPopup : Control
{
	private bool _showing;
	private float _timer;
	private int _stars;
	private Vector2 _screenPos;
	private const float DisplayDuration = 1.5f;

	private Action<PhotoTakenEvent> _onPhotoTaken;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_onPhotoTaken = OnPhotoTaken;
		EventBus.Subscribe(_onPhotoTaken);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onPhotoTaken);
	}

	public override void _Process(double delta)
	{
		if (!_showing) return;

		_timer += (float)delta;
		if (_timer >= DisplayDuration)
		{
			_showing = false;
			QueueRedraw();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_showing) return;

		float progress = _timer / DisplayDuration;
		float alpha = progress < 0.7f ? 1f : 1f - (progress - 0.7f) / 0.3f;
		float yOffset = -40f * progress;

		Vector2 drawPosition = _screenPos + new Vector2(0, yOffset);

		string starText = _stars switch
		{
			3 => "★★★",
			2 => "★★☆",
			_ => "★☆☆"
		};

		Color starColor = _stars switch
		{
			3 => new Color(1f, 0.85f, 0.1f, alpha),
			2 => new Color(0.9f, 0.9f, 0.5f, alpha),
			_ => new Color(0.8f, 0.8f, 0.7f, alpha)
		};

		// Background pill
		var backgroundColor = new Color(0, 0, 0, 0.5f * alpha);
		var backgroundRect = new Rect2(drawPosition.X - 45, drawPosition.Y - 18, 90, 36);
		DrawRect(backgroundRect, backgroundColor);

		// Star text
		var font = ThemeDB.FallbackFont;
		DrawString(font, drawPosition + new Vector2(-35, 8), starText,
			HorizontalAlignment.Left, 70, 22, starColor);
	}

	private void OnPhotoTaken(PhotoTakenEvent photoEvent)
	{
		_stars = photoEvent.StarRating;

		// Convert world position to screen position
		var camera = GetViewport().GetCamera2D();
		if (camera != null)
		{
			var viewportSize = GetViewportRect().Size;
			_screenPos = (photoEvent.WorldPosition - camera.GlobalPosition) * camera.Zoom
				+ viewportSize / 2f;
		}
		else
		{
			_screenPos = photoEvent.WorldPosition;
		}

		_timer = 0f;
		_showing = true;
	}
}
