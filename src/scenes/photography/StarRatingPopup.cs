using System;
using Godot;
using ProjectFlutter;

public partial class StarRatingPopup : Control
{
	private bool _showing;
	private float _timer;
	private int _stars;
	private bool _isNewDiscovery;
	private Vector2 _screenPosition;
	private const float DisplayDuration = 1.5f;

	// Golden particle burst for high-quality photos
	private const int ParticleCount = 12;
	private Vector2[] _particleDirections;
	private RandomNumberGenerator _rng;

	private Action<PhotoTakenEvent> _onPhotoTaken;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_rng = new RandomNumberGenerator();
		_rng.Randomize();
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

		// Proportional scale: ★☆☆ = 1.0, ★★☆ = 1.1, ★★★ = 1.25, new = 1.4
		float scale = _isNewDiscovery ? 1.4f : _stars switch { 3 => 1.25f, 2 => 1.1f, _ => 1.0f };

		// Bounce effect for ★★★ and new discovery
		if ((_stars == 3 || _isNewDiscovery) && progress < 0.15f)
		{
			float bounce = Mathf.Sin(progress / 0.15f * Mathf.Pi) * 0.2f;
			scale += bounce;
		}

		float yOffset = -40f * progress;
		Vector2 drawPosition = _screenPosition + new Vector2(0, yOffset);

		// Golden particle burst for ★★★ and new discovery
		if ((_stars == 3 || _isNewDiscovery) && _particleDirections != null)
		{
			DrawParticleBurst(drawPosition, progress, alpha);
		}

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

		// Background pill — proportional size
		float pillWidth = 90f * scale;
		float pillHeight = 36f * scale;
		var backgroundColor = new Color(0, 0, 0, 0.5f * alpha);
		var backgroundRect = new Rect2(drawPosition.X - pillWidth / 2f, drawPosition.Y - pillHeight / 2f, pillWidth, pillHeight);
		DrawRect(backgroundRect, backgroundColor);

		// Golden glow ring for ★★★
		if (_stars == 3 || _isNewDiscovery)
		{
			float glowRadius = pillWidth * 0.7f;
			float glowAlpha = alpha * 0.3f * (1f - progress);
			Color glowColor = _isNewDiscovery
				? new Color(0.5f, 1f, 0.5f, glowAlpha)
				: new Color(1f, 0.85f, 0.2f, glowAlpha);
			DrawArc(drawPosition, glowRadius, 0f, Mathf.Tau, 32, glowColor, 2f);
		}

		// Star text — proportional font size
		int fontSize = (int)(22 * scale);
		var font = ThemeDB.FallbackFont;
		float textWidth = pillWidth - 10f;
		DrawString(font, drawPosition + new Vector2(-textWidth / 2f, fontSize * 0.35f), starText,
			HorizontalAlignment.Left, (int)textWidth, fontSize, starColor);
	}

	private void DrawParticleBurst(Vector2 center, float progress, float alpha)
	{
		// Particles expand outward and fade
		float particleProgress = Mathf.Clamp(progress * 2f, 0f, 1f); // particles finish in first half
		float particleAlpha = alpha * (1f - particleProgress);
		float particleDistance = 100f * particleProgress;
		float particleRadius = 5f * (1f - particleProgress * 0.5f);

		Color particleColor = _isNewDiscovery
			? new Color(0.5f, 1f, 0.5f, particleAlpha)
			: new Color(1f, 0.85f, 0.2f, particleAlpha);

		for (int i = 0; i < _particleDirections.Length; i++)
		{
			Vector2 particlePosition = center + _particleDirections[i] * particleDistance;
			DrawCircle(particlePosition, particleRadius, particleColor);
		}
	}

	private void GenerateParticleDirections()
	{
		_particleDirections = new Vector2[ParticleCount];
		for (int i = 0; i < ParticleCount; i++)
		{
			float angle = (Mathf.Tau / ParticleCount * i) + _rng.RandfRange(-0.3f, 0.3f);
			float distance = _rng.RandfRange(0.7f, 1.3f);
			_particleDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
		}
	}

	private void OnPhotoTaken(PhotoTakenEvent photoEvent)
	{
		_stars = photoEvent.StarRating;
		_isNewDiscovery = photoEvent.IsNewDiscovery;

		// Generate particle directions for high-quality shots
		if (_stars == 3 || _isNewDiscovery)
			GenerateParticleDirections();

		// Convert world position to screen position
		var camera = GetViewport().GetCamera2D();
		if (camera != null)
		{
			var viewportSize = GetViewportRect().Size;
			_screenPosition = (photoEvent.WorldPosition - camera.GlobalPosition) * camera.Zoom
				+ viewportSize / 2f;
		}
		else
		{
			_screenPosition = photoEvent.WorldPosition;
		}

		_timer = 0f;
		_showing = true;
	}
}
