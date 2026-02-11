using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class DiscoveryNotification : Control
{
	private const float SlideDuration = 0.4f;
	private const float DisplayDuration = 2.5f;
	private const float FadeOutDuration = 0.5f;
	private const float TotalDuration = SlideDuration + DisplayDuration + FadeOutDuration;

	private readonly Queue<NotificationData> _queue = new();
	private NotificationData _current;
	private float _timer;
	private bool _showing;

	private Action<SpeciesDiscoveredEvent> _onDiscovered;
	private Action<JournalUpdatedEvent> _onUpdated;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_onDiscovered = OnSpeciesDiscovered;
		_onUpdated = OnJournalUpdated;
		EventBus.Subscribe(_onDiscovered);
		EventBus.Subscribe(_onUpdated);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onDiscovered);
		EventBus.Unsubscribe(_onUpdated);
	}

	public override void _Process(double delta)
	{
		if (!_showing)
		{
			if (_queue.Count > 0)
			{
				_current = _queue.Dequeue();
				_timer = 0f;
				_showing = true;
			}
			else
			{
				return;
			}
		}

		_timer += (float)delta;
		if (_timer >= TotalDuration)
		{
			_showing = false;
			_current = null;
			QueueRedraw();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_showing || _current == null) return;

		var font = ThemeDB.FallbackFont;
		float viewportWidth = GetViewportRect().Size.X;

		// Calculate animation phases
		float slideProgress = Mathf.Clamp(_timer / SlideDuration, 0f, 1f);
		float fadeProgress = _timer > SlideDuration + DisplayDuration
			? Mathf.Clamp((_timer - SlideDuration - DisplayDuration) / FadeOutDuration, 0f, 1f)
			: 0f;

		float alpha = 1f - fadeProgress;
		float yPosition = Mathf.Lerp(-60f, 20f, Mathf.Ease(slideProgress, -2f));

		// Banner dimensions
		float bannerWidth = _current.IsNewDiscovery ? 340f : 300f;
		float bannerHeight = _current.IsNewDiscovery ? 60f : 50f;
		float bannerX = (viewportWidth - bannerWidth) / 2f;

		// Background
		Color backgroundColor = _current.IsNewDiscovery
			? new Color(0.15f, 0.4f, 0.15f, 0.85f * alpha)
			: new Color(0.4f, 0.35f, 0.1f, 0.85f * alpha);
		DrawRect(new Rect2(bannerX, yPosition, bannerWidth, bannerHeight), backgroundColor);

		// Border
		Color borderColor = _current.IsNewDiscovery
			? new Color(0.5f, 1f, 0.5f, 0.6f * alpha)
			: new Color(1f, 0.85f, 0.2f, 0.6f * alpha);
		DrawRect(new Rect2(bannerX, yPosition, bannerWidth, bannerHeight), borderColor, false, 2f);

		if (_current.IsNewDiscovery)
		{
			// Title line: "New Species Discovered!"
			var titleColor = new Color(0.7f, 1f, 0.7f, alpha);
			DrawString(font, new Vector2(bannerX + 15f, yPosition + 22f),
				"New Species Discovered!", HorizontalAlignment.Left, (int)bannerWidth - 30, 16, titleColor);

			// Species name line
			var nameColor = new Color(1f, 1f, 1f, alpha);
			DrawString(font, new Vector2(bannerX + 15f, yPosition + 46f),
				_current.DisplayName, HorizontalAlignment.Left, (int)bannerWidth - 30, 20, nameColor);
		}
		else
		{
			// Star upgrade: "Species Name ★★★"
			var nameColor = new Color(1f, 1f, 1f, alpha);
			string starText = new string('\u2605', _current.StarRating) + new string('\u2606', 3 - _current.StarRating);
			DrawString(font, new Vector2(bannerX + 15f, yPosition + 22f),
				_current.DisplayName, HorizontalAlignment.Left, (int)bannerWidth - 30, 18, nameColor);

			var starColor = new Color(1f, 0.85f, 0.2f, alpha);
			DrawString(font, new Vector2(bannerX + 15f, yPosition + 42f),
				$"New best: {starText}", HorizontalAlignment.Left, (int)bannerWidth - 30, 16, starColor);
		}
	}

	private void OnSpeciesDiscovered(SpeciesDiscoveredEvent discoveredEvent)
	{
		var species = InsectRegistry.GetById(discoveredEvent.InsectId);
		if (species == null) return;

		_queue.Enqueue(new NotificationData
		{
			DisplayName = species.DisplayName,
			IsNewDiscovery = true,
			StarRating = JournalManager.Instance.GetStarRating(discoveredEvent.InsectId),
		});
	}

	private void OnJournalUpdated(JournalUpdatedEvent updatedEvent)
	{
		var species = InsectRegistry.GetById(updatedEvent.InsectId);
		if (species == null) return;

		_queue.Enqueue(new NotificationData
		{
			DisplayName = species.DisplayName,
			IsNewDiscovery = false,
			StarRating = updatedEvent.StarRating,
		});
	}

	private class NotificationData
	{
		public string DisplayName;
		public bool IsNewDiscovery;
		public int StarRating;
	}
}
