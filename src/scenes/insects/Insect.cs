using System;
using Godot;
using ProjectFlutter;

public partial class Insect : Area2D
{
	public enum InsectState { Arriving, Visiting, PreDeparture, Departing, Freed }

	private const float PreDepartureMinDuration = 5f;
	private const float PreDepartureMaxDuration = 8f;
	private const float PreDepartureCircleRadius = 40f;
	private const float PreDepartureCircleSpeed = 2.5f;
	private const float PreDepartureWingSpeedMultiplier = 3f;

	private InsectData _data;
	private IMovementBehavior _movement;
	private InsectState _state = InsectState.Arriving;
	private float _visitTimeRemaining;
	private float _time;
	private Vector2 _plantAnchor;
	private Vector2I _hostCellPos;
	private Tween _currentTween;
	private RandomNumberGenerator _rng;

	// Pre-departure circling
	private float _preDepartureTimer;
	private float _preDepartureAngle;

	// Placeholder visual color (set by Initialize based on movement pattern)
	private Color _bodyColor = Colors.White;
	private float _bodyRadius = 6f;

	// Freeze state (photography)
	private bool _isFrozen;
	private double _freezeTimer;

	public InsectData Data => _data;
	public InsectState CurrentState => _state;
	public bool IsPhotographable => (_state == InsectState.Visiting || _state == InsectState.PreDeparture) && !_isFrozen && IsInsideTree();

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

		// Unique color per species derived from ID hash
		uint hash = 0;
		foreach (char character in data.Id)
			hash = hash * 31 + character;
		float hue = (hash % 360) / 360f;
		_bodyColor = Color.FromHsv(hue, 0.7f, 0.9f);

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
		if (_isFrozen)
		{
			_freezeTimer -= delta;
			if (_freezeTimer <= 0) _isFrozen = false;
			QueueRedraw();
			return;
		}

		float dt = (float)delta * TimeManager.Instance.SpeedMultiplier;
		_time += dt;

		switch (_state)
		{
			case InsectState.Visiting:
				ProcessVisiting(dt);
				break;
			case InsectState.PreDeparture:
				ProcessPreDeparture(dt);
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
				// Wing shapes — faster flap during pre-departure
				float wingSpeed = _state == InsectState.PreDeparture ? 8f * PreDepartureWingSpeedMultiplier : 8f;
				float wingFlap = Mathf.Sin(_time * wingSpeed) * 4f;
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

	public void Freeze(float duration = 0.5f)
	{
		if (_state != InsectState.Visiting && _state != InsectState.PreDeparture) return;
		_isFrozen = true;
		_freezeTimer = duration;
		Modulate = new Color(1.3f, 1.3f, 1.3f, 1f);
		CreateTween().TweenProperty(this, "modulate",
			new Color(1f, 1f, 1f, 1f), duration * 0.8f)
			.SetTrans(Tween.TransitionType.Sine);
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
			StartPreDeparture();
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

	private void StartPreDeparture()
	{
		if (_state == InsectState.PreDeparture || _state == InsectState.Departing || _state == InsectState.Freed) return;

		_state = InsectState.PreDeparture;
		_preDepartureTimer = _rng.RandfRange(PreDepartureMinDuration, PreDepartureMaxDuration);
		_preDepartureAngle = _rng.RandfRange(0f, Mathf.Tau);
	}

	private void ProcessPreDeparture(float dt)
	{
		_preDepartureTimer -= dt;
		if (_preDepartureTimer <= 0f)
		{
			StartDeparture();
			return;
		}

		// Circle around the plant anchor with increasing radius
		_preDepartureAngle += dt * PreDepartureCircleSpeed;
		float expandingRadius = PreDepartureCircleRadius * (1f + (1f - _preDepartureTimer / PreDepartureMaxDuration) * 0.5f);
		Vector2 circleTarget = _plantAnchor + new Vector2(
			Mathf.Cos(_preDepartureAngle) * expandingRadius,
			Mathf.Sin(_preDepartureAngle) * expandingRadius
		);

		GlobalPosition = GlobalPosition.Lerp(circleTarget, dt * 5f);
	}

	private void StartDeparture()
	{
		if (_state == InsectState.Departing || _state == InsectState.Freed) return;

		EventBus.Publish(new InsectDepartingEvent(_data.Id, this));
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
		if (_data == null || !CanProcess()) return;
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
		if (!CanProcess()) return;
		if (evt.GridPos == _hostCellPos)
			StartDeparture();
	}
}
