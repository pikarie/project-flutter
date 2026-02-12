using Godot;

namespace ProjectFlutter;

// -- Time --
public record HourPassedEvent(int Hour);
public record TimeOfDayChangedEvent(string OldPeriod, string NewPeriod);

// -- Game state --
public record GameStateChangedEvent(GameManager.GameState NewState);
public record NectarChangedEvent(int NewAmount);
public record PauseToggledEvent(bool IsPaused);

// -- Plants --
public record PlantPlantedEvent(string PlantType, Vector2I GridPos);
public record PlantHarvestedEvent(string PlantType, Vector2I GridPos, int NectarYield, Vector2 WorldPosition);
public record PlantBloomingEvent(Vector2I GridPos);
public record PlantRemovedEvent(Vector2I GridPos);

// -- Journal --
public record SpeciesDiscoveredEvent(string InsectId);
public record JournalUpdatedEvent(string InsectId, int StarRating);

// -- Insects --
public record InsectArrivedEvent(string InsectId, Vector2 Position);
public record InsectDepartingEvent(string InsectId, Node2D Insect);
public record InsectDepartedEvent(string InsectId, Vector2 Position);
public record InsectClickedEvent(string InsectId, Node2D Insect, Vector2 Position);

// -- Photography --
public record PhotoTakenEvent(string InsectId, string DisplayName, int StarRating, Vector2 WorldPosition);
public record PhotoMissedEvent(Vector2 WorldPosition);

// -- Seeds --
public record SeedSelectedEvent(string PlantId);

// -- Zones --
public record ZoneChangedEvent(ZoneType From, ZoneType To);
public record ZoneUnlockedEvent(ZoneType Zone);
