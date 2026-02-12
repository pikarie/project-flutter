using System;
using Godot;
using System.Collections.Generic;
using ProjectFlutter;

public partial class GardenGrid : Node2D
{
	[Export] public Vector2I GridSize { get; set; } = new(4, 4);
	[Export] public int TileSize { get; set; } = 128;

	private TileMapLayer _groundLayer;
	private Dictionary<Vector2I, CellState> _cells = new();
	private Vector2I? _hoveredCell;
	private string _selectedPlantId;

	private static readonly Color SoilTilledColor = new(0.45f, 0.28f, 0.12f);
	private static readonly Color SoilWateredColor = new(0.35f, 0.22f, 0.10f);
	private static readonly Color HoverValidColor = new(0.5f, 1f, 0.5f, 0.3f);
	private static readonly Color HoverInvalidColor = new(1f, 0.5f, 0.5f, 0.3f);
	private static readonly Color GridLineColor = new(0.3f, 0.2f, 0.1f, 0.4f);
	private static readonly Color GrassColor = new(0.35f, 0.55f, 0.2f);

	// Plant stage placeholder colors
	private static readonly Color SeedColor = new(0.6f, 0.45f, 0.2f);
	private static readonly Color SproutColor = new(0.4f, 0.7f, 0.3f);
	private static readonly Color GrowingColor = new(0.2f, 0.65f, 0.15f);
	private static readonly Color WaterDropColor = new(0.3f, 0.6f, 0.9f);

	private Action<SeedSelectedEvent> _onSeedSelected;

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

		_onSeedSelected = OnSeedSelected;
		EventBus.Subscribe(_onSeedSelected);
	}

	public override void _ExitTree()
	{
		EventBus.Unsubscribe(_onSeedSelected);
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

		var newHover = MouseToGrid();
		if (newHover != _hoveredCell)
		{
			_hoveredCell = newHover;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		// Draw grass background
		int margin = TileSize * 2;
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

				// Soil color based on state
				Color soilColor = cell.IsWatered ? SoilWateredColor : SoilTilledColor;
				DrawRect(rect, soilColor);
				DrawRect(rect, GridLineColor, false, 1.0f);

				// Draw plant placeholder based on growth stage
				DrawPlantPlaceholder(x, y, cell);
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
	}

	private bool CanActOnCell(CellState cell)
	{
		if (cell == null) return false;
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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

		if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
		{
			var gridPos = MouseToGrid();
			if (gridPos is not Vector2I pos) return;

			switch (mouseBtn.ButtonIndex)
			{
				case MouseButton.Left:
					HandlePrimaryAction(pos);
					break;
				case MouseButton.Right:
					HandleSecondaryAction(pos);
					break;
			}
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
				int nectarYield = harvestedPlant?.NectarYield ?? 3;
				GameManager.Instance.AddNectar(nectarYield);
				cell.CurrentState = CellState.State.Growing;
				cell.IsWatered = false;
				EventBus.Publish(new PlantHarvestedEvent(cell.PlantType, pos));
				GD.Print($"Harvested {harvestedPlant?.DisplayName ?? cell.PlantType} at {pos}, +{nectarYield} nectar (total: {GameManager.Instance.Nectar})");
				break;
		}
		QueueRedraw();
	}

	private void HandleSecondaryAction(Vector2I pos)
	{
		var cell = _cells[pos];
		if (cell.CurrentState != CellState.State.Tilled)
		{
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

	public Vector2 GridToWorld(Vector2I gridPos)
	{
		return new Vector2(
			gridPos.X * TileSize + TileSize / 2.0f,
			gridPos.Y * TileSize + TileSize / 2.0f
		);
	}

	public Dictionary<Vector2I, CellState> GetCells() => _cells;

	public CellState GetCell(Vector2I pos) => _cells.GetValueOrDefault(pos);
}
