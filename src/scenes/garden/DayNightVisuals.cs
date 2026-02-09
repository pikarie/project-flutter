using Godot;
using System.Collections.Generic;

public partial class DayNightVisuals : CanvasModulate
{
	private static readonly Dictionary<string, Color> PeriodColors = new()
	{
		{ "dawn",        new Color(0.94f, 0.78f, 0.73f) },
		{ "morning",     new Color(1.0f, 0.96f, 0.90f) },
		{ "noon",        new Color(1.0f, 1.0f, 1.0f) },
		{ "golden_hour", new Color(1.0f, 0.90f, 0.70f) },
		{ "dusk",        new Color(0.85f, 0.65f, 0.70f) },
		{ "night",       new Color(0.30f, 0.35f, 0.55f) },
	};

	private Tween _currentTween;

	public override void _Ready()
	{
		var timeManager = GetNode<TimeManager>("/root/TimeManager");
		timeManager.TimeOfDayChanged += OnPeriodChanged;

		// Set initial color
		if (PeriodColors.TryGetValue(timeManager.CurrentPeriod, out var initial))
			Color = initial;
	}

	private void OnPeriodChanged(string period)
	{
		if (!PeriodColors.TryGetValue(period, out var target))
			target = Colors.White;

		_currentTween?.Kill();
		_currentTween = CreateTween();
		_currentTween.TweenProperty(this, "color", target, 2.0)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Sine);
	}
}
