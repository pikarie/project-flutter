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
		{ ZoneType.Forest, false },
		{ ZoneType.DeepWood, false },
		{ ZoneType.RockGarden, false },
		{ ZoneType.Pond, false },
		{ ZoneType.Tropical, false },
	};

	public static readonly Dictionary<ZoneType, (string Name, int Width, int Height, int NectarCost, int JournalRequired)> ZoneConfig = new()
	{
		{ ZoneType.Starter, ("Starter Garden", 5, 5, 0, 0) },
		{ ZoneType.Meadow, ("Meadow", 6, 6, 100, 5) },
		{ ZoneType.Forest, ("Forest", 6, 6, 200, 11) },
		{ ZoneType.DeepWood, ("Deep Wood", 5, 5, 350, 14) },
		{ ZoneType.RockGarden, ("Rock Garden", 5, 5, 500, 18) },
		{ ZoneType.Pond, ("Pond Edge", 5, 5, 700, 20) },
		{ ZoneType.Tropical, ("Tropical Greenhouse", 7, 7, 1000, 54) },
	};

	private readonly Dictionary<ZoneType, int> _expansionTiers = new()
	{
		{ ZoneType.Starter, 0 },
		{ ZoneType.Meadow, 0 },
		{ ZoneType.Forest, 0 },
		{ ZoneType.DeepWood, 0 },
		{ ZoneType.RockGarden, 0 },
		{ ZoneType.Pond, 0 },
		{ ZoneType.Tropical, 0 },
	};

	public ZoneType ActiveZone => _activeZone;

	public override void _Ready()
	{
		Instance = this;
	}

	public int GetExpansionTier(ZoneType zone) => _expansionTiers[zone];

	public Vector2I GetCurrentGridSize(ZoneType zone)
	{
		int tier = _expansionTiers[zone];
		if (tier == 0)
		{
			var (_, width, height, _, _) = ZoneConfig[zone];
			return new Vector2I(width, height);
		}
		var tierData = ExpansionConfig.GetTierData(zone, tier);
		return new Vector2I(tierData.GridWidth, tierData.GridHeight);
	}

	public bool CanExpand(ZoneType zone)
	{
		int currentTier = _expansionTiers[zone];
		if (currentTier >= ExpansionConfig.MaxExpansionTier(zone)) return false;
		var nextTierData = ExpansionConfig.GetTierData(zone, currentTier + 1);
		return GameManager.Instance.Nectar >= nextTierData.NectarCost;
	}

	public bool TryExpand(ZoneType zone)
	{
		int currentTier = _expansionTiers[zone];
		if (currentTier >= ExpansionConfig.MaxExpansionTier(zone)) return false;
		var nextTierData = ExpansionConfig.GetTierData(zone, currentTier + 1);
		if (!GameManager.Instance.SpendNectar(nextTierData.NectarCost)) return false;
		_expansionTiers[zone] = currentTier + 1;
		EventBus.Publish(new ZoneExpandedEvent(zone, currentTier + 1, nextTierData.Name));
		return true;
	}

	public int GetInsectCap(ZoneType zone)
	{
		const int baseInsectCap = 12;
		int tier = _expansionTiers[zone];
		int bonus = 0;
		for (int i = 1; i <= tier; i++)
		{
			var tierData = ExpansionConfig.GetTierData(zone, i);
			if (tierData != null) bonus += tierData.InsectCapBonus;
		}
		return baseInsectCap + bonus;
	}

	public bool IsUnlocked(ZoneType zone) => _unlockedZones[zone];

	public void SwitchToZone(ZoneType target)
	{
		if (target == _activeZone || !_unlockedZones[target]) return;
		var previous = _activeZone;
		_activeZone = target;
		GameManager.Instance.CurrentZone = target;

		if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
			GameManager.Instance.ChangeState(GameManager.GameState.Playing);

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

	private static readonly ZoneType[] ZoneOrder =
	{
		ZoneType.Starter, ZoneType.Meadow, ZoneType.Forest,
		ZoneType.DeepWood, ZoneType.RockGarden, ZoneType.Pond, ZoneType.Tropical,
	};

	public void CycleZoneNext()
	{
		int current = System.Array.IndexOf(ZoneOrder, _activeZone);
		for (int i = 1; i < ZoneOrder.Length; i++)
		{
			var candidate = ZoneOrder[(current + i) % ZoneOrder.Length];
			if (_unlockedZones[candidate])
			{
				SwitchToZone(candidate);
				return;
			}
		}
	}

	public void CycleZonePrevious()
	{
		int current = System.Array.IndexOf(ZoneOrder, _activeZone);
		for (int i = 1; i < ZoneOrder.Length; i++)
		{
			var candidate = ZoneOrder[(current - i + ZoneOrder.Length) % ZoneOrder.Length];
			if (_unlockedZones[candidate])
			{
				SwitchToZone(candidate);
				return;
			}
		}
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

	public void DebugExpandActive()
	{
		var zone = _activeZone;
		int currentTier = _expansionTiers[zone];
		int maxTier = ExpansionConfig.MaxExpansionTier(zone);
		if (currentTier >= maxTier)
		{
			GD.Print($"DEBUG: {zone} already at max expansion tier {currentTier}");
			return;
		}
		_expansionTiers[zone] = currentTier + 1;
		var tierData = ExpansionConfig.GetTierData(zone, currentTier + 1);
		EventBus.Publish(new ZoneExpandedEvent(zone, currentTier + 1, tierData.Name));
		GD.Print($"DEBUG: {zone} expanded to tier {currentTier + 1} ({tierData.Name}, {tierData.GridWidth}×{tierData.GridHeight})");
	}
}
