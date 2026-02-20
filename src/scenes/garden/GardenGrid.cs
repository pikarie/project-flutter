using System;
using Godot;
using System.Collections.Generic;
using ProjectFlutter;

public partial class GardenGrid : Node2D
{
	[Export] public Vector2I GridSize { get; set; } = new(4, 4);
	[Export] public int TileSize { get; set; } = 128;
	[Export] public int[] WaterTileData { get; set; } = System.Array.Empty<int>();
	[Export] public int[] LogTileData { get; set; } = System.Array.Empty<int>();
	[Export] public int[] StoneTileData { get; set; } = System.Array.Empty<int>();
	[Export] public int[] UVLampTileData { get; set; } = System.Array.Empty<int>();
	[Export] public ZoneType ZoneType { get; set; }

	private TileMapLayer _groundLayer;
	private Dictionary<Vector2I, CellState> _cells = new();
	private Vector2I? _hoveredCell;
	private string _selectedPlantId;
	private bool _expansionConfirmPending;
	private bool _hasWaterTiles;
	private float _waterAnimationTime;

	private static readonly Color SoilTilledColor = new(0.45f, 0.28f, 0.12f);
	private static readonly Color SoilWateredColor = new(0.35f, 0.22f, 0.10f);
	private static readonly Color HoverValidColor = new(0.5f, 1f, 0.5f, 0.3f);
	private static readonly Color HoverInvalidColor = new(1f, 0.5f, 0.5f, 0.3f);
	private static readonly Color GridLineColor = new(0.3f, 0.2f, 0.1f, 0.4f);
	private static readonly Color GrassColor = new(0.35f, 0.55f, 0.2f);
	private static readonly Color WaterTileColor = new(0.2f, 0.45f, 0.7f);
	private static readonly Color WaterTileDeepColor = new(0.15f, 0.35f, 0.6f);
	private static readonly Color SprinklerBodyColor = new(0.5f, 0.6f, 0.7f);
	private static readonly Color SprinklerRangeColor = new(0.3f, 0.6f, 0.9f, 0.12f);
	private static readonly Color SprinklerRangeBorderColor = new(0.3f, 0.6f, 0.9f, 0.3f);

	// Plant stage placeholder colors
	private static readonly Color SeedColor = new(0.6f, 0.45f, 0.2f);
	private static readonly Color SproutColor = new(0.4f, 0.7f, 0.3f);
	private static readonly Color GrowingColor = new(0.2f, 0.65f, 0.15f);
	private static readonly Color WaterDropColor = new(0.3f, 0.6f, 0.9f);

	// UV Lamp + Mist colors (Tropical)
	private static readonly Color UVLampGlowColor = new(0.6f, 0.3f, 0.9f);
	private static readonly Color UVLampBodyColor = new(0.4f, 0.4f, 0.5f);
	private static readonly Color MistColor = new(0.8f, 0.9f, 0.85f, 0.15f);

	// Heated stone colors (Rock Garden)
	private static readonly Color StoneCoolColor = new(0.55f, 0.55f, 0.53f);
	private static readonly Color StoneWarmColor = new(0.75f, 0.5f, 0.25f);
	private static readonly Color StoneLichenColor = new(0.6f, 0.65f, 0.3f);

	// Log decomposition colors (Deep Wood)
	private static readonly Color LogFreshColor = new(0.55f, 0.35f, 0.15f);
	private static readonly Color LogMoldyColor = new(0.35f, 0.38f, 0.18f);
	private static readonly Color LogRottenColor = new(0.22f, 0.20f, 0.12f);
	private static readonly Color LogMossAccent = new(0.3f, 0.5f, 0.2f);

	// Expansion preview colors
	private static readonly Color ExpansionPreviewColor = new(0.25f, 0.35f, 0.15f, 0.5f);
	private static readonly Color ExpansionBorderColor = new(0.5f, 0.6f, 0.3f, 0.6f);
	private static readonly Color ExpansionConfirmColor = new(0.4f, 0.55f, 0.25f, 0.7f);
	private static readonly Color ExpansionTextColor = new(1f, 0.9f, 0.6f);

	private Action<SeedSelectedEvent> _onSeedSelected;
	private Action<ZoneExpandedEvent> _onZoneExpanded;

