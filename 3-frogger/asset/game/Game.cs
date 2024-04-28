using Godot;
using System;
using System.Xml;

public partial class Game : Node2D
{
	private Player player;
	private Camera2D camera;
	private GameBounds bounds;

	private PackedScene tileScene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		camera = GetNode<Camera2D>("Camera2D");
		bounds = GetNode<GameBounds>("GameBounds");

		tileScene = GD.Load<PackedScene>("res://asset/tile/tile.tscn");

		var horizontalCount = 14;
		var verticalCount = 14;
		var scene = GetTree().CurrentScene;

		var tileSize = new Vector2(
			bounds.shape.Size.X / horizontalCount,
			bounds.shape.Size.Y / verticalCount
		);
		GD.Print($"KEK {bounds.shape.Size} {tileSize}");

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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
	}
}
