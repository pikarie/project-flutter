using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class HarvestPopup : Control
{
	private const float DisplayDuration = 1.5f;
	private const float DriftDistance = 50f;

	private readonly List<PopupEntry> _activePopups = new();
	private Action<PlantHarvestedEvent> _onHarvested;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_onHarvested = OnHarvested;
		EventBus.Subscribe(_onHarvested);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onHarvested);
	}

	public override void _Process(double delta)
	{
		if (_activePopups.Count == 0) return;

		for (int i = _activePopups.Count - 1; i >= 0; i--)
		{
			_activePopups[i].Timer += (float)delta;
			if (_activePopups[i].Timer >= DisplayDuration)
				_activePopups.RemoveAt(i);
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_activePopups.Count == 0) return;

		var font = ThemeDB.FallbackFont;

		foreach (var popup in _activePopups)
		{
			float progress = popup.Timer / DisplayDuration;
			float alpha = progress < 0.7f ? 1f : 1f - (progress - 0.7f) / 0.3f;
			float yOffset = -DriftDistance * progress;

			Vector2 drawPosition = popup.ScreenPosition + new Vector2(0, yOffset);

			// Background pill
			string text = $"+{popup.NectarYield}";
			float pillWidth = 60f;
			float pillHeight = 30f;
			var backgroundRect = new Rect2(
				drawPosition.X - pillWidth / 2f,
				drawPosition.Y - pillHeight / 2f,
				pillWidth, pillHeight);
			DrawRect(backgroundRect, new Color(0, 0, 0, 0.5f * alpha));

			// Nectar text in golden color
			var textColor = new Color(1f, 0.85f, 0.2f, alpha);
			DrawString(font, drawPosition + new Vector2(-18, 7), text,
				HorizontalAlignment.Left, 50, 20, textColor);
		}
	}

	private void OnHarvested(PlantHarvestedEvent harvestEvent)
	{
		var camera = GetViewport().GetCamera2D();
		Vector2 screenPosition;
		if (camera != null)
		{
			var viewportSize = GetViewportRect().Size;
			screenPosition = (harvestEvent.WorldPosition - camera.GlobalPosition) * camera.Zoom
				+ viewportSize / 2f;
		}
		else
		{
			screenPosition = harvestEvent.WorldPosition;
		}

		_activePopups.Add(new PopupEntry
		{
			ScreenPosition = screenPosition,
			NectarYield = harvestEvent.NectarYield,
			Timer = 0f,
		});
	}

	private class PopupEntry
	{
		public Vector2 ScreenPosition;
		public int NectarYield;
		public float Timer;
	}
}
