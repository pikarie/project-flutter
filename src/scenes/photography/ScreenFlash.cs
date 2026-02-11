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
		Color = new Color(1, 1, 1, 0.7f);
		var tween = CreateTween();
		tween.TweenProperty(this, "color:a", 0f, 0.3f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
	}
}
