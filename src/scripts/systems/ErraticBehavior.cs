using Godot;

/// <summary>
/// Random velocity changes with a soft tether back to anchor (moth/dragonfly-like).
/// </summary>
public class ErraticBehavior : IMovementBehavior
{
	private readonly float _speed;
	private readonly float _maxDistance;
	private readonly float _changeInterval;
	private readonly RandomNumberGenerator _rng;

	private Vector2 _anchor;
	private Vector2 _currentPos;
	private Vector2 _velocity;
	private float _changeTimer;

	public ErraticBehavior(InsectData data, RandomNumberGenerator rng)
	{
		_rng = rng;
		_speed = data.MovementSpeed * (1f + rng.RandfRange(-0.15f, 0.15f));
		_maxDistance = 35f;
		_changeInterval = 0.3f + rng.RandfRange(-0.1f, 0.1f);
		_velocity = RandomDirection() * _speed;
	}

	public void Reset(Vector2 anchor)
	{
		_anchor = anchor;
		_currentPos = anchor;
		_changeTimer = 0f;
	}

	public Vector2 CalculatePosition(float delta)
	{
		_changeTimer -= delta;
		if (_changeTimer <= 0f)
		{
			_velocity = RandomDirection() * _speed;
			_changeTimer = _changeInterval + _rng.RandfRange(-0.1f, 0.15f);
		}

		_currentPos += _velocity * delta;

		// Soft tether: lerp back toward anchor when too far
		Vector2 offset = _currentPos - _anchor;
		if (offset.Length() > _maxDistance)
			_currentPos = _currentPos.Lerp(_anchor + offset.Normalized() * _maxDistance, delta * 4f);

		return _currentPos;
	}

	private Vector2 RandomDirection()
	{
		float angle = _rng.RandfRange(0f, Mathf.Tau);
		return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
	}
}
