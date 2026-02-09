using Godot;

public partial class TimeManager : Node
{
	[Signal] public delegate void HourPassedEventHandler(int hour);
	[Signal] public delegate void TimeOfDayChangedEventHandler(string period);

	public const float DayCycleDuration = 300.0f;

	public float SpeedMultiplier { get; private set; } = 1.0f;
	public float CurrentTimeNormalized { get; private set; } = 0.25f; // Start at morning (6h)
	public string CurrentPeriod { get; private set; }
	public bool Paused { get; set; }

	private float _accumulatedTime;
	private float _secondsPerGameMinute;
	private int _lastHour = -1;

	public override void _Ready()
	{
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
			EmitSignal(SignalName.HourPassed, currentHour);
		}

		var period = GetPeriod();
		if (period != CurrentPeriod)
		{
			CurrentPeriod = period;
			EmitSignal(SignalName.TimeOfDayChanged, CurrentPeriod);
		}
	}

	public string GetPeriod()
	{
		float hour = CurrentTimeNormalized * 24.0f;
		return hour switch
		{
			< 5.5f  => "night",
			< 7.0f  => "dawn",
			< 10.0f => "morning",
			< 14.0f => "noon",
			< 17.0f => "golden_hour",
			< 19.5f => "dusk",
			_       => "night"
		};
	}

	public void SetSpeed(float multiplier) => SpeedMultiplier = multiplier;

	public bool IsDaytime() => CurrentPeriod is "dawn" or "morning" or "noon" or "golden_hour";

	public bool IsNighttime() => CurrentPeriod is "dusk" or "night";
}
