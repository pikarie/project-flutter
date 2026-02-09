using Godot;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }

	[Signal] public delegate void PlantPlantedEventHandler(Resource plantData, Vector2I gridPos);
	[Signal] public delegate void PlantHarvestedEventHandler(Resource plantData, Vector2I gridPos);
	[Signal] public delegate void PlantBloomingEventHandler(Node2D plant);
	[Signal] public delegate void PauseToggledEventHandler(bool isPaused);

	public override void _Ready() => Instance = this;

	public void EmitPlantPlanted(PlantData data, Vector2I pos)
		=> EmitSignal(SignalName.PlantPlanted, data, pos);

	public void EmitPlantHarvested(PlantData data, Vector2I pos)
		=> EmitSignal(SignalName.PlantHarvested, data, pos);

	public void EmitPlantBlooming(Node2D plant)
		=> EmitSignal(SignalName.PlantBlooming, plant);
}
