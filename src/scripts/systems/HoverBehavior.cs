using Godot;

/// <summary>
/// Smooth noise-based hovering around an anchor (bee-like).
/// </summary>
public class HoverBehavior : IMovementBehavior
{
	private readonly FastNoiseLite _noise;
	private readonly float _hoverRadius;
	private Vector2 _anchor;
	private float _time;

	public HoverBehavior(InsectData data, RandomNumberGenerator rng)
	{
		_noise = new FastNoiseLite
		{
			Seed = rng.RandiRange(0, 99999),
			NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
			Frequency = 0.8f
		};
		_hoverRadius = 8f * (1f + rng.RandfRange(-0.15f, 0.15f));
	}

	public void Reset(Vector2 anchor) => _anchor = anchor;

	public Vector2 CalculatePosition(float delta)
	{
		_time += delta;
		return _anchor + new Vector2(
			_noise.GetNoise1D(_time) * _hoverRadius,
			_noise.GetNoise1D(_time + 1000f) * _hoverRadius
		);
	}
}
