using Godot;
using ProjectFlutter;

[GlobalClass]
public partial class InsectData : Resource
{
	[Export] public string Id { get; set; }
	[Export] public string DisplayName { get; set; }
	[Export] public ZoneType Zone { get; set; }
	[Export] public string Rarity { get; set; }
	[Export] public string TimeOfDay { get; set; }
	[Export] public string[] RequiredPlants { get; set; }
	[Export] public float SpawnWeight { get; set; } = 1.0f;
	[Export] public int RequiredWaterTiles { get; set; }
	[Export] public int RequiredDecompositionStage { get; set; } = -1; // -1=not required, 0+=minimum stage
	[Export] public bool RequiresHeatedStone { get; set; }
	[Export] public bool RequiresUVLamp { get; set; }
	[Export] public float VisitDurationMin { get; set; } = 60.0f;
	[Export] public float VisitDurationMax { get; set; } = 180.0f;
	[Export] public string PhotoDifficulty { get; set; }
	[Export] public MovementPattern MovementPattern { get; set; } = MovementPattern.Hover;
	[Export] public float MovementSpeed { get; set; } = 30.0f;
	[Export] public float PauseFrequency { get; set; } = 0.4f;
	[Export] public float PauseDuration { get; set; } = 2.0f;
	[Export] public SpriteFrames GardenSprite { get; set; }
	[Export] public Texture2D JournalIllustration { get; set; }
	[Export] public Texture2D JournalSilhouette { get; set; }
	[Export] public string JournalText { get; set; }
	[Export] public string HintText { get; set; }
	[Export] public AudioStream AmbientSound { get; set; }
}
