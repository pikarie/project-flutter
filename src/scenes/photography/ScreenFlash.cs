using System;
using Godot;
using ProjectFlutter;

public partial class ScreenFlash : ColorRect
{
	private Action<PhotoTakenEvent> _onPhotoTaken;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Color = new Color(1, 1, 1, 0);

		_onPhotoTaken = OnPhotoTaken;
		EventBus.Subscribe(_onPhotoTaken);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onPhotoTaken);
	}

	private void OnPhotoTaken(PhotoTakenEvent photoEvent)
	{
		// Proportional flash intensity based on quality
		float flashAlpha;
		float fadeDuration;
		Color flashColor;

		if (photoEvent.IsNewDiscovery)
		{
			flashAlpha = 0.9f;
			fadeDuration = 0.5f;
			flashColor = new Color(1f, 0.95f, 0.8f, flashAlpha); // warm gold tint
		}
		else
		{
			flashColor = photoEvent.StarRating switch
			{
				3 => new Color(1f, 0.97f, 0.85f, 0f), // warm white
				2 => new Color(1f, 1f, 1f, 0f),
				_ => new Color(1f, 1f, 1f, 0f),
			};
			flashAlpha = photoEvent.StarRating switch
			{
				3 => 0.7f,
				2 => 0.5f,
				_ => 0.3f,
			};
			fadeDuration = photoEvent.StarRating switch
			{
				3 => 0.4f,
				2 => 0.3f,
				_ => 0.2f,
			};
			flashColor = new Color(flashColor.R, flashColor.G, flashColor.B, flashAlpha);
		}

		Color = flashColor;
		var tween = CreateTween();
		tween.TweenProperty(this, "color:a", 0f, fadeDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
	}
}
