using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

public partial class Game : Node2D
{
	private Player player;
	private Camera2D camera;
	private GameBounds bounds;

	private PackedScene tileScene;

	private Dictionary<TileKey, Tile> tiles = new();

	private Tree tree;

	private Vector2 tileSize;

	static readonly int horizontalCount = 15;
	static readonly int verticalCount = 14;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		camera = GetNode<Camera2D>("Camera2D");
		bounds = GetNode<GameBounds>("GameBounds");
		tree = GetNode<Tree>("Tree");

		SetupEverything();
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
		tree.Setup(new Vector2(tileSize.X * 4, tileSize.Y));
		SpawnTree();
	}

	private void SpawnTree()
	{
		var bottomWaterTile = tiles.Values.Where(tile => tile.tileType == TileType.Water).MaxBy(tile => tile.GlobalPosition.Y);
		var treeInitialPosition = new Vector2(
			bounds.GlobalPosition.X + bounds.shape.Size.X / 2f + tree.shape.Size.X / 2f,
			bottomWaterTile.GlobalPosition.Y
		);
		if (bottomWaterTile != null)
		{
			tree.GlobalPosition = treeInitialPosition;
			GD.Print($"new tree position: ${tree.GlobalPosition}");
		}
	}

	private void InitTileGrid()
	{
		var scene = GetTree().CurrentScene;

		for (int vertical = 0; vertical < verticalCount; vertical++)
		{
			for (int horizontal = 0; horizontal < horizontalCount; horizontal++)
			{
				var type = TileType.Ground;
				if (vertical > 1 && vertical < 7)
				{
					type = TileType.Water;
				}
				else if (vertical == 1)
				{
					if (horizontal == 0 || horizontal == 7 || horizontal == horizontalCount - 1)
					{
						type = TileType.Ground;
					}
					else
					{
						if (horizontal == horizontalCount / 2)
						{
							type = TileType.Ground;
						}
						if ((horizontal % 6 == 1) || (horizontal % 6 == 2))
						{
							type = TileType.Water;
						}
						else if ((horizontal % 6 == 3) || (horizontal % 6 == 4))
						{
							type = TileType.Ground;
						}
						else if ((horizontal % 6 == 5) || (horizontal % 6 == 6))
						{
							type = TileType.Ground;
						}
						else
						{
							type = TileType.Water;
						}
					}
				}

				SpawnTile(scene, x: horizontal, y: vertical, tileSize, type);
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

	private async void SpawnTile(Node scene, int x, int y, Vector2 tileSize, TileType type)
	{
		var tile = (Tile)tileScene.Instantiate();
		scene.CallDeferred("add_child", tile);
		await ToSignal(GetTree(), "process_frame");
		var tileKey = new TileKey(x, y);
		tile.Setup(tileSize, tileKey, type);
		var size = tile.shape.Size;

		tile.GlobalPosition = new Vector2(
			bounds.GlobalPosition.X - bounds.shape.Size.X / 2f + size.X / 2f + size.X * x,
			bounds.GlobalPosition.Y - bounds.shape.Size.Y / 2f + size.Y / 2f + size.Y * y
		);


		tile.key = tileKey;
		tiles[tileKey] = tile;
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdatePlayerInput();
		UpdateTrees((float)delta);
	}

	private void UpdatePlayerInput()
	{
		var horizontal = 0;
		var vertical = 0;

		if (Input.IsActionJustReleased("up"))
		{
			vertical = -1;
		}
		else if (Input.IsActionJustReleased("right"))
		{
			horizontal = 1;
		}
		else if (Input.IsActionJustReleased("down"))
		{
			vertical = 1;
		}
		else if (Input.IsActionJustReleased("left"))
		{
			horizontal = -1;
		}

		if (horizontal != 0 || vertical != 0)
		{
			UpdatePlayerTile(horizontal: horizontal, vertical: vertical);
		}
	}

	private void UpdateTrees(float delta)
	{
		var bottomWaterTile = tiles.Values.MaxBy(tile => tile.GlobalPosition.Y);
		if (bottomWaterTile != null)
		{
			tree.GlobalPosition = new Vector2(tree.GlobalPosition.X - 250f * (float)delta, tree.GlobalPosition.Y);
		}

		if (tree.GlobalPosition.X + tree.shape.Size.X / 2f < bounds.GlobalPosition.X - bounds.shape.Size.X / 2f)
		{
			SpawnTree();
		}

		var treeTiles = tiles.Values.ToList()
		.Where(tile =>
		{
			var treeLeftSide = tree.GlobalPosition.X - tree.shape.Size.X / 2f;
			var treeRightSide = tree.GlobalPosition.X + tree.shape.Size.X / 2f;
			var tileLeftSide = tile.GlobalPosition.X - tile.shape.Size.X / 2f;
			var tileRightSide = tile.GlobalPosition.X + tile.shape.Size.X / 2f;
			return tile.GlobalPosition.Y == tree.GlobalPosition.Y
			&& ((treeLeftSide >= tileLeftSide && treeLeftSide <= tileRightSide) || (treeRightSide >= tileLeftSide && treeRightSide <= tileRightSide));
		}).Select(tile => tile.key).ToHashSet();

		tiles.Values.ToList().ForEach(tile =>
		{
			if (treeTiles.Contains(tile.key))
			{
				tile.UpdateType(TileType.Tree);
			}
			else
			{
				tile.ResetTypeToOriginal();
			}
		});
	}

	private void UpdatePlayerTile(int horizontal, int vertical)
	{
		var currentTile = GetKeyForCoordinates(player.GlobalPosition);
		var newTile = tiles[ClampKey(currentTile.Copy(newX: currentTile.X + horizontal, newY: currentTile.Y + vertical))];
		player.GlobalPosition = newTile.GlobalPosition;

		switch (newTile.tileType)
		{
			case TileType.Ground:
				break;
			case TileType.Tree:
				break;
			case TileType.Water:
				HandlePlayerDying(newTile.key);
				break;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.IsReleased())
		{
			var position = eventMouseButton.Position;

			var key = GetKeyForCoordinates(position);
			var newType = TileType.Ground;
			switch (tiles[key].tileType)
			{
				case TileType.Ground:
					newType = TileType.Water;
					break;
				case TileType.Water:
					newType = TileType.Ground;
					break;
			}
			tiles[key].UpdateType(newType);
			GD.Print($"MOUSE {position} {key}");
		}
	}

	private void HandlePlayerDying(TileKey playerTileKey)
	{
		GD.Print($"Player has died at tile: {tiles[playerTileKey].key}");
		Respawn();
	}

	private void Respawn()
	{
		InitPlayer();
	}

	private TileKey GetKeyForCoordinates(Vector2 coordinates)
	{
		var epsilon = 0.0001f;
		int x = (int)Mathf.Clamp((coordinates.X - epsilon) / tileSize.X, 0, horizontalCount - 1);
		int y = (int)Mathf.Clamp((coordinates.Y - epsilon) / tileSize.Y, 0, verticalCount - 1);
		var tileKey = new TileKey(x, y);
		var finalKey = ClampKey(tileKey);
		var tile = tiles[finalKey];
		GD.Print($"position {coordinates} tile size {tileSize} key {tileKey} clamped {finalKey} tile position {tile.GlobalPosition}");

		return finalKey;
	}

	private TileKey ClampKey(TileKey key)
	{
		return key.Copy(
			newX: Math.Clamp(key.X, 0, horizontalCount - 1),
			newY: Math.Clamp(key.Y, 0, verticalCount - 1)
		);
	}
}
