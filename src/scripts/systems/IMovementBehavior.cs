using Godot;
using ProjectFlutter;

public interface IMovementBehavior
{
	Vector2 CalculatePosition(float delta);
	void Reset(Vector2 anchor);
}

public static class MovementBehaviorFactory
{
	public static IMovementBehavior Create(MovementPattern pattern, InsectData data, RandomNumberGenerator rng)
		=> pattern switch
		{
			MovementPattern.Hover   => new HoverBehavior(data, rng),
			MovementPattern.Flutter => new FlutterBehavior(data, rng),
			MovementPattern.Crawl   => new CrawlBehavior(data, rng),
			MovementPattern.Erratic => new ErraticBehavior(data, rng),
			_ => new HoverBehavior(data, rng),
		};
}
