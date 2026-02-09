using Godot;

public partial class Plant : Node2D
{
	[Export] public PlantData PlantData { get; set; }

	public enum GrowthStage { Seed, Sprout, Growing, Blooming }

	public GrowthStage CurrentStage { get; private set; } = GrowthStage.Seed;
	public float GrowthProgressHours { get; set; }
	public bool IsWatered { get; set; }

	private float[] _stageThresholds;
	private Sprite2D _sprite;
	private AnimationPlayer _animPlayer;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		ComputeThresholds();
		UpdateVisuals();

		var timeManager = GetNode<TimeManager>("/root/TimeManager");
		timeManager.HourPassed += OnHourPassed;
	}

	private void ComputeThresholds()
	{
		// Each stage takes GrowthCycles hours to complete
		// Seed→Sprout, Sprout→Growing, Growing→Blooming
		float hoursPerStage = PlantData.GrowthCycles;
		_stageThresholds = new float[]
		{
			hoursPerStage,
			hoursPerStage * 2,
			hoursPerStage * 3
		};
	}

	private void OnHourPassed(int hour)
	{
		if (CurrentStage == GrowthStage.Blooming) return;
		if (!IsWatered) return;

		GrowthProgressHours += 1.0f;
		CheckStageAdvancement();
	}

	private void CheckStageAdvancement()
	{
		for (int i = _stageThresholds.Length - 1; i >= 0; i--)
		{
			if (GrowthProgressHours >= _stageThresholds[i])
			{
				var newStage = (GrowthStage)(i + 1);
				if (newStage != CurrentStage)
				{
					CurrentStage = newStage;
					UpdateVisuals();
					if (CurrentStage == GrowthStage.Blooming)
						EventBus.Instance.EmitPlantBlooming(this);
				}
				break;
			}
		}
	}

	private void UpdateVisuals()
	{
		int index = (int)CurrentStage;
		if (PlantData.GrowthSprites != null && index < PlantData.GrowthSprites.Length)
			_sprite.Texture = PlantData.GrowthSprites[index];

		if (_animPlayer.HasAnimation("stage_transition"))
			_animPlayer.Play("stage_transition");
	}

	public void Water()
	{
		IsWatered = true;
	}

	public int Harvest()
	{
		if (CurrentStage != GrowthStage.Blooming) return 0;

		int nectar = PlantData.NectarYield;
		CurrentStage = GrowthStage.Growing;
		GrowthProgressHours = _stageThresholds[1]; // Reset to start of Growing
		IsWatered = false;
		UpdateVisuals();
		return nectar;
	}
}
