using Godot;

/// <summary>
/// Elliptical crawl around an anchor (ladybug-like).
/// Y-axis compressed for top-down perspective.
/// </summary>
public class CrawlBehavior : IMovementBehavior
{
	private readonly float _angularSpeed;
	private readonly float _radius;
	private Vector2 _anchor;
	private float _angle;

	public CrawlBehavior(InsectData data, RandomNumberGenerator rng)
	{
		_angularSpeed = (data.MovementSpeed / 30f) * (1f + rng.RandfRange(-0.15f, 0.15f));
		_radius = 14f * (1f + rng.RandfRange(-0.2f, 0.2f));
		_angle = rng.RandfRange(0f, Mathf.Tau);
	}

	public void Reset(Vector2 anchor) => _anchor = anchor;

	public Vector2 CalculatePosition(float delta)
	{
		_angle += _angularSpeed * delta;
		float r = _radius + Mathf.Sin(_angle * 2f) * 2f;
		return _anchor + new Vector2(
			Mathf.Cos(_angle) * r,
			Mathf.Sin(_angle) * r * 0.6f
		);
	}
}
