using Godot;

[GlobalClass]
public partial class InsectData : Resource
{
	[Export] public string Id { get; set; }
	[Export] public string DisplayName { get; set; }
	[Export] public ZoneType Zone { get; set; }
	[Export] public string Rarity { get; set; }
	[Export] public string TimeOfDay { get; set; }
	[Export] public string[] RequiredPlants { get; set; }
	[Export] public float SpawnWeight { get; set; }
	[Export] public float VisitDurationMin { get; set; }
	[Export] public float VisitDurationMax { get; set; }
	[Export] public string PhotoDifficulty { get; set; }
	[Export] public string MovementPattern { get; set; }
	[Export] public float MovementSpeed { get; set; }
	[Export] public float PauseFrequency { get; set; }
	[Export] public float PauseDuration { get; set; }
	[Export] public SpriteFrames GardenSprite { get; set; }
	[Export] public Texture2D JournalIllustration { get; set; }
	[Export] public Texture2D JournalSilhouette { get; set; }
	[Export] public string JournalText { get; set; }
	[Export] public string HintText { get; set; }
	[Export] public AudioStream AmbientSound { get; set; }
}
