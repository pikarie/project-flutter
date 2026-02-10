using System;
using Godot;
using ProjectFlutter;

public partial class HUD : Control
{
	private Label _timeLabel;
	private Label _nectarLabel;
	private Button _speedButton;
	private int _currentSpeedIndex;
	private readonly float[] _speeds = { 1.0f, 2.0f, 3.0f, 10.0f };
	private readonly string[] _speedLabels = { "x1", "x2", "x3", "x10" };

	private Action<HourPassedEvent> _onHourPassed;
	private Action<TimeOfDayChangedEvent> _onPeriodChanged;
	private Action<NectarChangedEvent> _onNectarChanged;

	public override void _Ready()
	{
		_timeLabel = GetNode<Label>("TimeLabel");
		_nectarLabel = GetNode<Label>("NectarLabel");
		_speedButton = GetNode<Button>("SpeedButton");

		_speedButton.Pressed += OnSpeedButtonPressed;

		_onHourPassed = OnHourPassed;
		_onPeriodChanged = OnPeriodChanged;
		_onNectarChanged = OnNectarChanged;

		EventBus.Subscribe(_onHourPassed);
		EventBus.Subscribe(_onPeriodChanged);
		EventBus.Subscribe(_onNectarChanged);

		UpdateTimeDisplay();
		UpdateNectarDisplay();
		UpdateSpeedButton();
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onHourPassed);
		EventBus.Unsubscribe(_onPeriodChanged);
		EventBus.Unsubscribe(_onNectarChanged);
	}

	private void OnSpeedButtonPressed()
	{
		_currentSpeedIndex = (_currentSpeedIndex + 1) % _speeds.Length;
		TimeManager.Instance.SetSpeed(_speeds[_currentSpeedIndex]);
		UpdateSpeedButton();
	}

	private void OnHourPassed(HourPassedEvent evt) => UpdateTimeDisplay();

	private void OnPeriodChanged(TimeOfDayChangedEvent evt) => UpdateTimeDisplay();

	private void OnNectarChanged(NectarChangedEvent evt) => UpdateNectarDisplay();

	private void UpdateTimeDisplay()
	{
		var tm = TimeManager.Instance;
		int hour = (int)(tm.CurrentTimeNormalized * 24.0f);
		int minute = (int)((tm.CurrentTimeNormalized * 24.0f - hour) * 60.0f);
		_timeLabel.Text = $"{hour:D2}:{minute:D2} ({tm.CurrentPeriod})";
	}

	private void UpdateNectarDisplay()
	{
		_nectarLabel.Text = $"Nectar: {GameManager.Instance.Nectar}";
	}

	private void UpdateSpeedButton()
	{
		_speedButton.Text = _speedLabels[_currentSpeedIndex];
	}
}
