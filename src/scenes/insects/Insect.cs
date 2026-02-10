using System;
using Godot;
using ProjectFlutter;

public partial class Insect : Area2D
{
	public enum InsectState { Arriving, Visiting, Departing, Freed }

	private InsectData _data;
	private IMovementBehavior _movement;
	private InsectState _state = InsectState.Arriving;
	private float _visitTimeRemaining;
	private float _time;
	private Vector2 _plantAnchor;
	private Vector2I _hostCellPos;
	private Tween _currentTween;
	private RandomNumberGenerator _rng;

	// Placeholder visual color (set by Initialize based on movement pattern)
	private Color _bodyColor = Colors.White;
	private float _bodyRadius = 6f;

	public InsectData Data => _data;
	public InsectState CurrentState => _state;

	private Action<TimeOfDayChangedEvent> _onTimeChanged;
	private Action<PlantRemovedEvent> _onPlantRemoved;

	public void Initialize(InsectData data, Vector2 plantPosition, Vector2 entryPosition, Vector2I hostCellPos)
	{
		_data = data;
		_plantAnchor = plantPosition;
		_hostCellPos = hostCellPos;

		_rng = new RandomNumberGenerator();
		_rng.Randomize();

		_movement = MovementBehaviorFactory.Create(data.MovementPattern, data, _rng);
		_movement.Reset(plantPosition);

		_visitTimeRemaining = _rng.RandfRange(data.VisitDurationMin, data.VisitDurationMax);

		// Placeholder colors per movement pattern
		_bodyColor = data.MovementPattern switch
		{
			MovementPattern.Hover   => new Color(1.0f, 0.85f, 0.2f),  // yellow (bee)
			MovementPattern.Flutter => new Color(0.85f, 0.4f, 0.8f),  // pink (butterfly)
			MovementPattern.Crawl   => new Color(0.9f, 0.2f, 0.15f),  // red (ladybug)
			MovementPattern.Erratic => new Color(0.6f, 0.55f, 0.45f), // brown (moth)
			_ => Colors.White,
		};

		GlobalPosition = entryPosition;
	}

	public override void _Ready()
	{
		_onTimeChanged = OnTimeOfDayChanged;
		_onPlantRemoved = OnPlantRemoved;
		EventBus.Subscribe(_onTimeChanged);
		EventBus.Subscribe(_onPlantRemoved);

		StartArrival();
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onTimeChanged);
		EventBus.Unsubscribe(_onPlantRemoved);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta * TimeManager.Instance.SpeedMultiplier;
		_time += dt;

		switch (_state)
		{
			case InsectState.Visiting:
				ProcessVisiting(dt);
				break;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		// Body
		DrawCircle(Vector2.Zero, _bodyRadius, _bodyColor);

		// Outline
		DrawArc(Vector2.Zero, _bodyRadius, 0, Mathf.Tau, 16,
			_bodyColor.Darkened(0.3f), 1.0f);

		// Idle bob indicator — small dot on top
		float bobY = Mathf.Sin(_time * 3f) * 1.5f;
		DrawCircle(new Vector2(0, bobY - _bodyRadius - 2f), 1.5f, Colors.White);

		// Movement-specific details
		switch (_data?.MovementPattern)
		{
			case MovementPattern.Flutter:
				// Wing shapes
				float wingFlap = Mathf.Sin(_time * 8f) * 4f;
				DrawCircle(new Vector2(-5f, wingFlap - 2f), 4f, _bodyColor.Lightened(0.3f));
				DrawCircle(new Vector2(5f, wingFlap - 2f), 4f, _bodyColor.Lightened(0.3f));
				break;
			case MovementPattern.Crawl:
				// Spots
				DrawCircle(new Vector2(-2f, -2f), 1.5f, Colors.Black);
				DrawCircle(new Vector2(2f, 1f), 1.5f, Colors.Black);
				break;
		}
	}

	private void StartArrival()
	{
		_state = InsectState.Arriving;
		Modulate = new Color(1, 1, 1, 0);

		_currentTween?.Kill();
		_currentTween = CreateTween();
		_currentTween.TweenProperty(this, "global_position", _plantAnchor, 1.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_currentTween.Parallel()
			.TweenProperty(this, "modulate:a", 1.0f, 0.5f).From(0.0f);
		_currentTween.TweenCallback(Callable.From(() =>
		{
			_state = InsectState.Visiting;
			EventBus.Publish(new InsectArrivedEvent(_data.Id, GlobalPosition));
		}));
	}

	private void ProcessVisiting(float dt)
	{
		_visitTimeRemaining -= dt;
		if (_visitTimeRemaining <= 0f)
		{
			StartDeparture();
			return;
		}

		Vector2 target = _movement.CalculatePosition(dt);

		// Soft clamp to max wander distance
		Vector2 offset = target - _plantAnchor;
		float maxWander = 50f;
		if (offset.Length() > maxWander)
			target = _plantAnchor + offset.Normalized() * maxWander;

		GlobalPosition = GlobalPosition.Lerp(target, dt * 8f);
	}

	private void StartDeparture()
	{
		if (_state == InsectState.Departing || _state == InsectState.Freed) return;
		_state = InsectState.Departing;

		// Fly off to a random screen edge
		Vector2 exitDir = (GlobalPosition - _plantAnchor).Normalized();
		if (exitDir.LengthSquared() < 0.1f)
			exitDir = Vector2.Right;
		Vector2 exitPos = GlobalPosition + exitDir * 500f;

		_currentTween?.Kill();
		_currentTween = CreateTween();
		_currentTween.TweenProperty(this, "global_position", exitPos, 1.2f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		_currentTween.Parallel()
			.TweenProperty(this, "modulate:a", 0.0f, 0.8f);
		_currentTween.TweenCallback(Callable.From(() =>
		{
			_state = InsectState.Freed;
			EventBus.Publish(new InsectDepartedEvent(_data.Id, GlobalPosition));
			QueueFree();
		}));
	}

	private void OnTimeOfDayChanged(TimeOfDayChangedEvent evt)
	{
		if (_data == null) return;
		// Day insects leave at night, night insects leave at day
		bool shouldLeave = _data.TimeOfDay switch
		{
			"day" => evt.NewPeriod is "dusk" or "night",
			"night" => evt.NewPeriod is "dawn" or "morning",
			_ => false // "both" never forced to leave
		};
		if (shouldLeave) StartDeparture();
	}

	private void OnPlantRemoved(PlantRemovedEvent evt)
	{
		if (evt.GridPos == _hostCellPos)
			StartDeparture();
	}
}
