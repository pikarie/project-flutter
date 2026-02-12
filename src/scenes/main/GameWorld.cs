using System;
using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class GameWorld : Node2D
{
	private readonly Dictionary<ZoneType, Node2D> _zones = new();
	private Action<ZoneChangedEvent> _onZoneChanged;

	public override void _Ready()
	{
		_zones[ZoneType.Starter] = GetNode<Node2D>("StarterZone");
		_zones[ZoneType.Meadow] = GetNode<Node2D>("MeadowZone");
		_zones[ZoneType.Pond] = GetNode<Node2D>("PondZone");

		// Initialize: only Starter visible and processing
		foreach (var (zone, node) in _zones)
		{
			bool active = zone == ZoneType.Starter;
			node.Visible = active;
			node.ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		}

		_onZoneChanged = OnZoneChanged;
		EventBus.Subscribe(_onZoneChanged);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onZoneChanged);
	}

	private void OnZoneChanged(ZoneChangedEvent zoneEvent)
	{
		if (_zones.TryGetValue(zoneEvent.From, out var fromZone))
		{
			fromZone.Visible = false;
			fromZone.ProcessMode = ProcessModeEnum.Disabled;
		}
		if (_zones.TryGetValue(zoneEvent.To, out var toZone))
		{
			toZone.Visible = true;
			toZone.ProcessMode = ProcessModeEnum.Inherit;
		}
	}
}
