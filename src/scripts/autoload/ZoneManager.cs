using System.Collections.Generic;
using Godot;
using ProjectFlutter;

public partial class ZoneManager : Node
{
	public static ZoneManager Instance { get; private set; }

	private ZoneType _activeZone = ZoneType.Starter;
	private readonly Dictionary<ZoneType, bool> _unlockedZones = new()
	{
		{ ZoneType.Starter, true },
		{ ZoneType.Meadow, false },
		{ ZoneType.Pond, false },
	};

	public static readonly Dictionary<ZoneType, (string Name, int Width, int Height, int NectarCost, int JournalRequired)> ZoneConfig = new()
	{
		{ ZoneType.Starter, ("Starter Garden", 4, 4, 0, 0) },
		{ ZoneType.Meadow, ("Meadow", 6, 6, 100, 5) },
		{ ZoneType.Pond, ("Pond Edge", 5, 5, 150, 12) },
	};

	public ZoneType ActiveZone => _activeZone;

	public override void _Ready()
	{
		Instance = this;
	}

	public bool IsUnlocked(ZoneType zone) => _unlockedZones[zone];

	public void SwitchToZone(ZoneType target)
	{
		if (target == _activeZone || !_unlockedZones[target]) return;
		var previous = _activeZone;
		_activeZone = target;
		GameManager.Instance.CurrentZone = target;
		EventBus.Publish(new ZoneChangedEvent(previous, target));
	}

	public bool CanUnlock(ZoneType zone)
	{
		if (_unlockedZones[zone]) return false;
		var (_, _, _, cost, journalRequired) = ZoneConfig[zone];
		return GameManager.Instance.Nectar >= cost
			&& JournalManager.Instance.GetDiscoveredCount() >= journalRequired;
	}

	public bool TryUnlock(ZoneType zone)
	{
		if (!CanUnlock(zone)) return false;
		var (_, _, _, cost, _) = ZoneConfig[zone];
		GameManager.Instance.SpendNectar(cost);
		_unlockedZones[zone] = true;
		EventBus.Publish(new ZoneUnlockedEvent(zone));
		return true;
	}

	public void DebugUnlockAll()
	{
		foreach (var zone in ZoneConfig.Keys)
		{
			if (!_unlockedZones[zone])
			{
				_unlockedZones[zone] = true;
				EventBus.Publish(new ZoneUnlockedEvent(zone));
			}
		}
		GD.Print("DEBUG: All zones unlocked");
	}
}
