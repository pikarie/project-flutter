using Godot;

[GlobalClass]
public partial class PlantData : Resource
{
	[Export] public string Id { get; set; }
	[Export] public string DisplayName { get; set; }
	[Export] public ZoneType Zone { get; set; }
	[Export] public string Rarity { get; set; }
	[Export] public int SeedCost { get; set; }
	[Export] public int NectarYield { get; set; }
	[Export] public int GrowthCycles { get; set; }
	[Export] public int InsectSlots { get; set; }
	[Export] public bool NightBlooming { get; set; }
	[Export] public Texture2D[] GrowthSprites { get; set; }
	[Export] public string[] AttractedInsects { get; set; }
	[Export] public Color DrawColor { get; set; } = new Color(0.9f, 0.4f, 0.6f);
}
