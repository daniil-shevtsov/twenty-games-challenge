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

	static readonly int horizontalCount = 15;
	static readonly int verticalCount = 14;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		camera = GetNode<Camera2D>("Camera2D");
		bounds = GetNode<GameBounds>("GameBounds");

		SetupEverything();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustReleased("up"))
		{
			var currentTile = GetKeyForCoordinates(player.GlobalPosition);
			var newTile = tiles[Tuple.Create(currentTile.Item1, currentTile.Item2 - 1)];
			player.GlobalPosition = newTile.GlobalPosition;
		}
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

	private async void SetupEverything()
	{
		camera.GlobalPosition = bounds.GlobalPosition;

		tileScene = GD.Load<PackedScene>("res://asset/tile/tile.tscn");
		tileSize = new Vector2(
			bounds.shape.Size.X / horizontalCount,
			bounds.shape.Size.Y / verticalCount
		);
		InitTileGrid();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		InitPlayer();
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
		var bottomCenterCoordinates = new Vector2(bounds.GlobalPosition.X, bounds.GlobalPosition.Y + bounds.shape.Size.Y / 2f);
		var bottomCenterKey = GetKeyForCoordinates(bottomCenterCoordinates);
		player.GlobalPosition = tiles[bottomCenterKey].GlobalPosition;
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
		var epsilon = 0.0001f;
		int x = (int)(Mathf.Clamp((coordinates.X - epsilon) / tileSize.X, 0, horizontalCount - 1));
		int y = (int)(Mathf.Clamp((coordinates.Y - epsilon) / tileSize.Y, 0, verticalCount - 1));
		var tuple = Tuple.Create(x, y);
		GD.Print($"position {coordinates} tile size {tileSize} tuple {tuple}");

		return tuple;
	}
}
