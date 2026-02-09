using Godot;

public partial class HUD : Control
{
	private Label _timeLabel;
	private Label _nectarLabel;
	private Button _speedButton;
	private int _currentSpeedIndex;
	private readonly float[] _speeds = { 1.0f, 2.0f, 3.0f, 10.0f };
	private readonly string[] _speedLabels = { "x1", "x2", "x3", "x10" };

	public override void _Ready()
	{
		_timeLabel = GetNode<Label>("TimeLabel");
		_nectarLabel = GetNode<Label>("NectarLabel");
		_speedButton = GetNode<Button>("SpeedButton");

		_speedButton.Pressed += OnSpeedButtonPressed;

		var timeManager = GetNode<TimeManager>("/root/TimeManager");
		timeManager.HourPassed += OnHourPassed;
		timeManager.TimeOfDayChanged += OnPeriodChanged;

		UpdateTimeDisplay();
		UpdateSpeedButton();
	}

	public override void _Process(double delta)
	{
		var gameManager = GetNode<GameManager>("/root/GameManager");
		_nectarLabel.Text = $"Nectar: {gameManager.Nectar}";
	}

	private void OnSpeedButtonPressed()
	{
		_currentSpeedIndex = (_currentSpeedIndex + 1) % _speeds.Length;
		var timeManager = GetNode<TimeManager>("/root/TimeManager");
		timeManager.SetSpeed(_speeds[_currentSpeedIndex]);
		UpdateSpeedButton();
	}

	private void OnHourPassed(int hour)
	{
		UpdateTimeDisplay();
	}

	private void OnPeriodChanged(string period)
	{
		UpdateTimeDisplay();
	}

	private void UpdateTimeDisplay()
	{
		var timeManager = GetNode<TimeManager>("/root/TimeManager");
		int hour = (int)(timeManager.CurrentTimeNormalized * 24.0f);
		int minute = (int)((timeManager.CurrentTimeNormalized * 24.0f - hour) * 60.0f);
		string period = timeManager.CurrentPeriod;
		_timeLabel.Text = $"{hour:D2}:{minute:D2} ({period})";
	}

	private void UpdateSpeedButton()
	{
		_speedButton.Text = _speedLabels[_currentSpeedIndex];
	}
}
