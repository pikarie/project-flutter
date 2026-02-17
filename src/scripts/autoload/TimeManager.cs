using Godot;
using ProjectFlutter;

public partial class TimeManager : Node
{
	public static TimeManager Instance { get; private set; }

	public const float DayCycleDuration = 900.0f; // 15 minutes real-time per full 24h cycle

	private static readonly float[] SpeedOptions = { 0.5f, 1.0f, 2.0f };
	private int _speedIndex = 1; // default ×1

	public float SpeedMultiplier { get; private set; } = 1.0f;
	private bool _debugSpeedActive;
	private float _preDebugSpeed;
	public float CurrentTimeNormalized { get; private set; } = 0.25f; // Start at morning (6h)
	public string CurrentPeriod { get; private set; }
	public bool Paused { get; set; }
	public float CurrentGameHour => CurrentTimeNormalized * 24.0f;

	private float _accumulatedTime;
	private float _secondsPerGameMinute;
	private int _lastHour = -1;
	private int _dayCount;

	public override void _Ready()
	{
		Instance = this;
		_secondsPerGameMinute = DayCycleDuration / (24.0f * 60.0f);
		CurrentPeriod = GetPeriod();
	}

	public override void _Process(double delta)
	{
		if (Paused) return;

		_accumulatedTime += (float)delta * SpeedMultiplier;
		while (_accumulatedTime >= _secondsPerGameMinute)
		{
			_accumulatedTime -= _secondsPerGameMinute;
			AdvanceMinute();
		}
	}

	private void AdvanceMinute()
	{
		CurrentTimeNormalized += 1.0f / (24.0f * 60.0f);
		if (CurrentTimeNormalized >= 1.0f) CurrentTimeNormalized -= 1.0f;

		int currentHour = (int)(CurrentTimeNormalized * 24.0f);
		if (currentHour != _lastHour)
		{
			_lastHour = currentHour;
			EventBus.Publish(new HourPassedEvent(currentHour));
		}

		var period = GetPeriod();
		if (period != CurrentPeriod)
		{
			var old = CurrentPeriod;
			CurrentPeriod = period;
			EventBus.Publish(new TimeOfDayChangedEvent(old, CurrentPeriod));

			// Dawn marks the start of a new day
			if (CurrentPeriod == "dawn" && old == "night")
			{
				_dayCount++;
				EventBus.Publish(new DayEndedEvent(_dayCount));
			}
		}
	}

	public string GetPeriod()
	{
		float hour = CurrentTimeNormalized * 24.0f;
		return hour switch
		{
			< 5.0f  => "night",
			< 7.0f  => "dawn",
			< 12.0f => "morning",
			< 17.0f => "afternoon",
			< 19.0f => "dusk",
			_       => "night"
		};
	}

	public float CycleSpeed()
	{
		_speedIndex = (_speedIndex + 1) % SpeedOptions.Length;
		SpeedMultiplier = SpeedOptions[_speedIndex];
		EventBus.Publish(new SpeedChangedEvent(SpeedMultiplier));
		return SpeedMultiplier;
	}

	public void SetSpeed(float multiplier) => SpeedMultiplier = multiplier;

	public void ToggleDebugSpeed(float debugMultiplier)
	{
		if (_debugSpeedActive)
		{
			if (SpeedMultiplier == debugMultiplier)
			{
				// Same debug speed pressed again → restore previous
				SpeedMultiplier = _preDebugSpeed;
				_debugSpeedActive = false;
			}
			else
			{
				// Different debug speed → switch directly, keep saved speed
				SpeedMultiplier = debugMultiplier;
			}
		}
		else
		{
			_preDebugSpeed = SpeedMultiplier;
			SpeedMultiplier = debugMultiplier;
			_debugSpeedActive = true;
		}
		EventBus.Publish(new SpeedChangedEvent(SpeedMultiplier));
	}

	public bool IsDaytime() => CurrentPeriod is "dawn" or "morning" or "afternoon";

	public bool IsNighttime() => CurrentPeriod is "dusk" or "night";
}
