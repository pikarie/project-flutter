using Godot;

public partial class TimeManager : Node
{
	[Signal] public delegate void TimeOfDayChangedEventHandler(string timeOfDay);
	[Signal] public delegate void CycleTickEventHandler(float normalizedTime);

	public const float DayCycleDuration = 300.0f;
	public const float DawnRatio = 0.05f;
	public const float DuskRatio = 0.05f;

	public float ElapsedTime { get; private set; }
	public float SpeedMultiplier { get; private set; } = 1.0f;
	public string CurrentTimeOfDay { get; private set; } = "day";
	public bool Paused { get; set; }

	public override void _Process(double delta)
	{
		if (Paused) return;

		ElapsedTime += (float)delta * SpeedMultiplier;
		float normalized = (ElapsedTime % DayCycleDuration) / DayCycleDuration;
		EmitSignal(SignalName.CycleTick, normalized);
		UpdateTimeOfDay(normalized);
	}

	private void UpdateTimeOfDay(float normalized)
	{
		string newTime;
		if (normalized < DawnRatio)
			newTime = "dawn";
		else if (normalized < 0.5f - DuskRatio)
			newTime = "day";
		else if (normalized < 0.5f + DuskRatio)
			newTime = "dusk";
		else
			newTime = "night";

		if (newTime != CurrentTimeOfDay)
		{
			CurrentTimeOfDay = newTime;
			EmitSignal(SignalName.TimeOfDayChanged, CurrentTimeOfDay);
		}
	}

	public void SetSpeed(float multiplier) => SpeedMultiplier = multiplier;

	public float GetNormalizedTime() =>
		(ElapsedTime % DayCycleDuration) / DayCycleDuration;

	public bool IsDaytime() =>
		CurrentTimeOfDay is "day" or "dawn";

	public bool IsNighttime() =>
		CurrentTimeOfDay is "night" or "dusk";
}