	public override void _Ready()
	{
		_groundLayer = GetNode<TileMapLayer>("GroundLayer");

		// Center the grid around origin so camera at (0,0) sees it centered
		Position = new Vector2(
			-GridSize.X * TileSize / 2.0f,
			-GridSize.Y * TileSize / 2.0f
		);

		for (int x = 0; x < GridSize.X; x++)
		{
			for (int y = 0; y < GridSize.Y; y++)
			{
				var pos = new Vector2I(x, y);
				_cells[pos] = new CellState { CurrentState = CellState.State.Tilled };
			}
		}

		for (int i = 0; i + 1 < WaterTileData.Length; i += 2)
		{
			var waterPos = new Vector2I(WaterTileData[i], WaterTileData[i + 1]);
			if (_cells.TryGetValue(waterPos, out var waterCell))
			{
				waterCell.IsWater = true;
				waterCell.CurrentState = CellState.State.Empty;
			}
		}
		_hasWaterTiles = WaterTileData.Length > 0;

		for (int i = 0; i + 1 < LogTileData.Length; i += 2)
		{
			var logPos = new Vector2I(LogTileData[i], LogTileData[i + 1]);
			if (_cells.TryGetValue(logPos, out var logCell))
			{
				logCell.HasLog = true;
				logCell.DecompositionStage = 0;
				logCell.CurrentState = CellState.State.Empty;
			}
		}

		for (int i = 0; i + 1 < StoneTileData.Length; i += 2)
		{
			var stonePos = new Vector2I(StoneTileData[i], StoneTileData[i + 1]);
			if (_cells.TryGetValue(stonePos, out var stoneCell))
			{
				stoneCell.HasHeatedStone = true;
				stoneCell.CurrentState = CellState.State.Empty;
			}
		}

		for (int i = 0; i + 1 < UVLampTileData.Length; i += 2)
		{
			var uvPos = new Vector2I(UVLampTileData[i], UVLampTileData[i + 1]);
			if (_cells.TryGetValue(uvPos, out var uvCell))
			{
				uvCell.HasUVLamp = true;
				uvCell.CurrentState = CellState.State.Empty;
			}
		}

		_onSeedSelected = OnSeedSelected;
		_onZoneExpanded = OnZoneExpanded;
		EventBus.Subscribe(_onSeedSelected);
		EventBus.Subscribe(_onZoneExpanded);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onSeedSelected);
		EventBus.Unsubscribe(_onZoneExpanded);
	}

	private void OnZoneExpanded(ZoneExpandedEvent expansionEvent)
	{
		if (expansionEvent.Zone != ZoneType) return;
		var newSize = ZoneManager.Instance.GetCurrentGridSize(ZoneType);
		ExpandGrid(newSize);
	}

	public void ExpandGrid(Vector2I newGridSize)
	{
		int offsetX = (newGridSize.X - GridSize.X) / 2;
		int offsetY = (newGridSize.Y - GridSize.Y) / 2;

		// Remap existing cells to new coordinates
		var newCells = new Dictionary<Vector2I, CellState>();
		foreach (var (oldPosition, cellState) in _cells)
		{
			var newPosition = new Vector2I(oldPosition.X + offsetX, oldPosition.Y + offsetY);
			newCells[newPosition] = cellState;
		}

		// Fill new cells as Tilled
		for (int x = 0; x < newGridSize.X; x++)
		{
			for (int y = 0; y < newGridSize.Y; y++)
			{
				var position = new Vector2I(x, y);
				if (!newCells.ContainsKey(position))
					newCells[position] = new CellState { CurrentState = CellState.State.Tilled };
			}
		}

		_cells = newCells;
		GridSize = newGridSize;
		_expansionConfirmPending = false;

		// Recenter the grid
		Position = new Vector2(
			-GridSize.X * TileSize / 2.0f,
			-GridSize.Y * TileSize / 2.0f
		);

		QueueRedraw();
		GD.Print($"Grid expanded to {GridSize.X}×{GridSize.Y}");
	}

	private void OnSeedSelected(SeedSelectedEvent seedEvent)
	{
		_selectedPlantId = seedEvent.PlantId;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
		{
			if (_hoveredCell != null)
			{
				_hoveredCell = null;
				QueueRedraw();
			}
			return;
		}

		// Auto-water cells in sprinkler range
		ApplySprinklerWatering();

		var newHover = MouseToGrid();
		if (newHover != _hoveredCell)
		{
			_hoveredCell = newHover;
			QueueRedraw();
		}

		// Animate water tiles
		if (_hasWaterTiles)
		{
			_waterAnimationTime += (float)delta;
			QueueRedraw();
		}

		// Advance log decomposition timers
		AdvanceDecomposition((float)delta);

		// Update heated stone temperatures
		UpdateStoneHeat((float)delta);

		// Redraw for expansion preview hover effect
		if (MouseToExpansionPreview() != null)
			QueueRedraw();
	}

	private void ApplySprinklerWatering()
	{
		bool changed = false;
		foreach (var (position, cell) in _cells)
		{
			if (!cell.HasSprinkler) continue;
			int radius = GetSprinklerRadius(cell.SprinklerTier);
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					var target = new Vector2I(position.X + dx, position.Y + dy);
					if (_cells.TryGetValue(target, out var targetCell)
						&& !targetCell.IsWater
						&& !targetCell.HasSprinkler
						&& !targetCell.IsWatered)
					{
						targetCell.IsWatered = true;
						changed = true;
					}
				}
			}
		}
		if (changed) QueueRedraw();
	}

	public static int GetSprinklerRadius(int tier)
	{
		return tier switch { 1 => 1, 2 => 2, 3 => 3, _ => 1 };
	}

	private void AdvanceDecomposition(float delta)
	{
		// Decomposition time per stage: ~3 in-game minutes (180 game-seconds)
		const float decompositionDuration = 180f;
		float gameDelta = delta * TimeManager.Instance.SpeedMultiplier;
		bool changed = false;
		foreach (var (_, cell) in _cells)
		{
			if (!cell.HasLog || cell.DecompositionStage >= 2) continue;
			cell.DecompositionTimer += gameDelta;
			if (cell.DecompositionTimer >= decompositionDuration)
			{
				cell.DecompositionTimer -= decompositionDuration;
				cell.DecompositionStage++;
				changed = true;
				GD.Print($"Log decomposed to stage {cell.DecompositionStage}");
			}
		}
		if (changed) QueueRedraw();
	}

	private void UpdateStoneHeat(float delta)
	{
		float gameDelta = delta * TimeManager.Instance.SpeedMultiplier;
		// Heat rate: full heat in ~120 game-seconds, cool in ~180 game-seconds
		float heatRate = gameDelta / 120f;
		float coolRate = gameDelta / 180f;
		bool isDaytime = TimeManager.Instance.IsDaytime();
		foreach (var (_, cell) in _cells)
		{
			if (!cell.HasHeatedStone) continue;
			if (isDaytime)
				cell.StoneHeat = Mathf.Min(1.0f, cell.StoneHeat + heatRate);
			else
				cell.StoneHeat = Mathf.Max(0.0f, cell.StoneHeat - coolRate);
		}
	}

	public override void _Draw()
	{
		// Draw grass background — large enough to fill screen at any zoom/resolution
		int margin = 3072;
		var bgRect = new Rect2(
			-margin, -margin,
			GridSize.X * TileSize + margin * 2,
			GridSize.Y * TileSize + margin * 2
		);
		DrawRect(bgRect, GrassColor);

		// Draw soil tiles and plants
		for (int x = 0; x < GridSize.X; x++)
		{
			for (int y = 0; y < GridSize.Y; y++)
			{
				var rect = new Rect2(x * TileSize, y * TileSize, TileSize, TileSize);
				var cell = _cells[new Vector2I(x, y)];

				if (cell.IsWater)
				{
					DrawRect(rect, WaterTileColor);
					float cx = x * TileSize + TileSize / 2.0f;
					float cy = y * TileSize + TileSize / 2.0f;
					// Animated sine-wave lines (phase offset per tile for variety)
					float phase = x * 1.2f + y * 0.8f;
					float wave1 = Mathf.Sin(_waterAnimationTime * 2.0f + phase) * 4f;
					float wave2 = Mathf.Sin(_waterAnimationTime * 1.5f + phase + 2f) * 3f;
					DrawLine(new Vector2(cx - 20, cy - 4 + wave1), new Vector2(cx + 20, cy - 4 + wave1), WaterTileDeepColor, 2);
					DrawLine(new Vector2(cx - 14, cy + 8 + wave2), new Vector2(cx + 14, cy + 8 + wave2), WaterTileDeepColor, 1.5f);
					// Ripple circles
					float rippleRadius = 3f + Mathf.Sin(_waterAnimationTime * 3f + phase * 0.5f) * 2f;
					DrawArc(new Vector2(cx + 10, cy - 10), rippleRadius, 0, Mathf.Tau, 12, WaterTileDeepColor, 1f);
				}
				else if (cell.HasLog)
				{
					DrawRect(rect, SoilTilledColor.Darkened(0.2f));
					DrawLogPlaceholder(x, y, cell);
				}
				else if (cell.HasHeatedStone)
				{
					DrawRect(rect, SoilTilledColor);
					DrawStonePlaceholder(x, y, cell);
				}
				else if (cell.HasUVLamp)
				{
					DrawRect(rect, SoilTilledColor);
					DrawUVLampPlaceholder(x, y);
				}
				else
				{
					Color soilColor = cell.IsWatered ? SoilWateredColor : SoilTilledColor;
					DrawRect(rect, soilColor);
					DrawPlantPlaceholder(x, y, cell);
				}
				DrawRect(rect, GridLineColor, false, 1.0f);
			}
		}

		// Draw sprinkler range overlays and bodies
		foreach (var (position, cell) in _cells)
		{
			if (!cell.HasSprinkler) continue;
			int radius = GetSprinklerRadius(cell.SprinklerTier);
			// Range overlay
			var rangeRect = new Rect2(
				(position.X - radius) * TileSize,
				(position.Y - radius) * TileSize,
				(radius * 2 + 1) * TileSize,
				(radius * 2 + 1) * TileSize
			);
			DrawRect(rangeRect, SprinklerRangeColor);
			DrawRect(rangeRect, SprinklerRangeBorderColor, false, 1.5f);

			// Sprinkler body: metallic circle with tier indicator
			float cx = position.X * TileSize + TileSize / 2.0f;
			float cy = position.Y * TileSize + TileSize / 2.0f;
			DrawCircle(new Vector2(cx, cy), 14, SprinklerBodyColor);
			DrawCircle(new Vector2(cx, cy), 8, WaterDropColor);
			// Tier dots
			for (int i = 0; i < cell.SprinklerTier; i++)
			{
				float dotX = cx - (cell.SprinklerTier - 1) * 5 + i * 10;
				DrawCircle(new Vector2(dotX, cy + 20), 3, Colors.White);
			}
		}

		// Draw hover highlight
		if (_hoveredCell is Vector2I hovered)
		{
			var hoverRect = new Rect2(hovered.X * TileSize, hovered.Y * TileSize, TileSize, TileSize);
			var cell = _cells.GetValueOrDefault(hovered);
			bool canAct = CanActOnCell(cell);
			DrawRect(hoverRect, canAct ? HoverValidColor : HoverInvalidColor);

			// Seed preview on tilled cells when seed is selected
			if (canAct && cell.CurrentState == CellState.State.Tilled && _selectedPlantId != null)
			{
				var plantData = PlantRegistry.GetById(_selectedPlantId);
				if (plantData != null)
				{
					float cx = hovered.X * TileSize + TileSize / 2.0f;
					float cy = hovered.Y * TileSize + TileSize / 2.0f;
					var previewColor = plantData.DrawColor with { A = 0.4f };
					DrawCircle(new Vector2(cx, cy), 10, previewColor);
				}
			}
		}

		// Draw tropical mist overlay
		if (ZoneType == ZoneType.Tropical)
			DrawTropicalMist();

		// Draw expansion preview
		DrawExpansionPreview();
	}

	private void DrawExpansionPreview()
	{
		int currentTier = ZoneManager.Instance.GetExpansionTier(ZoneType);
		int maxTier = ExpansionConfig.MaxExpansionTier(ZoneType);
		if (currentTier >= maxTier) return;

		var nextTierData = ExpansionConfig.GetTierData(ZoneType, currentTier + 1);
		var nextSize = new Vector2I(nextTierData.GridWidth, nextTierData.GridHeight);

		int offsetX = (nextSize.X - GridSize.X) / 2;
		int offsetY = (nextSize.Y - GridSize.Y) / 2;

		Color fillColor = _expansionConfirmPending ? ExpansionConfirmColor : ExpansionPreviewColor;
		bool isHoveringPreview = MouseToExpansionPreview() != null;

		// Draw ring/band cells (cells in next size that are NOT in current size)
		for (int x = -offsetX; x < GridSize.X + offsetX; x++)
		{
			for (int y = -offsetY; y < GridSize.Y + offsetY; y++)
			{
				bool isExistingCell = x >= 0 && x < GridSize.X && y >= 0 && y < GridSize.Y;
				if (isExistingCell) continue;

				var rect = new Rect2(x * TileSize, y * TileSize, TileSize, TileSize);
				Color cellColor = isHoveringPreview ? fillColor.Lightened(0.15f) : fillColor;
				DrawRect(rect, cellColor);
				DrawRect(rect, ExpansionBorderColor, false, 1.0f);
			}
		}

		// Draw label centered on expansion area — hover only (or confirm state)
		if (!isHoveringPreview && !_expansionConfirmPending) return;

		bool canAfford = ZoneManager.Instance.CanExpand(ZoneType);

		string costText;
		string secondLine = null;
		if (_expansionConfirmPending)
		{
			costText = $"Click to confirm: {nextTierData.Name}";
			secondLine = "Right-click to cancel";
		}
		else if (canAfford)
		{
			costText = $"{nextTierData.Name} — {nextTierData.NectarCost} nectar";
		}
		else
		{
			costText = $"{nextTierData.Name} — {nextTierData.NectarCost} nectar (not enough!)";
		}

		// Position text on the expansion preview band (not on the existing grid)
		float areaCenterX;
		float areaCenterY;
		if (offsetY > 0)
		{
			// Top band exists (rings or vertical growth) — center text in top band
			areaCenterX = (GridSize.X * TileSize) / 2.0f;
			areaCenterY = -offsetY * TileSize / 2.0f;
		}
		else
		{
			// Side bands only (lateral horizontal) — center text in right band
			areaCenterX = GridSize.X * TileSize + offsetX * TileSize / 2.0f;
			areaCenterY = (GridSize.Y * TileSize) / 2.0f;
		}

		var font = ThemeDB.FallbackFont;
		int fontSize = 14;
		var textSize = font.GetStringSize(costText, HorizontalAlignment.Center, -1, fontSize);
		float totalHeight = textSize.Y + 6;
		if (secondLine != null)
			totalHeight += fontSize + 4;
		var textPosition = new Vector2(areaCenterX - textSize.X / 2, areaCenterY + textSize.Y / 2);
		var textBgRect = new Rect2(textPosition.X - 6, textPosition.Y - textSize.Y - 2, textSize.X + 12, totalHeight);
		DrawRect(textBgRect, new Color(0, 0, 0, 0.6f));
		Color mainTextColor = canAfford || _expansionConfirmPending ? ExpansionTextColor : new Color(1f, 0.5f, 0.5f);
		DrawString(font, textPosition, costText, HorizontalAlignment.Left, -1, fontSize, mainTextColor);
		if (secondLine != null)
		{
			var secondSize = font.GetStringSize(secondLine, HorizontalAlignment.Center, -1, fontSize);
			var secondPosition = new Vector2(areaCenterX - secondSize.X / 2, textPosition.Y + fontSize + 4);
			DrawString(font, secondPosition, secondLine, HorizontalAlignment.Left, -1, fontSize, new Color(0.8f, 0.8f, 0.8f));
		}
	}

	private bool CanActOnCell(CellState cell)
	{
		if (cell == null || cell.IsWater || cell.HasSprinkler) return false;
		return cell.CurrentState switch
		{
			CellState.State.Tilled => _selectedPlantId != null,
			CellState.State.Planted or CellState.State.Growing => true,
			CellState.State.Blooming => true,
			_ => false
		};
	}

	private void DrawPlantPlaceholder(int x, int y, CellState cell)
	{
		float cx = x * TileSize + TileSize / 2.0f;
		float cy = y * TileSize + TileSize / 2.0f;

		// Look up plant-specific color for bloom
		var plantData = !string.IsNullOrEmpty(cell.PlantType)
			? PlantRegistry.GetById(cell.PlantType)
			: null;
		Color bloomColor = plantData?.DrawColor ?? new Color(0.9f, 0.4f, 0.6f);

		switch (cell.CurrentState)
		{
			case CellState.State.Planted:
				// Seed: small circle tinted by plant color
				var seedTint = plantData != null
					? bloomColor.Darkened(0.4f)
					: SeedColor;
				DrawCircle(new Vector2(cx, cy + 8), 6, seedTint);
				break;

			case CellState.State.Growing:
				// Sprout: stem + small leaves
				DrawLine(new Vector2(cx, cy + 14), new Vector2(cx, cy - 4), GrowingColor, 3);
				DrawCircle(new Vector2(cx - 6, cy - 2), 5, SproutColor);
				DrawCircle(new Vector2(cx + 6, cy - 2), 5, SproutColor);
				break;

			case CellState.State.Blooming:
				// Bloom: stem + flower with plant-specific color
				DrawLine(new Vector2(cx, cy + 14), new Vector2(cx, cy - 10), GrowingColor, 3);
				DrawCircle(new Vector2(cx - 8, cy), 5, GrowingColor);
				DrawCircle(new Vector2(cx + 8, cy), 5, GrowingColor);
				DrawCircle(new Vector2(cx, cy - 14), 10, bloomColor);
				DrawCircle(new Vector2(cx, cy - 14), 5, bloomColor.Lightened(0.4f));
				break;
		}

		// Water drop indicator
		if (cell.IsWatered && cell.CurrentState is CellState.State.Planted or CellState.State.Growing)
		{
			DrawCircle(new Vector2(x * TileSize + 10, y * TileSize + 10), 4, WaterDropColor);
		}
	}

	private void DrawLogPlaceholder(int x, int y, CellState cell)
	{
		float cx = x * TileSize + TileSize / 2.0f;
		float cy = y * TileSize + TileSize / 2.0f;

		Color logColor = cell.DecompositionStage switch
		{
			0 => LogFreshColor,
			1 => LogMoldyColor,
			_ => LogRottenColor,
		};

		// Log body: horizontal rounded rectangle
		DrawRect(new Rect2(cx - 28, cy - 10, 56, 20), logColor);
		// Wood rings (ends)
		DrawCircle(new Vector2(cx - 26, cy), 10, logColor.Darkened(0.15f));
		DrawCircle(new Vector2(cx + 26, cy), 10, logColor.Darkened(0.15f));
		// Rings inside
		DrawArc(new Vector2(cx - 26, cy), 5, 0, Mathf.Tau, 8, logColor.Darkened(0.3f), 1f);
		DrawArc(new Vector2(cx + 26, cy), 5, 0, Mathf.Tau, 8, logColor.Darkened(0.3f), 1f);

		// Moss accents for moldy and rotten stages
		if (cell.DecompositionStage >= 1)
		{
			DrawCircle(new Vector2(cx - 12, cy - 8), 4, LogMossAccent);
			DrawCircle(new Vector2(cx + 8, cy + 6), 3, LogMossAccent);
		}
		if (cell.DecompositionStage >= 2)
		{
			DrawCircle(new Vector2(cx + 18, cy - 6), 3, LogMossAccent.Darkened(0.2f));
			DrawCircle(new Vector2(cx - 5, cy + 8), 5, LogMossAccent);
		}

		// Stage indicator dots at bottom
		for (int i = 0; i <= cell.DecompositionStage; i++)
		{
			float dotX = cx - (cell.DecompositionStage) * 5 + i * 10;
			DrawCircle(new Vector2(dotX, cy + 22), 2.5f, Colors.White);
		}
	}

	private void DrawUVLampPlaceholder(int x, int y)
	{
		float cx = x * TileSize + TileSize / 2.0f;
		float cy = y * TileSize + TileSize / 2.0f;

		// Lamp body: metallic cylinder
		DrawRect(new Rect2(cx - 8, cy - 16, 16, 24), UVLampBodyColor);
		// UV glow: purple circle on top
		DrawCircle(new Vector2(cx, cy - 18), 12, UVLampGlowColor);
		DrawCircle(new Vector2(cx, cy - 18), 7, UVLampGlowColor.Lightened(0.3f));
		// Glow aura (pulsing)
		float pulse = 0.3f + Mathf.Sin(_waterAnimationTime * 2.5f) * 0.15f;
		DrawCircle(new Vector2(cx, cy - 18), 20, new Color(UVLampGlowColor.R, UVLampGlowColor.G, UVLampGlowColor.B, pulse));
		// Base
		DrawRect(new Rect2(cx - 10, cy + 6, 20, 6), UVLampBodyColor.Darkened(0.2f));
	}

	private void DrawTropicalMist()
	{
		// Animated mist: semi-transparent patches drifting across the zone
		for (int i = 0; i < 5; i++)
		{
			float phaseX = i * 1.7f;
			float phaseY = i * 2.3f;
			float driftX = Mathf.Sin(_waterAnimationTime * 0.3f + phaseX) * GridSize.X * TileSize * 0.3f;
			float driftY = Mathf.Sin(_waterAnimationTime * 0.2f + phaseY) * GridSize.Y * TileSize * 0.2f;
			float centerX = GridSize.X * TileSize * (0.2f + i * 0.15f) + driftX;
			float centerY = GridSize.Y * TileSize * (0.15f + i * 0.18f) + driftY;
			float alpha = 0.08f + Mathf.Sin(_waterAnimationTime * 0.5f + i) * 0.04f;
			var mistPatchColor = new Color(MistColor.R, MistColor.G, MistColor.B, alpha);
			DrawCircle(new Vector2(centerX, centerY), 80 + i * 20, mistPatchColor);
		}
	}

	private void DrawStonePlaceholder(int x, int y, CellState cell)
	{
		float cx = x * TileSize + TileSize / 2.0f;
		float cy = y * TileSize + TileSize / 2.0f;

		// Lerp color from cool grey to warm orange based on heat
		Color stoneColor = StoneCoolColor.Lerp(StoneWarmColor, cell.StoneHeat);

		// Main stone: large rounded shape (two overlapping circles)
		DrawCircle(new Vector2(cx - 6, cy + 2), 22, stoneColor);
		DrawCircle(new Vector2(cx + 8, cy - 4), 18, stoneColor.Lightened(0.05f));
		// Stone edge highlight
		DrawArc(new Vector2(cx, cy), 20, 0.3f, 2.5f, 10, stoneColor.Darkened(0.2f), 2f);

		// Lichen patches
		DrawCircle(new Vector2(cx - 14, cy - 12), 4, StoneLichenColor);
		DrawCircle(new Vector2(cx + 12, cy + 8), 3, StoneLichenColor);

		// Heat shimmer indicator when hot
		if (cell.StoneHeat > 0.5f)
		{
			float shimmerAlpha = (cell.StoneHeat - 0.5f) * 0.6f;
			var shimmerColor = new Color(1f, 0.7f, 0.3f, shimmerAlpha);
			DrawCircle(new Vector2(cx, cy - 20), 3, shimmerColor);
			DrawCircle(new Vector2(cx + 10, cy - 18), 2, shimmerColor);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

		// Expansion purchase interaction
		if (@event.IsActionPressed("primary_action") && MouseToExpansionPreview() != null)
		{
			if (_expansionConfirmPending)
			{
				ZoneManager.Instance.TryExpand(ZoneType);
				GetViewport().SetInputAsHandled();
			}
			else if (ZoneManager.Instance.CanExpand(ZoneType))
			{
				_expansionConfirmPending = true;
				QueueRedraw();
				GetViewport().SetInputAsHandled();
			}
			else
			{
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		// Cancel expansion confirm
		if (_expansionConfirmPending)
		{
			if (@event.IsActionPressed("cancel_action") || @event.IsActionPressed("primary_action"))
			{
				_expansionConfirmPending = false;
				QueueRedraw();
				if (@event.IsActionPressed("cancel_action"))
				{
					GetViewport().SetInputAsHandled();
					return;
				}
				// primary_action on non-preview area — fall through to normal handling
			}
		}

		// Remove plant — rebindable (X / Delete by default)
		if (@event.IsActionPressed("remove_plant"))
		{
			var removePos = MouseToGrid();
			if (removePos is Vector2I pos)
			{
				RemovePlant(pos);
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		// Sprinkler placing mode
		if (ShopUI.Instance != null && ShopUI.Instance.IsPlacingSprinkler)
		{
			if (@event.IsActionPressed("primary_action"))
			{
				var gridPos = MouseToGrid();
				if (gridPos is not Vector2I sprinklerPos) return;
				if (PlaceSprinkler(sprinklerPos, ShopUI.Instance.SelectedSprinklerTier))
				{
					ShopUI.Instance.ExitPlacingMode();
					GD.Print($"Sprinkler placed at {sprinklerPos}");
				}
				else
				{
					GD.Print($"Cannot place sprinkler at {sprinklerPos}");
				}
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("cancel_action"))
			{
				// Cancel placing — refund nectar
				int tier = ShopUI.Instance.SelectedSprinklerTier;
				GameManager.Instance.AddNectar(ShopUI.SprinklerCosts[tier]);
				ShopUI.Instance.ExitPlacingMode();
				GD.Print("Sprinkler placement cancelled — nectar refunded");
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		// Cancel / deselect seed — rebindable (Right-click by default)
		if (@event.IsActionPressed("cancel_action"))
		{
			if (_selectedPlantId != null)
			{
				_selectedPlantId = null;
				EventBus.Publish(new SeedSelectedEvent(null));
				GD.Print("Seed deselected");
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		// Primary action — rebindable (Left-click by default)
		if (@event.IsActionPressed("primary_action"))
		{
			var gridPos = MouseToGrid();
			if (gridPos is not Vector2I pos) return;
			HandlePrimaryAction(pos);
		}
	}

	private void HandlePrimaryAction(Vector2I pos)
	{
		var cell = _cells[pos];

		switch (cell.CurrentState)
		{
			case CellState.State.Tilled:
				if (_selectedPlantId == null)
				{
					GD.Print("Select a seed from the toolbar first (keys 1-9)");
					return;
				}
				var plantData = PlantRegistry.GetById(_selectedPlantId);
				if (plantData == null) return;
				if (!GameManager.Instance.SpendNectar(plantData.SeedCost))
				{
					GD.Print($"Not enough nectar! Need {plantData.SeedCost}, have {GameManager.Instance.Nectar}");
					return;
				}
				cell.CurrentState = CellState.State.Planted;
				cell.PlantType = plantData.Id;
				cell.MaxInsectSlots = plantData.InsectSlots;
				EventBus.Publish(new PlantPlantedEvent(cell.PlantType, pos));
				GD.Print($"Planted {plantData.DisplayName} at {pos} (cost {plantData.SeedCost} nectar)");
				break;

			case CellState.State.Planted:
			case CellState.State.Growing:
				if (!cell.IsWatered)
				{
					cell.IsWatered = true;
					GD.Print($"Watered at {pos}");
				}
				else
				{
					cell.IsWatered = false;
					cell.CurrentState = cell.CurrentState == CellState.State.Planted
						? CellState.State.Growing
						: CellState.State.Blooming;
					if (cell.CurrentState == CellState.State.Blooming)
						EventBus.Publish(new PlantBloomingEvent(pos));
					GD.Print($"Grew to {cell.CurrentState} at {pos}");
				}
				break;

			case CellState.State.Blooming:
				var harvestedPlant = PlantRegistry.GetById(cell.PlantType);
				int baseYield = harvestedPlant?.NectarYield ?? 3;
				float multiplier = PlantLevelManager.Instance.GetNectarMultiplier(cell.PlantType);
				int nectarYield = Mathf.RoundToInt(baseYield * multiplier);
				GameManager.Instance.AddNectar(nectarYield);
				cell.CurrentState = CellState.State.Growing;
				cell.IsWatered = false;
				var worldPosition = ToGlobal(GridToWorld(pos));
				EventBus.Publish(new PlantHarvestedEvent(cell.PlantType, pos, nectarYield, worldPosition));
				int level = PlantLevelManager.Instance.GetLevel(cell.PlantType);
				string levelTag = level > 1 ? $" [Lv{level} x{multiplier:F2}]" : "";
				GD.Print($"Harvested {harvestedPlant?.DisplayName ?? cell.PlantType} at {pos}, +{nectarYield} nectar{levelTag} (total: {GameManager.Instance.Nectar})");
				break;
		}
		QueueRedraw();
	}

	private void RemovePlant(Vector2I pos)
	{
		if (!_cells.TryGetValue(pos, out var cell)) return;
		if (cell.IsWater) return;

		// Remove sprinkler
		if (cell.HasSprinkler)
		{
			cell.HasSprinkler = false;
			cell.SprinklerTier = 0;
			cell.CurrentState = CellState.State.Tilled;
			cell.MaxInsectSlots = 2;
			GD.Print($"Removed sprinkler at {pos}");
			QueueRedraw();
			return;
		}

		if (cell.CurrentState == CellState.State.Tilled) return;

		cell.CurrentState = CellState.State.Tilled;
		cell.PlantType = "";
		cell.GrowthStage = 0;
		cell.IsWatered = false;
		cell.MaxInsectSlots = 2;
		cell.PlantNode?.QueueFree();
		cell.PlantNode = null;
		cell.ClearSlots();
		EventBus.Publish(new PlantRemovedEvent(pos));
		GD.Print($"Removed plant at {pos}");
		QueueRedraw();
	}

	private Vector2I? MouseToGrid()
	{
		var mousePos = GetGlobalMousePosition();
		var localPos = ToLocal(mousePos);
		var gridPos = new Vector2I(
			(int)(localPos.X / TileSize),
			(int)(localPos.Y / TileSize)
		);

		if (localPos.X >= 0 && localPos.Y >= 0
			&& gridPos.X >= 0 && gridPos.X < GridSize.X
			&& gridPos.Y >= 0 && gridPos.Y < GridSize.Y)
			return gridPos;

		return null;
	}

	private Vector2I? MouseToExpansionPreview()
	{
		int currentTier = ZoneManager.Instance.GetExpansionTier(ZoneType);
		if (currentTier >= ExpansionConfig.MaxExpansionTier(ZoneType)) return null;

		var nextTierData = ExpansionConfig.GetTierData(ZoneType, currentTier + 1);
		var nextSize = new Vector2I(nextTierData.GridWidth, nextTierData.GridHeight);

		int offsetX = (nextSize.X - GridSize.X) / 2;
		int offsetY = (nextSize.Y - GridSize.Y) / 2;

		var mousePos = GetGlobalMousePosition();
		var localPos = ToLocal(mousePos);
		var gridPos = new Vector2I(
			Mathf.FloorToInt(localPos.X / TileSize),
			Mathf.FloorToInt(localPos.Y / TileSize)
		);

		// Must be within the expanded bounds
		if (gridPos.X < -offsetX || gridPos.X >= GridSize.X + offsetX) return null;
		if (gridPos.Y < -offsetY || gridPos.Y >= GridSize.Y + offsetY) return null;

		// Must NOT be within the current grid
		bool isCurrentGrid = gridPos.X >= 0 && gridPos.X < GridSize.X
			&& gridPos.Y >= 0 && gridPos.Y < GridSize.Y;
		if (isCurrentGrid) return null;

		return gridPos;
	}

	public Vector2 GridToWorld(Vector2I gridPos)
	{
		return new Vector2(
			gridPos.X * TileSize + TileSize / 2.0f,
			gridPos.Y * TileSize + TileSize / 2.0f
		);
	}

	public bool PlaceSprinkler(Vector2I pos, int tier)
	{
		if (!_cells.TryGetValue(pos, out var cell)) return false;
		if (cell.IsWater || cell.HasSprinkler) return false;
		if (cell.CurrentState != CellState.State.Tilled) return false;
		cell.CurrentState = CellState.State.Empty;
		cell.IsWatered = false;
		cell.MaxInsectSlots = 0;
		cell.HasSprinkler = true;
		cell.SprinklerTier = tier;
		EventBus.Publish(new SprinklerPlacedEvent(pos, tier));
		QueueRedraw();
		return true;
	}

	public Dictionary<Vector2I, CellState> GetCells() => _cells;

	public CellState GetCell(Vector2I pos) => _cells.GetValueOrDefault(pos);

	public int CountWaterTiles()
	{
		int count = 0;
		foreach (var cell in _cells.Values)
			if (cell.IsWater) count++;
		return count;
	}
}
