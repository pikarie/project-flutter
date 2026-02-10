using Godot;

/// <summary>
/// Sine-wave path between random points near an anchor (butterfly-like).
/// </summary>
public class FlutterBehavior : IMovementBehavior
{
	private readonly float _speed;
	private readonly float _amplitude;
	private readonly float _sineFrequency;
	private readonly RandomNumberGenerator _rng;
	private readonly float _wanderRadius;

	private Vector2 _anchor;
	private Vector2 _start;
	private Vector2 _end;
	private float _progress;

	public FlutterBehavior(InsectData data, RandomNumberGenerator rng)
	{
		_rng = rng;
		_speed = data.MovementSpeed * (1f + rng.RandfRange(-0.15f, 0.15f));
		_amplitude = 12f * (1f + rng.RandfRange(-0.2f, 0.2f));
		_sineFrequency = 2f + rng.RandfRange(-0.5f, 0.5f);
		_wanderRadius = 40f;
	}

	public void Reset(Vector2 anchor)
	{
		_anchor = anchor;
		_start = anchor;
		_end = PickRandomTarget();
		_progress = 0f;
	}

	public Vector2 CalculatePosition(float delta)
	{
		float dist = _start.DistanceTo(_end);
		if (dist < 1f) dist = 1f;

		_progress = Mathf.Clamp(_progress + (_speed * delta / dist), 0f, 1f);

		Vector2 basePos = _start.Lerp(_end, _progress);
		Vector2 dir = (_end - _start).Normalized();
		Vector2 perp = new(-dir.Y, dir.X);
		float wobble = Mathf.Sin(_progress * _sineFrequency * Mathf.Tau) * _amplitude;
		wobble *= Mathf.Sin(_progress * Mathf.Pi); // dampen at endpoints

		if (_progress >= 1f)
		{
			_start = _end;
			_end = PickRandomTarget();
			_progress = 0f;
		}

		return basePos + perp * wobble;
	}

	private Vector2 PickRandomTarget()
	{
		float angle = _rng.RandfRange(0f, Mathf.Tau);
		float dist = _rng.RandfRange(_wanderRadius * 0.3f, _wanderRadius);
		return _anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
	}
}
