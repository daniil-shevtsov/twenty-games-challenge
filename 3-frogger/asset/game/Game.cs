using Godot;
using System;
using System.Collections.Generic;
using System.Xml;

public partial class Game : Node2D
{
	private Player player;
	private Camera2D camera;
	private GameBounds bounds;

	private PackedScene tileScene;

	private Dictionary<Tuple<int, int>, Tile> tiles = new();
	private Vector2 tileSize;

	static readonly int horizontalCount = 14;
	static readonly int verticalCount = horizontalCount;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		camera = GetNode<Camera2D>("Camera2D");
		bounds = GetNode<GameBounds>("GameBounds");

		tileScene = GD.Load<PackedScene>("res://asset/tile/tile.tscn");
		var horizontalCount = 14;
		var verticalCount = 14;
		tileSize = new Vector2(
			bounds.shape.Size.X / horizontalCount,
			bounds.shape.Size.Y / verticalCount
		);
		InitTileGrid();
		InitPlayer();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.IsReleased())
		{
			var position = eventMouseButton.Position;

			var key = GetKeyForCoordinates(position);
			tiles[key].UpdateColor(Color.FromHtml("#FF0000"));
			GD.Print($"MOUSE {position} {key}");
		}
	}

	private void InitTileGrid()
	{
		var scene = GetTree().CurrentScene;

		for (int vertical = 0; vertical < verticalCount; vertical++)
		{
			for (int horizontal = 0; horizontal < horizontalCount; horizontal++)
			{
				Color color;
				if (horizontal % 2 == vertical % 2)
				{
					color = Color.FromHtml("#FFFFFF");
				}
				else
				{
					color = Color.FromHtml("#000000");
				}
				SpawnTile(scene, x: horizontal, y: vertical, tileSize, color);
			}
		}
	}


	private void InitPlayer()
	{
		player.Setup(tileSize);
		var bottomCenterCoordinates = new Vector2(bounds.shape.Size.X / 2, bounds.shape.Size.Y);
		var bottomCenterIndices = GetKeyForCoordinates(bottomCenterCoordinates);
	}

	private async void SpawnTile(Node scene, int x, int y, Vector2 tileSize, Color color)
	{
		var tile = (Tile)tileScene.Instantiate();
		scene.CallDeferred("add_child", tile);
		await ToSignal(GetTree(), "process_frame");
		tile.Setup(tileSize, color);
		var size = tile.shape.Size;

		tile.GlobalPosition = new Vector2(
			bounds.GlobalPosition.X - bounds.shape.Size.X / 2f + size.X / 2f + size.X * x,
			bounds.GlobalPosition.Y - bounds.shape.Size.Y / 2f + size.Y / 2f + size.Y * y
		);

		tiles[Tuple.Create(x, y)] = tile;
	}

	private Tuple<int, int> GetKeyForCoordinates(Vector2 coordinates)
	{
		int x = (int)(coordinates.X / (tileSize.X));
		int y = (int)(coordinates.Y / (tileSize.Y));
		var tuple = Tuple.Create(x, y);
		GD.Print($"position {coordinates} tile size {tileSize} tuple {tuple}");

		return tuple;
	}
}
