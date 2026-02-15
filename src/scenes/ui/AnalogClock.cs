using System;
using Godot;
using ProjectFlutter;

public partial class AnalogClock : Control
{
	private const float ClockRadius = 64.0f;
	private const int SegmentCount = 12;
	private const int ArcPointsPerSegment = 8;

	// Clock face colors — vibrant for readability
	private static readonly Color NightColor = new(0.15f, 0.15f, 0.40f);
	private static readonly Color DawnColor = new(0.95f, 0.70f, 0.55f);
	private static readonly Color MorningColor = new(1.0f, 0.90f, 0.50f);
	private static readonly Color AfternoonColor = new(0.55f, 0.80f, 1.0f);
	private static readonly Color DuskColor = new(0.70f, 0.45f, 0.70f);

	private static readonly Color BorderColor = new(0.30f, 0.20f, 0.15f);
	private static readonly Color HandColor = new(0.20f, 0.15f, 0.10f);
	private static readonly Color CenterColor = new(0.25f, 0.18f, 0.12f);

	private Action<SpeedChangedEvent> _onSpeedChanged;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(ClockRadius * 2 + 8, ClockRadius * 2 + 8);
		MouseFilter = MouseFilterEnum.Stop;

		_onSpeedChanged = _ => QueueRedraw();
		EventBus.Subscribe(_onSpeedChanged);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onSpeedChanged);
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
		{
			TimeManager.Instance.CycleSpeed();
			AcceptEvent();
		}
	}

	public override void _Draw()
	{
		Vector2 center = Size / 2;

		// 12 colored segments — flat color, no blur
		for (int i = 0; i < SegmentCount; i++)
		{
			float startHour = i * 2f;
			float endHour = (i + 1) * 2f;
			float midHour = startHour + 1f;
			DrawClockSegment(center, ClockRadius, startHour, endHour, GetColorForHour(midHour));
		}

		// Thin separator lines between segments
		for (int i = 0; i < SegmentCount; i++)
		{
			float hour = i * 2f;
			float angle = (hour / 24f) * Mathf.Tau;
			Vector2 direction = new(Mathf.Sin(angle), -Mathf.Cos(angle));
			DrawLine(center + direction * 18f, center + direction * ClockRadius, BorderColor, 1.0f);
		}

		// Border ring
		DrawArc(center, ClockRadius, 0, Mathf.Tau, 64, BorderColor, 2.5f);

		// Hour hand — one full revolution per 24h game time
		float currentHour = TimeManager.Instance.CurrentGameHour;
		float handAngle = (currentHour / 24f) * Mathf.Tau;
		Vector2 handDirection = new(Mathf.Sin(handAngle), -Mathf.Cos(handAngle));
		Vector2 handEnd = center + handDirection * (ClockRadius - 8);
		DrawLine(center, handEnd, HandColor, 3.0f);
		DrawCircle(handEnd, 4f, HandColor);

		// Center circle
		float centerRadius = 18f;
		DrawCircle(center, centerRadius, CenterColor);
		DrawArc(center, centerRadius, 0, Mathf.Tau, 32, BorderColor, 2.0f);

		// Speed text centered in the circle — white for readability
		float speed = TimeManager.Instance.SpeedMultiplier;
		string speedText = speed switch
		{
			0.5f => ".5",
			10.0f => "10",
			50.0f => "50",
			2.0f => "2",
			_ => "1"
		};
		var font = ThemeDB.FallbackFont;
		int fontSize = speedText.Length > 1 ? 12 : 16;
		Vector2 textSize = font.GetStringSize(speedText, HorizontalAlignment.Center, -1, fontSize);
		Vector2 textPosition = center + new Vector2(-textSize.X / 2, textSize.Y * 0.35f);
		DrawString(font, textPosition, speedText, HorizontalAlignment.Left, -1, fontSize, Colors.White);
	}

	private void DrawClockSegment(Vector2 center, float radius, float startHour, float endHour, Color color)
	{
		float startAngle = (startHour / 24f) * Mathf.Tau;
		float endAngle = (endHour / 24f) * Mathf.Tau;

		int vertexCount = ArcPointsPerSegment + 2;
		var points = new Vector2[vertexCount];
		var colors = new Color[vertexCount];

		points[0] = center;
		colors[0] = color;

		for (int j = 0; j <= ArcPointsPerSegment; j++)
		{
			float t = (float)j / ArcPointsPerSegment;
			float angle = Mathf.Lerp(startAngle, endAngle, t);
			points[j + 1] = center + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * radius;
			colors[j + 1] = color;
		}

		DrawPolygon(points, colors);
	}

	private static Color GetColorForHour(float hour)
	{
		hour %= 24f;
		if (hour < 0) hour += 24f;

		return hour switch
		{
			< 5f => NightColor,
			< 7f => DawnColor,
			< 12f => MorningColor,
			< 17f => AfternoonColor,
			< 19f => DuskColor,
			_ => NightColor
		};
	}
}
